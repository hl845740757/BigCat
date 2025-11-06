#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson;
using Wjybxx.Dson.Types;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
/// 数据图
///
/// 1.数据图通常对应一个文件，是DataNode的集合，其对应的Dson结构为Collection。
/// 2.数据图是分文件夹（folder）的，但folder是显示层的，仍可通过localId直接引用；但显示层不应该绘制跨folder节点之间的连线。
/// 3.逻辑层是没有边的概念的，因为引用记录在输出端口上；如果需要为连接配置额外数据，可通过桥接Node实现。
/// 4.即所有的数据都通过Node存储，边只是显示层概念的。
/// 5.虽然框架支持自定义结构Map的Key，但推荐仅使用int32、int64、enum、string类型。
/// </summary>
public sealed class DataGraph
{
    /// <summary>
    /// 编辑器关联的DataScript仓库
    /// </summary>
    public readonly DSRepository repository;
    /// <summary>
    /// NodeList
    ///
    /// 1.部分Node是不保存到资产的，Node也不全存在于GraphView。
    /// 2.避免直接修改List，请通过封装的数组相关方法操作。
    /// 3.避免对List的元素位置产生依赖，考虑在表现层维护额外的缓存List用于排序等逻辑。
    /// </summary>
    public readonly List<DataNode> nodeList = new List<DataNode>();
    /// <summary>
    /// 当前的所有Node
    /// </summary>
    public readonly Dictionary<long, DataNode> nodeDic = new();

    /// <summary>
    /// 数据图展示对象(缓存)
    /// </summary>
    public GraphView graphView { get; set; }
    /// <summary>
    /// 用户自定义数据(缓存)
    /// </summary>
    public object userData { get; set; }

    /// <summary>
    /// 用于分配LocalId（需要和已存在的Node去重）
    /// </summary>
    private long _nextLocalId;
    /// <summary>
    /// 按帧计时，同一帧产生的修改同时执行undo和redo
    /// </summary>
    private double _tickTime;
    private readonly ArrayDeque<Command> undoQueue = new(50);
    private readonly ArrayDeque<Command> redoQueue = new(20);
    private readonly ObjectPool<DataNode.NodeMemento> mementoPool = new(
        () => new DataNode.NodeMemento(), e => e.Reset(), 100);

    private readonly ObjectPool<HashSet<DataNode>> _nodeSetPool = ObjectPoolUtil.NewHashSetPool<DataNode>(4);
    private readonly ObjectPool<List<Variable>> _variableListPool = ObjectPoolUtil.NewListPool<Variable>(4);
    private readonly ObjectPool<List<DSField>> _fieldListPool = ObjectPoolUtil.NewListPool<DSField>(8);
    private readonly ObjectPool<List<DSTypeElement>> _typeListPool = ObjectPoolUtil.NewListPool<DSTypeElement>(2);

    private readonly DataGraphHelper _helper;

    public DataGraph(DSRepository repository) {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tickTime = Time.realtimeSinceStartup;
        _helper = new DataGraphHelper(this);
    }

    public void Update() {
        float realtime = Time.realtimeSinceStartup;
        if (realtime - _tickTime < 0.1f) {
            return;
        }
        _tickTime = realtime;
        UpdateUndoQueue();
    }

    #region node

    /// <summary>
    /// 创建Node
    ///
    /// 注：该方法只负责一些公共的初始化工作，不会立即添加到对象图。
    /// </summary>
    /// <param name="namedType">node的初始类型</param>
    /// <returns></returns>
    public DataNode CreateNode(DSNamedType namedType = null) {
        namedType ??= repository.GetBuiltinType(DSKeywords.TYPE_OBJECT);
        DataNode nodeData = new DataNode(++_nextLocalId)
        {
            value = CreateVariable(namedType)
        };
        return nodeData;
    }

    /// <summary>
    /// 获取指定folder下的所有节点
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="outList"></param>
    public void GetNodes(string folder, List<DataNode> outList) {
        foreach (DataNode dataNode in nodeList) {
            if (dataNode.folder == folder) {
                outList.Add(dataNode);
            }
        }
    }

    /// <summary>
    /// 添加Node
    ///
    /// 注：在添加Node后应当避免再修改Node的特征值。
    /// </summary>
    /// <param name="node"></param>
    public void AddNode(DataNode node) {
        // localId可能来自反序列化数据
        _nextLocalId = Math.Max(_nextLocalId, node.localId);
        if (nodeDic.ContainsKey(node.localId)) {
            throw new ArgumentException("localId: " + node.localId);
        }
        // 初始化数据端口字段
        // node.inputFields.Clear();
        if (node.features.HasFlag(Features.EnablePort)) {
            InitOutputFields(node);
        }
        node.graph = this;
        nodeDic.Add(node.localId, node);
        nodeList.Add(node);
        nodeList.Sort(CompareNode);
        CreateInsertCommand(node);
    }

    /// <summary>
    /// 删除节点
    /// 
    /// 注：删除Node默认不会删除其它Node对它的引用，只是引用会无效。
    /// </summary>
    /// <param name="node">要删除的节点</param>
    /// <param name="disconnectInputs">是否断开数据层的输入数据</param>
    /// <param name="disconnectOutputs">是否断开数据层的输出数据</param>
    public void DeleteNode(DataNode node, bool disconnectInputs = false, bool disconnectOutputs = false) {
        if (!nodeDic.ContainsKey(node.localId)) {
            return;
        }
        Disconnect(node, disconnectInputs, disconnectOutputs);
        // 主动备份一次
        if (node.IsDataChanged()) {
            CreateUpdateCommand(node);
        }
        DataNode.NodeMemento prevState = node.currentMemento;
        node.graph = null;
        node.currentMemento = null;
        nodeDic.Remove(node.localId);
        nodeList.Remove(node);
        CreateDeleteCommand(prevState);
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    /// <param name="node">节点</param>
    /// <param name="disconnectInputs">是否断开数据层的输入数据</param>
    /// <param name="disconnectOutputs">是否断开数据层的输出数据</param>
    public void Disconnect(DataNode node, bool disconnectInputs, bool disconnectOutputs) {
        if (!disconnectInputs && !disconnectOutputs) {
            return;
        }
        HashSet<DataNode> modifiedNodes = _nodeSetPool.Acquire();
        if (disconnectInputs) {
            List<Variable> inputs = _variableListPool.Acquire();
            GetInputs(node, inputs);
            foreach (Variable variable in inputs) {
                Disconnect(variable, false);
                if (variable.dataNode != null) {
                    modifiedNodes.Add(variable.dataNode);
                }
            }
            _variableListPool.Release(inputs);
        }
        if (disconnectOutputs && node.outputFields.Count > 0) {
            int count = 0;
            foreach (Variable variable in node.outputFields) {
                if (variable.objectPathValue.IsEmpty) {
                    continue;
                }
                count++;
                Disconnect(variable, false);
            }
            if (count > 0) {
                modifiedNodes.Add(node);
            }
        }
        foreach (DataNode dataNode in modifiedNodes) {
            dataNode.ApplyModifiedProperties();
        }
        _nodeSetPool.Release(modifiedNodes);
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    /// <param name="variable">output字段</param>
    /// <param name="applyModifiers">是否应用修改</param>
    public void Disconnect(Variable variable, bool applyModifiers = true) {
        ObjectPath objectPath = variable.objectPathValue;
        if (objectPath.IsEmpty) { // Empty不测试Type
            return;
        }
        int assetType = objectPath.type; // 保留assetType
        objectPath = default;
        objectPath.type = assetType;
        variable.objectPathValue = objectPath;
        if (applyModifiers) {
            variable.ApplyModifiedProperties();
        }
    }

    /// <summary>
    /// 获取指向Node的所有输入
    /// 
    /// 注：只会返回collection为空且localId等于目标Node的引用，会返回跨虚拟文件夹的引用。
    /// </summary>
    public void GetInputs(DataNode targetNode, List<Variable> result) {
        // 由于Node数量通常不多，因此实时查询的效率足够
        foreach (DataNode dataNode in nodeList) {
            if (dataNode.outputFields.Count == 0) continue;
            foreach (Variable outputField in dataNode.outputFields) {
                ObjectPath objectPath = outputField.objectPathValue;
                if (objectPath.HasCollection || objectPath.localId != targetNode.localId) {
                    continue;
                }
                result.Add(outputField);
            }
        }
    }

    /// <summary>
    /// 初始化Node的Port字段
    /// 
    /// 注：
    /// 1.会递归所有的静态路径字段，并将标记有PortField注解的字段转换为ObjectPath类型。
    /// 2.在修改Node的顶层Value的数据类型后，应当调用该方法修正端口数据。
    /// </summary>
    public void InitOutputFields(DataNode nodeData) {
        nodeData.outputFields.Clear();
        if (!nodeData.features.HasFlag(Features.EnablePort)) {
            return;
        }
        InitOutputFields(nodeData.value, nodeData.outputFields);
    }

    private void InitOutputFields(Variable variable, List<Variable> outList) {
        DSNamedType varType = variable.type;
        if (varType.IsValueType || DSUtil.IsAtomicType(varType)) {
            return;
        }
        VariableCfg variableCfg = variable.cfg;
        if (!variableCfg.HasPortCfg) {
            if (DSUtil.IsCollectionOrMapType(varType)) {
                return; // 不扫描动态路径字段
            }
            foreach (Variable nestedVar in variable.values) {
                InitOutputFields(nestedVar, outList);
            }
            return;
        }
        DSNamedType objectPathType = repository.GetType("ObjectPath");
        // 集合类型修改为List<ObjectPath>
        if (DSUtil.IsCollectionType(varType)) {
            if (varType.TypeArguments[0] != objectPathType) {
                varType = repository.MakeGenericType(varType.OriginNamedType, new List<DSTypeElement>(1)
                {
                    objectPathType
                });
                ChangeVariableType(variable, varType, false);
            }
            outList.Add(variable);
            return;
        }
        // 字典类型修改为Map<Key,ObjectPath>
        if (DSUtil.IsMapType(varType)) {
            if (varType.TypeArguments[1] != objectPathType) {
                varType = repository.MakeGenericType(varType.OriginNamedType, new List<DSTypeElement>(2)
                {
                    varType.TypeArguments[0],
                    objectPathType
                });
                ChangeVariableType(variable, varType, false);
            }
            outList.Add(variable);
            return;
        }
        // 普通Class修改ObjectPath
        {
            if (varType != objectPathType) {
                ChangeVariableType(variable, objectPathType, false);
            }
            outList.Add(variable);
        }
    }

    #endregion

    #region variable

    /// <summary>
    /// 创建变量
    /// </summary>
    public Variable CreateVariable(DSElement defineInfo, VariableCfg variableCfg = null) {
        return CreateVariable(defineInfo, GetDeclaredType(defineInfo), variableCfg);
    }

    /// <summary>
    /// 创建变量
    /// 
    /// 1.多态类型的字段，在创建时应当传入目标类型
    /// 2.集合创建元素变量时，应当传入元素的编辑器配置
    /// </summary>
    /// <param name="defineInfo">变量的定义信息</param>
    /// <param name="type">变量的初始类型</param>
    /// <param name="variableCfg">变量的展示配置</param>
    /// <returns></returns>
    public Variable CreateVariable(DSElement defineInfo, DSNamedType type, VariableCfg variableCfg = null) {
        variableCfg ??= GetVariableCfg(defineInfo);
        Variable variable = new Variable
        {
            defineInfo = defineInfo,
            cfg = variableCfg,
            type = type ?? throw new ArgumentNullException(nameof(type)),
            typeSymbol = DSUtil.ToDisplayString(type.TypeName),
            isNull = variableCfg.initNull || DSUtil.IsNullableType(type)
        };
        CreateValues(variable);
        return variable;
    }

    /// <summary>
    /// 创建变量的Values
    ///
    /// 注：创建变量的Values意味着变量不再为null。
    /// </summary>
    /// <param name="variable"></param>
    private void CreateValues(Variable variable) {
        DSNamedType varType = variable.type;
        if (DSUtil.IsAtomicType(varType)) {
            return;
        }
        if (DSUtil.IsCollectionOrMapType(varType)) {
            variable.values = new List<Variable>();
            return;
        }
        // Nullable提前创建变量 - 稳定path
        if (DSUtil.IsNullableType(varType)) {
            DSField valueField = variable.type.GetField("value")!;
            VariableCfg elementCfg = variable.cfg.elementCfg;
            Variable nestedVar = CreateVariable(valueField, (DSNamedType)valueField.Type, elementCfg);
            variable.values = new List<Variable>(1);
            variable.Add(nestedVar);
            return;
        }
        // 递归创建Value
        List<DSField> fields = varType.GetFields(true, _fieldListPool.Acquire());
        variable.values = new List<Variable>(fields.Count);
        foreach (DSField field in fields) {
            variable.Add(CreateVariable(field));
        }
        _fieldListPool.Release(fields);
    }

    /// <summary>
    /// 切换变量的数据结构类型
    ///
    /// 注：如果切换Node顶层变量的数据类型，需要重新初始化Node的输出字段信息<see cref="InitOutputFields"/>。
    /// </summary>
    /// <param name="variable">目标变量</param>
    /// <param name="newType">新类型</param>
    /// <param name="inheritData">是否继承当前数据</param>
    /// <returns>是否切换了类型</returns>
    public bool ChangeVariableType(Variable variable, DSNamedType newType, bool inheritData = true) {
        if (newType == null) throw new ArgumentNullException(nameof(newType));
        if (Equals(variable.type, newType)) {
            return false;
        }
        DsonValue dsonValue = inheritData ? Encode(variable) : null;
        ResetVariable(variable);
        // 按照新类型再初始化
        variable.type = newType;
        variable.typeSymbol = DSUtil.ToDisplayString(newType.TypeName);
        CreateValues(variable);
        if (inheritData) {
            Decode(variable, dsonValue);
        }
        return true;
    }

    /// <summary>
    /// 重置变量为默认值
    /// 
    /// 注：Nullable类型会自动置为null，其它结构需手动指定。
    /// </summary>
    /// <param name="variable">要重置的变量</param>
    public void ResetVariable(Variable variable) {
        if (variable == null) return;
        variable.longValue = 0;
        variable.doubleValue = 0;
        variable.stringValue = null;
        //
        List<Variable> values = variable.values;
        if (values == null || values.Count == 0) {
            return;
        }
        // List和字典直接清空 - 需要记录调用电
        if (DSUtil.IsCollectionOrMapType(variable.type)) {
            variable.ClearArray();
            return;
        }
        // Nullable固定重置为null，普通Object由用户选择是否重置为null
        if (DSUtil.IsNullableType(variable.type)) {
            variable.isNull = true;
        }
        foreach (Variable nestedVar in values) {
            ResetVariable(nestedVar);
        }
    }

    /// <summary>
    /// 重置变量为指定值
    /// </summary>
    /// <param name="variable"></param>
    /// <param name="dsonValue"></param>
    public void ResetVariable(Variable variable, DsonValue dsonValue) {
        ResetVariable(variable);
        Decode(variable, dsonValue);
    }

    /// <summary>
    /// 创建一个Map的值
    /// </summary>
    /// <param name="variable"></param>
    public Variable CreateListItem(Variable variable) {
        DSField valuesField = variable.type.GetField("values")!;
        return CreateVariable(valuesField, (DSNamedType)valuesField.Type, variable.cfg.elementCfg);
    }

    /// <summary>
    /// 创建一个Map的值
    /// </summary>
    /// <param name="variable"></param>
    public Variable CreateMapItem(Variable variable) {
        DSNamedType pairType = GetPairType(variable.type);
        Variable pairVar = CreateVariable(pairType);
        pairVar[1].cfg = variable.cfg.elementCfg; // 修正value的配置
        return pairVar;
    }

    private DSNamedType GetPairType(DSNamedType mapType) {
        DSField keysField = mapType.GetField("keys")!;
        DSField valuesField = mapType.GetField("values")!;

        List<DSTypeElement> list = _typeListPool.Acquire();
        list.Add(keysField.Type);
        list.Add(valuesField.Type);

        DSNamedType pairType = repository.GetBuiltinType(DSKeywords.TYPE_PAIR);
        pairType = repository.MakeGenericType(pairType, list);
        _typeListPool.Release(list);
        return pairType;
    }

    /// <summary>
    /// 拷贝变量
    /// </summary>
    /// <param name="variable"></param>
    /// <returns></returns>
    public Variable Duplicate(Variable variable) {
        // 新模式下不再需要先导出Dson，直接拷贝内存
        Variable result = new Variable();
        result.Restore(variable, false);
        return result;
    }

    /// <summary>
    /// 获取类型的编辑器配置
    /// 
    /// 注：不可以修改返回对象的数据。
    /// </summary>
    /// <param name="element"></param>
    public VariableCfg GetVariableCfg(DSElement element) {
        DSElement originDefine = element.OriginDefine;
        if (originDefine.editorContext == null) {
            originDefine.editorContext = VariableCfg.Parse(originDefine);
            // 匹配关联的实例 - 只匹配顶层类型
            if (originDefine.Kind.IsNamedType()
                && !DSUtil.IsAtomicType(originDefine)
                && originDefine.EnclosingElement.Kind == DSElementKind.File) {
                InitSupportedInsts((DSNamedType)originDefine);
            }
        }
        return (VariableCfg)originDefine.editorContext;
    }

    private void InitSupportedInsts(DSNamedType namedType) {
        VariableCfg variableCfg = (VariableCfg)namedType.editorContext;
        foreach (DSFile dsFile in repository.FileMap.Values) {
            foreach (var pair in dsFile.InstMap) {
                if (!MatchInstName(namedType, pair.Key)) {
                    continue;
                }
                variableCfg.supportedInsts ??= new List<DSInst>();
                variableCfg.supportedInsts.Add(pair.Value);
            }
        }
        variableCfg.supportedInsts?.TrimExcess();
    }

    private static bool MatchInstName(DSNamedType namedType, string instName) {
        string typeName = namedType.SimpleName;
        if (instName == typeName) return true;
        // inst MyClass/A {}
        // inst MyClass:10001  冒号的常见作用之一就是表作用域，更双冒号可能更常见
        return instName.StartsWith(typeName)
               && (instName[typeName.Length] == '/' || instName[typeName.Length] == ':');
    }

    /// <summary>
    /// 获取元素（字段）的声明类型
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static DSNamedType GetDeclaredType(DSElement element) {
        return element is DSField field ? (DSNamedType)field.Type : (DSNamedType)element;
    }

    #endregion

    #region 序列化

    /// <summary>
    /// 导出数据，用户负责写入文件（硬盘）
    ///
    /// 注：导出的数据可直接用于反序列化。
    /// </summary>
    /// <returns></returns>
    public DsonArray<string> Export() {
        DsonArray<string> result = new DsonArray<string>(nodeList.Count);
        foreach (DataNode dataNode in nodeList) {
            result.Add(EncodeNode(dataNode));
        }
        return result;
    }

    /// <summary>
    /// 导入数据
    ///
    /// 注：该方法仅适用于初始化阶段。
    /// </summary>
    /// <param name="collection"></param>
    public void Import(DsonArray<string> collection) {
        nodeList.Clear();
        nodeDic.Clear();
        ClearRedoQueue();
        ClearUndoQueue();
        //
        foreach (DsonValue dsonValue in collection) {
            DataNode dataNode = DecodeNode(dsonValue);
            RepairNode(dataNode);
            nodeDic.Add(dataNode.localId, dataNode);
            nodeList.Add(dataNode);
        }
        RepairGraph(new List<DataNode>(nodeList), null, null);
    }

    private DsonValue EncodeNode(DataNode node) {
        return _helper.EncodeNode(node);
    }

    private DataNode DecodeNode(DsonValue dsonValue) {
        return _helper.DecodeNode(dsonValue);
    }

    /// <summary>
    /// 将DsonValue赋值给当前变量（反序列化）
    ///
    /// 1.对于集合类型字段，默认会清空当前所有数据，再填充数据。
    /// 2.对于自定义结构，只赋值（覆盖）DsonValue中存在的字段。
    /// 3.该接口通常只应该在反序列化、切换Node或多态字段绑定的数据类型时调用（将既有数据赋值给新对象）。
    /// </summary>
    public void Decode(Variable variable, DsonValue dsonValue) {
        _helper.Decode(variable, dsonValue);
    }

    /// <summary>
    /// 将内存中的对象导出为Dson对象（序列化）
    ///
    /// 1.如果是Nullable类型，导出时会进行拆箱。
    /// 2.对于字典，如果是标准字典类型，则导出为DsonObject，否则导出为DsonArray。
    /// </summary>
    public DsonValue Encode(Variable variable) {
        return _helper.Encode(variable);
    }

    #endregion

    #region undo/redo

    /// <summary>
    /// List可能为null，外部应当尽量避免持有List的引用
    /// </summary>
    public delegate void UndoRedoCallback(List<DataNode> insetNodes, List<DataNode> deleteNodes,
                                          List<DataNode> updateNodes);

    public event UndoRedoCallback undoPerformed;
    public event UndoRedoCallback redoPerformed;

    /// <summary>
    /// 执行Undo操作
    ///
    /// 注：Undo只恢复逻辑层数据，显示层应当重新构建。
    /// </summary>
    /// <returns></returns>
    public bool Undo() {
        if (!undoQueue.TryPeekLast(out Command lastCommand)) {
            return false;
        }
        List<DataNode> insetNodes = null;
        List<DataNode> deleteNodes = null;
        List<DataNode> updateNodes = null;
        double tickTime = lastCommand.time;
        do {
            Command command = undoQueue.RemoveLast();
            redoQueue.TryAddFirst(command);
            switch (command.type) {
                case CommandType.Update: {
                    DataNode dataNode = RedoUpdateNode(command.prevState);
                    updateNodes ??= new List<DataNode>();
                    updateNodes.Add(dataNode);
                    break;
                }
                case CommandType.Insert: {
                    DataNode dataNode = RedoDeleteNode(command.nextState);
                    deleteNodes ??= new List<DataNode>();
                    deleteNodes.Add(dataNode);
                    break;
                }
                case CommandType.Delete: {
                    DataNode dataNode = RedoAddNode(command.prevState);
                    insetNodes ??= new List<DataNode>();
                    insetNodes.Add(dataNode);
                    break;
                }
                default: throw new AssertionError();
            }
        } while (undoQueue.TryPeekLast(out lastCommand)
                 && Math.Abs(lastCommand.time - tickTime) < 0.001f);
        //
        RepairGraph(insetNodes, deleteNodes, updateNodes);
        try {
            undoPerformed?.Invoke(insetNodes, deleteNodes, updateNodes);
        }
        catch (Exception ex) {
            Debug.LogException(ex);
        }
        return true;
    }

    public bool Redo() {
        if (!redoQueue.TryPeekFirst(out Command lastCommand)) {
            return false;
        }
        List<DataNode> insetNodes = null;
        List<DataNode> deleteNodes = null;
        List<DataNode> updateNodes = null;
        double tickTime = lastCommand.time;
        do {
            Command command = redoQueue.RemoveFirst();
            undoQueue.AddLast(command);
            switch (command.type) {
                case CommandType.Update: {
                    DataNode dataNode = RedoUpdateNode(command.nextState);
                    updateNodes ??= new List<DataNode>();
                    updateNodes.Add(dataNode);
                    break;
                }
                case CommandType.Insert: {
                    DataNode dataNode = RedoAddNode(command.nextState);
                    insetNodes ??= new List<DataNode>();
                    insetNodes.Add(dataNode);
                    break;
                }
                case CommandType.Delete: {
                    DataNode dataNode = RedoDeleteNode(command.prevState);
                    deleteNodes ??= new List<DataNode>();
                    deleteNodes.Add(dataNode);
                    break;
                }
                default: throw new AssertionError();
            }
        } while (redoQueue.TryPeekFirst(out lastCommand)
                 && Math.Abs(lastCommand.time - tickTime) < 0.001f);
        //
        RepairGraph(insetNodes, deleteNodes, updateNodes);
        try {
            redoPerformed?.Invoke(insetNodes, deleteNodes, updateNodes);
        }
        catch (Exception ex) {
            Debug.LogException(ex);
        }
        return true;
    }

    private DataNode RedoUpdateNode(DataNode.NodeMemento backup) {
        long localId = backup.localId;
        if (!nodeDic.ContainsKey(localId)) {
            throw new IllegalStateException("localId: " + localId);
        }
        DataNode dataNode = nodeDic[localId];
        dataNode.Restore(backup);
        dataNode.currentMemento = backup;
        //
        RepairNode(dataNode);
        return dataNode;
    }

    private DataNode RedoAddNode(DataNode.NodeMemento backup) {
        long localId = backup.localId;
        if (nodeDic.ContainsKey(localId)) {
            throw new IllegalStateException("localId: " + localId);
        }
        DataNode dataNode = new DataNode(backup.localId);
        dataNode.Restore(backup);
        dataNode.currentMemento = backup;
        //
        RepairNode(dataNode);
        nodeDic.Add(backup.localId, dataNode);
        nodeList.Add(dataNode);
        return dataNode;
    }

    private DataNode RedoDeleteNode(DataNode.NodeMemento backup) {
        long localId = backup.localId;
        if (!nodeDic.ContainsKey(localId)) {
            throw new IllegalStateException("localId: " + localId);
        }
        DataNode node = nodeDic[localId];
        node.graph = null;
        node.currentMemento = null;
        nodeDic.Remove(localId);
        nodeList.Remove(node);
        return node;
    }

    /// <summary>
    /// 修复Node的缓存数据
    /// 
    /// 注意：只能修复逻辑层数据，不能修复显示层数据，需要重新构建GraphView。
    /// </summary>
    /// <param name="node"></param>
    private void RepairNode(DataNode node) {
        node.graph = this;
        Debug.Assert(node.value != null, "node.value == null"); // 我们已默认为Object类型
        RepairVariable(node.value);
        InitOutputFields(node);
    }

    private void RepairVariable(Variable variable) {
        if (variable.defineInfo == null) {
            throw new InvalidOperationException();
        }
        variable.type ??= (DSNamedType)repository.ResolveTypeSymbol(null, variable.typeSymbol);
        variable.cfg ??= GetVariableCfg(variable.type);
        if (variable.values == null) {
            return;
        }
        // 修正集合元素
        if (DSUtil.IsCollectionType(variable.type)) {
            DSField valuesField = variable.type.GetField("values")!;
            for (int index = 0; index < variable.values.Count; index++) {
                Variable nestedVar = variable.values[index];
                if (nestedVar == null) { // ListView会先行修改数组长度
                    nestedVar = CreateVariable(valuesField);
                    variable[index] = nestedVar;
                }
                nestedVar.defineInfo = valuesField;
                nestedVar.cfg = variable.cfg.elementCfg;
                RepairVariable(nestedVar);
            }
            return;
        }
        if (DSUtil.IsMapType(variable.type)) {
            DSNamedType pairType = GetPairType(variable.type);
            for (int index = 0; index < variable.values.Count; index++) {
                Variable pairVar = variable.values[index];
                if (pairVar == null) { // ListView会先行修改数组长度
                    pairVar = CreateVariable(pairType);
                    variable[index] = pairVar;
                }
                pairVar.defineInfo = pairType;
                pairVar.type = pairType;
                RepairVariable(pairVar);
                pairVar[1].cfg = variable.cfg.elementCfg; // 修正value的配置
            }
            return;
        }
        // 修正潜在的List/Map字段
        List<DSField> fields = variable.type.GetFields(true, _fieldListPool.Acquire());
        for (int index = 0; index < variable.values.Count; index++) {
            Variable nestedVar = variable.values[index];
            if (nestedVar == null) {
                throw new AssertionError();
            }
            nestedVar.defineInfo = fields[index];
            RepairVariable(nestedVar);
        }
        _fieldListPool.Release(fields);
    }

    /// <summary>
    /// 修正对象图数据
    /// (数据层好像暂时没什么特殊逻辑，因为我们并不会直接清理无效引用)
    /// </summary>
    private void RepairGraph(List<DataNode> insetNodes, List<DataNode> deleteNodes,
                             List<DataNode> updateNodes) {
        nodeList.Sort(CompareNode);
    }

    private static int CompareNode(DataNode a, DataNode b) {
        return a.localId.CompareTo(b.localId);
    }

    /// <summary>
    /// 清理Undo队列
    ///
    /// 注：Undo队列不会自动清理，但会压缩。
    /// </summary>
    public void ClearUndoQueue() {
        while (undoQueue.TryRemoveLast(out Command command)) {
            if (command.prevState != null) {
                mementoPool.Release(command.prevState);
            }
        }
    }

    /// <summary>
    /// 清理Redo队列
    ///
    /// 注：用户在redo队列不为空的情况下创建新的undo备份将引发redo队列清空。
    /// </summary>
    public void ClearRedoQueue() {
        while (redoQueue.TryRemoveFirst(out Command command)) {
            if (command.nextState != null) {
                mementoPool.Release(command.nextState);
            }
        }
    }


    /// <summary>
    /// 更新Undo队列（压缩数据）
    ///
    /// 注：由于短期可能创建大量的备份点，因此我们不按照数量决定上限，而是按照时间。
    /// </summary>
    /// <param name="timeout">备份的超时时间，单位秒</param>
    private void UpdateUndoQueue(double timeout = 300) {
        // 先合并短时间内创建的Undo记录
        // if (undoQueue.Count > 30) {
        //     buffer.Clear();
        //     buffer.EnsureCapacity(undoQueue.Count);
        //     //
        //     using var enumerator = undoQueue.GetEnumerator();
        //     enumerator.MoveNext();
        //     GraphMemento previous = enumerator.Current!;
        //     buffer.Add(previous);
        //     //
        //     while (enumerator.MoveNext()) {
        //         GraphMemento current = enumerator.Current!;
        //         if (current.backupTime - previous.backupTime < 0.5f) {
        //             mementoPool.Release(current);
        //         } else {
        //             buffer.Add(current);
        //             previous = current;
        //         }
        //     }
        //     undoQueue.Clear();
        //     foreach (GraphMemento memento in buffer) {
        //         undoQueue.AddLast(memento);
        //     }
        //     buffer.Clear();
        // }
        // // 再删除超时的记录
        // double currentTime = _tickTime;
        // while (undoQueue.Count > 20) {
        //     GraphMemento memento = undoQueue.PeekFirst();
        //     if (currentTime - memento.backupTime >= timeout) {
        //         undoQueue.RemoveFirst();
        //     }
        // }
    }

    private void CreateInsertCommand(DataNode node) {
        DataNode.NodeMemento nextState = mementoPool.Acquire();
        node.Backup(nextState);
        node.currentMemento = nextState;
        Command command = new Command()
        {
            time = _tickTime,
            type = CommandType.Insert,
            prevState = null,
            nextState = nextState
        };
        ClearRedoQueue();
        undoQueue.TryAddLast(command);
    }

    private void CreateDeleteCommand(DataNode.NodeMemento prevState) {
        Command command = new Command()
        {
            time = _tickTime,
            type = CommandType.Delete,
            prevState = prevState,
            nextState = null
        };
        ClearRedoQueue();
        undoQueue.TryAddLast(command);
    }

    internal void CreateUpdateCommand(DataNode node) {
        DataNode.NodeMemento prevState;
        DataNode.NodeMemento nextState;
        if (undoQueue.TryPeekLast(out Command prevCommand)
            && (_tickTime - prevCommand.time) < 0.1f
            && prevCommand.type == CommandType.Update
            && prevCommand.nextState.localId == node.localId) {
            undoQueue.RemoveLast();
            // 覆盖上次的备份数据 - 短时间内数据结构通常不会发生变化，因此通常可完全复用所有对象，只是简单的内存拷贝
            prevState = prevCommand.prevState;
            nextState = node.currentMemento;
            node.Backup(nextState);
        } else {
            prevState = node.currentMemento;
            nextState = mementoPool.Acquire();
            node.Backup(nextState);
            node.currentMemento = nextState;
        }
        //
        Command command = new Command()
        {
            time = _tickTime,
            type = CommandType.Update,
            prevState = prevState,
            nextState = nextState
        };
        ClearRedoQueue();
        undoQueue.TryAddLast(command);
    }

    private struct Command
    {
        public double time;
        public CommandType type;
        public DataNode.NodeMemento prevState; // 只有当不能执行Undo时才能回收前一个数据备份
        public DataNode.NodeMemento nextState; // 只有当不能执行Redo时才能回收下一个数据备份
        // public Dictionary<long, DataNode.NodeMemento> fullBackup; // TODO 不定期全量备份
    }

    private enum CommandType
    {
        Update,
        Insert,
        Delete,
        // Sort, // TODO 允许用户调整Node顺序 
    }

    #endregion
}
}