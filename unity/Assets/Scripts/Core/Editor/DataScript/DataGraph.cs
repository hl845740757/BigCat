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
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCat.Editor.DataScript
{
/// <summary>
/// 数据图
///
/// 1.数据图通常对应一个文件，是DataNode的集合，其对应的Dson结构为Collection。
/// 2.数据图是分文件夹（folder）的，但folder是显示层的，不同folder的Node仍可通过localId直接引用；但显示层不应该绘制跨folder节点之间的连线。
/// 3.逻辑层是没有边的概念的，因为引用记录在输出端口上；如果需要为连接配置额外数据，可通过桥接Node实现。
/// 4.即所有的数据都通过Node存储，边只是显示层概念的。
/// 5.虽然框架支持自定义结构Map的Key，但推荐仅使用int32、int64、enum、string类型。
///
/// TODO 标记DataGraph是否为脏
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
    /// 2.避免直接修改List，请通过封装的方法操作。
    /// 3.避免对List的元素位置产生依赖，考虑在表现层维护额外的缓存List用于排序等逻辑。
    /// </summary>
    public readonly List<DataNode> nodeList = new List<DataNode>();
    /// <summary>
    /// 当前的所有Node
    /// </summary>
    public readonly LinkedDictionary<long, DataNode> nodeDic = new();
    /// <summary>
    /// 用户自定义数据(缓存)
    /// </summary>
    public object userData { get; set; }

    /// <summary>
    /// 资产文件路径
    /// </summary>
    public string assetPath { get; set; }
    public DsonTextWriterSettings writerSettings { get; set; } = DsonTextWriterSettings.Default;
    public DsonTextReaderSettings readerSettings { get; set; } = DsonTextReaderSettings.Default;

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
    private readonly StringBuilder _sbCache = new(1024);

    // 由于派发事件期间可能再次触发变化，因此不能使用单个缓存对象
    private int _modifyStack;
    private DataGraphChange _graphChange;
    private readonly ObjectPool<DataGraphChange> _graphChangePool = new ObjectPool<DataGraphChange>(
        DataGraphChange.Create, e => e.Clear());

    private readonly Dictionary<DSNamedType, DSNamedType> _pairTypeCache = new();
    private readonly DataGraphHelper _helper;
    private readonly LinkedHashSet<DSNamedType> _typeCreateStack = new(8);

    public DataGraph(DSRepository repository) {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tickTime = Time.realtimeSinceStartup;
        _helper = new DataGraphHelper(this);
    }

    /// <summary>
    /// 数据图发生变化时调用
    ///
    /// 注：Undo/Redo不会执行该方法，如需处理Undo/Redo，需监听<see cref="undoPerformed"/>和<see cref="redoPerformed"/>。
    /// </summary>
    public event Action<DataGraphChange> onGraphChanged;

    /// <summary>
    /// 开始数据修改
    /// </summary>
    public void BeginModify() {
        // 通过栈延迟事件派发，实际上并不是个好主意
        _modifyStack++;
        _graphChange ??= _graphChangePool.Acquire();
    }

    /// <summary>
    /// 结束数据修改
    /// </summary>
    public void EndModify() {
        if (_modifyStack == 0) {
            throw new IllegalStateException();
        }
        _modifyStack--;
        if (_modifyStack == 0) {
            DataGraphChange graphChange = _graphChange;
            _graphChange = null;
            if (!graphChange.IsEmpty) {
                onGraphChanged?.Invoke(graphChange);
            }
            _graphChangePool.Release(graphChange);
        }
    }

    /// <summary>
    /// 编辑器需要驱动该方法以正确维护Undo队列
    /// </summary>
    public void Update() {
        // 避免卡死 —— 只是简单的丢弃堆栈，因为无法安全恢复
        if (_graphChange != null) {
            _modifyStack = 0;
            _graphChange = null;
        }
        float realtime = Time.realtimeSinceStartup;
        if (realtime - _tickTime < 0.1f) {
            return;
        }
        _tickTime = realtime;
        UpdateUndoQueue();
    }

    /// <summary>
    /// 由于需要和手动分配的LocalId去除，因此不能简单的基于当前最大值进行++
    /// </summary>
    /// <returns></returns>
    internal long NextLocalId() {
        do {
            _nextLocalId++;
        } while (nodeDic.ContainsKey(_nextLocalId));
        return _nextLocalId;
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
        DataNode node = new DataNode(NextLocalId())
        {
            value = CreateVariable(namedType)
        };
        node.value.SetDataNode(node);
        // 初始值 - 为避免冲突，只尝试从类型的归属文件读默认值
        DSFile enclosingFile = namedType.GetEnclosingFile();
        DSInst inst;
        if (!namedType.IsGenericType && (inst = enclosingFile.GetInst(namedType.SimpleName)) != null) {
            ResetVariable(node.value, inst.DsonValue);
        }
        return node;
    }

    /// <summary>
    /// 是否包含目标Node（引用测试）
    /// </summary>
    /// <param name="dataNode"></param>
    /// <returns></returns>
    public bool Contains(DataNode dataNode) {
        if (dataNode == null) return false;
        if (nodeDic.TryGetValue(dataNode.localId, out DataNode tempNode)) {
            return tempNode == dataNode;
        }
        return false;
    }

    /// <summary>
    /// 添加Node
    ///
    /// 注：在添加Node后应当避免再修改Node的特征值。
    /// </summary>
    /// <param name="node"></param>
    public void AddNode(DataNode node) {
        if (nodeDic.ContainsKey(node.localId)) {
            throw new ArgumentException("localId: " + node.localId);
        }
        // 初始化数据端口字段 - 字段类型可能变更
        // node.inputFields.Clear();
        InitOutputFields(node);
        node.graph = this;
        node.value.SetDataNode(node);
        //
        nodeDic.Add(node.localId, node);
        nodeList.Add(node);
        CreateInsertCommand(node);

        if (_graphChange != null) {
            _graphChange.insetNodes.Add(node);
            // _graphChange.deleteNodes.Remove(node);
            // _graphChange.updateNodes.Remove(node);
            // _graphChange.prevLocalIds.Remove(node.localId);
        } else if (onGraphChanged != null) {
            DataGraphChange graphChange = _graphChangePool.Acquire();
            graphChange.insetNodes.Add(node);
            onGraphChanged.Invoke(graphChange);
            _graphChangePool.Release(graphChange);
        }
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
        node.ApplyModifiedProperties(); // 主动备份一次
        //
        DataNode.NodeMemento prevState = node.currentMemento;
        node.graph = null;
        node.currentMemento = null;
        nodeDic.Remove(node.localId);
        nodeList.Remove(node);
        CreateDeleteCommand(prevState);
        //
        if (_graphChange != null) {
            _graphChange.deleteNodes.Add(node);
            // _graphChange.insetNodes.Remove(node);
            // _graphChange.updateNodes.Remove(node);
            // _graphChange.prevLocalIds.Remove(node.localId);
        } else if (onGraphChanged != null) {
            DataGraphChange graphChange = _graphChangePool.Acquire();
            graphChange.deleteNodes.Add(node);
            onGraphChanged.Invoke(graphChange);
            _graphChangePool.Release(graphChange);
        }
    }

    /// <summary>
    /// 批量删除Node
    ///
    /// 注：保留以方便未来优化删除命令。
    /// </summary>
    /// <param name="nodes">要删除的节点</param>
    /// <param name="disconnectInputs">是否断开数据层的输入数据</param>
    /// <param name="disconnectOutputs">是否断开数据层的输出数据</param>
    public void DeleteNodes(IEnumerable<DataNode> nodes, bool disconnectInputs = false, bool disconnectOutputs = false) {
        BeginModify();
        try {
            foreach (DataNode dataNode in nodes) {
                DeleteNode(dataNode, disconnectInputs, disconnectOutputs);
            }
        }
        finally {
            EndModify();
        }
    }

    /// <summary>
    /// 序列化Node为剪切板数据
    /// </summary>
    public string SerializeNodes(List<DataNode> nodes) {
        using StringWriter streamWriter = new StringWriter();
        using DsonTextWriter textWriter = new DsonTextWriter(writerSettings, streamWriter, true);
        // 内存Node也拷贝
        foreach (DataNode dataNode in nodes) {
            _helper.Write(textWriter, dataNode);
        }
        textWriter.Flush();
        return streamWriter.ToString();
    }

    /// <summary>
    /// 反序列化数据并执行粘贴
    /// </summary>
    /// <param name="data">序列化数据</param>
    /// <param name="folder">粘贴到哪个虚拟文件夹</param>
    public List<DataNode> UnserializeAndPasteNodes(string data, string folder) {
        if (string.IsNullOrEmpty(data)) {
            return new List<DataNode>();
        }
        using DsonTextReader textReader = new DsonTextReader(readerSettings, data);
        DsonArray<string> collection = Dsons.ReadCollection(textReader);
        List<DataNode> srcNodes = new List<DataNode>(collection.Count);
        foreach (DsonValue dsonValue in collection) {
            DataNode dataNode = _helper.DecodeNode(dsonValue);
            dataNode.folder = folder;
            RepairNode(dataNode, true);
            srcNodes.Add(dataNode);
        }
        GraphPasteHelper pasteHelper = new GraphPasteHelper(this, srcNodes);
        List<DataNode> result = pasteHelper.Execute();
        BeginModify();
        try {
            foreach (DataNode dataNode in result) {
                AddNode(dataNode);
            }
        }
        finally {
            EndModify();
        }
        return result;
    }

    /// <summary>
    /// 复制Node(纯内存拷贝)
    /// </summary>
    public DataNode CopyNode(DataNode srcNode) {
        DataNode.NodeMemento memento = mementoPool.Acquire();
        srcNode.Backup(memento);
        //
        DataNode result = new DataNode(0);
        result.Restore(memento);
        RepairNode(result, true);
        return result;
    }

    /// <summary>
    /// 获取被引用的对象
    ///
    /// 注：该接口只支持不指定集合名的引用，即图内引用查询。
    /// </summary>
    public bool GetReferenceNode(ObjectPath objectPath, out DataNode dataNode) {
        if (objectPath.IsEmpty) {
            dataNode = null;
            return false;
        }
        if (objectPath.HasCollection) {
            dataNode = null;
            return false;
        }
        return nodeDic.TryGetValue(objectPath.localId, out dataNode);
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
                if (Disconnect(variable, false)) count++;
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
    /// 批量断开连接
    /// </summary>
    /// <param name="list"></param>
    public void Disconnect(List<Variable> list) {
        if (list.Count == 0) {
            return;
        }
        HashSet<DataNode> modifiedNodes = _nodeSetPool.Acquire();
        foreach (Variable variable in list) {
            if (Disconnect(variable, false)) {
                modifiedNodes.Add(variable.dataNode);
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
    /// <param name="variable">output字段，可能是List的元素</param>
    /// <param name="applyModifiers">是否应用修改</param>
    /// <returns>数据是否发生改变</returns>
    private bool Disconnect(Variable variable, bool applyModifiers = true) {
        if (variable.dataNode == null) { // 已被删除的变量
            return false;
        }
        // List删除所有连接 - Map端口会转为List
        if (variable.isCollectionType) {
            if (variable.Count == 0) {
                return false;
            }
            variable.ClearArray();
            if (applyModifiers) {
                variable.ApplyModifiedProperties();
            }
            return true;
        }
        // 需要判断是否是动态端口（List的元素）
        DataNode dataNode = variable.dataNode;
        if (dataNode.outputFields.Contains(variable)) {
            ObjectPath objectPath = variable.objectPathValue;
            if (objectPath.IsEmpty) { // Empty不测试Type
                return false;
            }
            variable.objectPathValue = default;
            if (applyModifiers) {
                variable.ApplyModifiedProperties();
            }
            return true;
        }
        // 查找归属的List容器
        foreach (Variable outputField in dataNode.outputFields) {
            int index = outputField.IndexOf(variable);
            if (index < 0) {
                continue;
            }
            outputField.RemoveAt(index);
            if (applyModifiers) {
                variable.ApplyModifiedProperties();
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 连接连接
    /// </summary>
    /// <param name="variable">port变量/输出字段</param>
    /// <param name="targetNode">目标节点</param>
    /// <param name="applyModifiers">是否应用修改</param>
    public void Connect(Variable variable, DataNode targetNode, bool applyModifiers = true) {
        if (targetNode == null) {
            throw new ArgumentNullException(nameof(targetNode));
        }
        DSNamedType varType = variable.type;
        ObjectPath objectPath = new ObjectPath(targetNode.localId);
        if (DSUtil.IsCollectionType(varType)) {
            FieldPortCfg portCfg = variable.cfg.portCfg;
            if (portCfg != null && portCfg.distinct && ContainsConnection(variable, objectPath)) {
                return;
            }
            Variable nestedVar = CreateListItem(variable);
            nestedVar.objectPathValue = objectPath;
            variable.Add(nestedVar);
            if (applyModifiers) {
                variable.ApplyModifiedProperties();
            }
            return;
        }
        variable.objectPathValue = objectPath;
        if (applyModifiers) {
            variable.ApplyModifiedProperties();
        }
    }

    private static bool ContainsConnection(Variable variable, ObjectPath connection) {
        foreach (Variable nestedVar in variable.values) {
            ObjectPath objectPath = nestedVar.objectPathValue;
            if (objectPath.collection == connection.collection
                && objectPath.localId == connection.localId) {
                return true; // 需要忽略Type比较，因此不能直接使用'=='
            }
        }
        return false;
    }

    /// <summary>
    /// 获取指向Node的所有输入（实时查询）
    /// 
    /// 注：
    /// 1.只会返回collection为空且localId等于目标Node的引用，会返回跨虚拟文件夹的引用。
    /// 2.由于UI刷新频率较高，因此UI应该使用Node上的缓存数据
    /// </summary>
    private void GetInputs(DataNode targetNode, List<Variable> result) {
        foreach (DataNode dataNode in nodeList) {
            if (dataNode.outputFields.Count == 0) {
                continue;
            }
            foreach (Variable outputField in dataNode.outputFields) {
                if (!outputField.isCollectionType) {
                    ObjectPath objectPath = outputField.objectPathValue;
                    if (objectPath.HasCollection || objectPath.localId != targetNode.localId) {
                        continue;
                    }
                    result.Add(outputField);
                    continue;
                }
                // List类型需要扫描子节点
                foreach (Variable nestedVar in outputField.values) {
                    ObjectPath objectPath = nestedVar.objectPathValue;
                    if (objectPath.HasCollection || objectPath.localId != targetNode.localId) {
                        continue;
                    }
                    result.Add(nestedVar);
                }
            }
        }
    }

    private DSNamedType GetObjectPathType() {
        return repository.GetType("ObjectPath")
               ?? throw new InvalidOperationException("ObjectPath not found");
    }

    /// <summary>
    /// 初始化Node的Port字段
    /// 
    /// 注：
    /// 1.会递归所有的静态路径字段，并将标记有PortField注解的字段转换为ObjectPath类型。
    /// 2.在修改Node的顶层Value的数据类型后，应当调用该方法修正端口数据。
    /// 3.Pair类型启用Port特征值后需要调用该方法初始化端口数据。
    /// </summary>
    public void InitOutputFields(DataNode node) {
        node.outputFields.Clear();
        if (!node.features.HasFlag(Features.EnablePort)) {
            return;
        }
        // Pair<K, V>修改为Pair<K, ObjectPath> -- 顶层Pair节点支持动态启用Port，主要用于支持字典
        DSNamedType varType = node.value.type;
        if (DSUtil.IsPairType(varType)) {
            DSNamedType objectPathType = GetObjectPathType();
            if (varType.TypeArguments[1] != objectPathType) {
                varType = repository.MakeGenericType(varType.OriginNamedType, new List<DSTypeElement>(2)
                {
                    varType.TypeArguments[0],
                    objectPathType
                });
                ChangeVariableType(node.value, varType, false);
                ChangeVariableType(node.value[1], objectPathType, false);
            }
            node.outputFields.Add(node.value[1]); // value字段
            return;
        }
        InitOutputFields(node.value, node.outputFields);
    }

    private void InitOutputFields(Variable variable, List<Variable> outList) {
        DSNamedType varType = variable.type;
        if (DSUtil.IsAtomicType(varType)) { // 这里不能拦截值类型，否则会拦截转换后的ObjectPath
            return;
        }
        if (!variable.cfg.HasPortCfg) {
            if (DSUtil.IsCollectionOrMapType(varType)) {
                return; // 不扫描动态路径字段
            }
            foreach (Variable nestedVar in variable.values) {
                InitOutputFields(nestedVar, outList);
            }
            return;
        }
        DSNamedType objectPathType = GetObjectPathType();
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
        // Map类型也修改为List<ObjectPath> -- 通过Pair节点赋值Key和连接Value
        if (DSUtil.IsMapType(varType)) {
            DSNamedType listType = repository.GetType(DSKeywords.TYPE_LIST);
            varType = repository.MakeGenericType(listType, new List<DSTypeElement>(1)
            {
                objectPathType
            });
            ChangeVariableType(variable, varType, false);
            outList.Add(variable);
            return;
        }
        // 普通Class修改为ObjectPath
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
            isNull = DSUtil.IsNullableType(type)
        };
        if (variableCfg.initNull) {
            variable.isNull = true;
            variable.values = new List<Variable>();
        } else {
            CreateValues(variable);
        }
        return variable;
    }

    /// <summary>
    /// 创建变量的Values
    /// 
    /// 注：创建变量的Values意味着变量不再为null。
    /// </summary>
    internal void CreateValues(Variable variable) {
        DSNamedType varType = variable.type;
        if (DSUtil.IsAtomicType(varType)) {
            return;
        }
        // Object视作空集合 - 以保证空对象的正确性
        if (DSUtil.IsCollectionOrMapType(varType) || DSUtil.IsObjectType(varType)) {
            variable.values = new List<Variable>();
            return;
        }
        // Nullable提前创建变量 - 稳定path
        if (DSUtil.IsNullableType(varType)) {
            DSField valueField = variable.type.GetField("value")!;
            VariableCfg elementCfg = variable.cfg?.elementCfg;
            //
            variable.values = new List<Variable>(1);
            variable.Add(CreateVariable(valueField, (DSNamedType)valueField.Type, elementCfg));
            return;
        }
        // Pair类型字段向下传递配置
        if (DSUtil.IsPairType(varType)) {
            DSField keyField = variable.type.GetField("key")!;
            DSField valueField = variable.type.GetField("value")!;
            VariableCfg elementCfg = variable.cfg?.elementCfg;
            //
            variable.values = new List<Variable>(2);
            variable.Add(CreateVariable(keyField, (DSNamedType)keyField.Type));
            variable.Add(CreateVariable(valueField, (DSNamedType)valueField.Type, elementCfg));
            return;
        }
        // 如果引用出现类型递归，强制延迟创建 - ObjectField需要做一下支持
        if (!_typeCreateStack.Add(varType)) {
            Debug.LogWarning("Reference type recursion"
                             + $", root: {_typeCreateStack.PeekFirst().FullName}"
                             + $", filed: {varType.SimpleName}.{variable.defineInfo.SimpleName}"
                             + ", manual init required");
            variable.isNull = true;
            variable.values = new List<Variable>();
            return;
        }
        // 递归创建Value
        List<DSField> fields = varType.GetFields(true, _fieldListPool.Acquire());
        try {
            variable.values = new List<Variable>(fields.Count);
            foreach (DSField field in fields) {
                variable.Add(CreateVariable(field));
            }
        }
        finally {
            _typeCreateStack.Remove(varType);
            _fieldListPool.Release(fields);
        }
    }

    /// <summary>
    /// 切换变量的数据结构类型
    /// 
    /// 1.如果切换Node变量的数据类型，方法返回后需要重新初始化Node的输出字段信息<see cref="InitOutputFields"/>。
    /// 为什么不在方法内部自动处理Output字段切换？那样返回的变量类型就和参数不同了。
    /// 
    /// 2.如果切换Node变量的数据类型，方法会强制修正defineInfo和cfg属性；
    /// 那List/Map类型作为顶层节点不就永远只有默认配置了吗？是的，需要额外的类型封装。
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
        string dson = inheritData ? DoCopy(variable) : null;
        variable.values = null;
        variable.type = newType;
        // 修正顶层变量定义
        DataNode dataNode = variable.dataNode;
        if (dataNode != null && variable == dataNode.value) {
            variable.defineInfo = newType;
            variable.cfg = GetVariableCfg(newType);
        }
        // 按照新类型再初始化
        CreateValues(variable);
        if (inheritData) {
            DoPaste(variable, dson);
        }
        if (dataNode != null) {
            variable.SetDataNode(dataNode);
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
        // List和字典直接清空
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
    ///
    /// 注：只继承数据，不继承类型（不安全）。
    /// </summary>
    public void ResetVariable(Variable variable, DsonValue dsonValue) {
        ResetVariable(variable);
        _helper.Decode(variable, dsonValue);
    }

    /// <summary>
    /// 创建一个Map的值
    /// </summary>
    /// <param name="variable"></param>
    public Variable CreateListItem(Variable variable) {
        DSField valuesField = variable.type.GetField("values")!;
        VariableCfg elementCfg = variable.cfg?.elementCfg; // 如果List是顶层对象，则可能为null
        return CreateVariable(valuesField, (DSNamedType)valuesField.Type, elementCfg);
    }

    /// <summary>
    /// 创建一个Map的值
    /// </summary>
    /// <param name="variable"></param>
    public Variable CreateMapItem(Variable variable) {
        DSNamedType pairType = GetPairType(variable.type);
        Variable pairVar = CreateVariable(pairType, variable.cfg); // 由Pair传递给Value
        return pairVar;
    }

    private DSNamedType GetPairType(DSNamedType mapType) {
        if (_pairTypeCache.TryGetValue(mapType, out DSNamedType pairType)) {
            return pairType;
        }
        pairType = repository.GetBuiltinType(DSKeywords.TYPE_PAIR);
        pairType = repository.MakeGenericType(pairType, new List<DSTypeElement>(2)
        {
            mapType.TypeArguments[0],
            mapType.TypeArguments[1],
        });
        _pairTypeCache[mapType] = pairType;
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
        result.Restore(variable);
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
        // inst MyClass:10001 {} 冒号的常见作用之一就是表作用域，更双冒号可能更常见
        return instName.StartsWith(typeName)
               && (instName[typeName.Length] == '/' || instName[typeName.Length] == ':');
    }

    /// <summary>
    /// 获取元素（字段）的声明类型
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static DSNamedType GetDeclaredType(DSElement element) {
        return element is DSField field ? (DSNamedType)field.Type : (DSNamedType)element;
    }

    #endregion

    #region 序列化

    /// <summary>
    /// 关闭文件
    /// </summary>
    public void Close() {
        if (string.IsNullOrWhiteSpace(assetPath)) {
            return;
        }
        nodeList.Clear();
        nodeDic.Clear();
        ClearRedoQueue();
        ClearUndoQueue();
        _nextLocalId = 0;
    }

    /// <summary>
    /// 保存数据到资产文件
    ///
    /// 注：这里主要处理编辑器下的Style需求，其逻辑与<see cref="Dsons"/>基本相同。
    /// </summary>
    public void Save() {
        if (string.IsNullOrWhiteSpace(assetPath)) {
            Debug.LogWarning("assetPath is null or empty");
            return;
        }
        string filePath = UnityEditorUtil.ConvertToFilePath(assetPath);
        using StreamWriter streamWriter = new StreamWriter(File.Create(filePath), new UTF8Encoding(false));
        using DsonTextWriter textWriter = new DsonTextWriter(writerSettings, streamWriter, true);
        //
        List<DataNode> sortedNodes = new(nodeList);
        sortedNodes.Sort((a, b) => {
            if (a == b) return 0;
            // 隐式类型靠后
            bool lhsImplicit = (a.features & Features.ImplicitType) != 0;
            bool rhsImplicit = (b.features & Features.ImplicitType) != 0;
            if (lhsImplicit != rhsImplicit) {
                return lhsImplicit ? 1 : -1;
            }
            return a.localId.CompareTo(b.localId);
        });
        foreach (DataNode dataNode in sortedNodes) {
            if ((dataNode.features & Features.MemoryOnly) != 0) {
                continue;
            }
            _helper.Write(textWriter, dataNode);
        }
        Debug.Log("Saved: " + assetPath);
    }

    /// <summary>
    /// 从资产文件中加载数据图
    ///
    /// 注意：该方法通常只应该在初始化阶段调用，否则可能导致错误。
    /// </summary>
    public void Load() {
        if (string.IsNullOrWhiteSpace(assetPath)) {
            Debug.LogWarning("assetPath is null or empty");
            return;
        }
        string filePath = UnityEditorUtil.ConvertToFilePath(assetPath);
        using StreamReader streamReader = new StreamReader(filePath, new UTF8Encoding(false));
        using DsonTextReader textReader = new DsonTextReader(readerSettings, streamReader);
        //
        DsonArray<string> collection = Dsons.ReadCollection(textReader);
        Close();
        Import(collection);
    }

    private void Import(DsonArray<string> collection) {
        foreach (DsonValue dsonValue in collection) {
            DataNode dataNode = _helper.DecodeNode(dsonValue);
            RepairNode(dataNode, false);
            nodeDic.Add(dataNode.localId, dataNode);
            nodeList.Add(dataNode);
            CreateInsertCommand(dataNode); // 否则无法回滚到初始状态
        }
        DataGraphChange graphChange = _graphChangePool.Acquire();
        graphChange.insetNodes.AddRange(nodeList);
        RepairGraph(graphChange);
        _graphChangePool.Release(graphChange);
    }

    /// <summary>
    /// 将内存中的对象导出为Dson文本
    /// </summary>
    public string DoCopy(Variable variable) {
        StringBuilder sb = _sbCache.Clear();
        using DsonTextWriter textWriter = new DsonTextWriter(writerSettings, new StringWriter(sb));
        _helper.Write(textWriter, variable, null);
        return sb.ToString();
    }

    /// <summary>
    /// 将文本赋值给当前变量
    /// </summary>
    public void DoPaste(Variable variable, string dson) {
        using DsonTextReader textReader = new DsonTextReader(readerSettings, dson);
        DsonValue dsonValue = Dsons.ReadTopDsonValue(textReader);
        _helper.Decode(variable, dsonValue);
    }

    #endregion

    #region undo/redo

    /// <summary>
    /// List可能为null，外部应当尽量避免持有List的引用
    /// </summary>
    public delegate void UndoRedoCallback(DataGraphChange graphChange);

    public event UndoRedoCallback undoPerformed;
    public event UndoRedoCallback redoPerformed;

    /// <summary>
    /// 执行Undo操作
    ///
    /// 注：Undo只恢复逻辑层数据，显示层应当重新构建。
    /// </summary>
    /// <returns></returns>
    public bool Undo() {
        if (!undoQueue.TryPeekLast(out Command tailCommand)) {
            return false;
        }
        DataGraphChange graphChange = _graphChangePool.Acquire();
        double tickTime = tailCommand.time;
        do {
            Command command = undoQueue.RemoveLast();
            redoQueue.TryAddFirst(command);
            switch (command.type) {
                case CommandType.Update: {
                    DataNode dataNode = RedoUpdateNode(command.prevState, command.nextState);
                    graphChange.updateNodes.Add(dataNode);
                    //
                    long prevLocalId = command.nextState.localId;
                    if (dataNode.localId != prevLocalId) {
                        graphChange.prevLocalIds[dataNode.localId] = prevLocalId;
                    }
                    break;
                }
                case CommandType.Insert: {
                    DataNode dataNode = RedoDeleteNode(command.nextState);
                    graphChange.deleteNodes.Add(dataNode);
                    break;
                }
                case CommandType.Delete: {
                    DataNode dataNode = RedoAddNode(command.prevState);
                    graphChange.insetNodes.Add(dataNode);
                    break;
                }
                default: throw new AssertionError();
            }
        } while (undoQueue.TryPeekLast(out tailCommand)
                 && Math.Abs(tailCommand.time - tickTime) < 0.001f);
        //
        RepairGraph(graphChange);
        try {
            undoPerformed?.Invoke(graphChange);
        }
        catch (Exception ex) {
            Debug.LogException(ex);
        }
        _graphChangePool.Release(graphChange);
        return true;
    }

    public bool Redo() {
        if (!redoQueue.TryPeekFirst(out Command headCommand)) {
            return false;
        }
        DataGraphChange graphChange = _graphChangePool.Acquire();
        double tickTime = headCommand.time;
        do {
            Command command = redoQueue.RemoveFirst();
            undoQueue.AddLast(command);
            switch (command.type) {
                case CommandType.Update: {
                    DataNode dataNode = RedoUpdateNode(command.nextState, command.prevState);
                    graphChange.updateNodes.Add(dataNode);
                    //
                    long prevLocalId = command.prevState.localId;
                    if (dataNode.localId != prevLocalId) {
                        graphChange.prevLocalIds[dataNode.localId] = prevLocalId;
                    }
                    break;
                }
                case CommandType.Insert: {
                    DataNode dataNode = RedoAddNode(command.nextState);
                    graphChange.insetNodes.Add(dataNode);
                    break;
                }
                case CommandType.Delete: {
                    DataNode dataNode = RedoDeleteNode(command.prevState);
                    graphChange.deleteNodes.Add(dataNode);
                    break;
                }
                default: throw new AssertionError();
            }
        } while (redoQueue.TryPeekFirst(out headCommand)
                 && Math.Abs(headCommand.time - tickTime) < 0.001f);
        //
        RepairGraph(graphChange);
        try {
            redoPerformed?.Invoke(graphChange);
        }
        catch (Exception ex) {
            Debug.LogException(ex);
        }
        _graphChangePool.Release(graphChange);
        return true;
    }

    private DataNode RedoUpdateNode(DataNode.NodeMemento nextState, DataNode.NodeMemento prevState) {
        DataNode dataNode = nodeDic[prevState.localId];
        dataNode.Restore(nextState);
        dataNode.currentMemento = nextState;
        // 同一批次的Undo自动恢复
        if (nextState.localId != prevState.localId) {
            FixPointer(dataNode, prevState.localId, nextState.localId, false);
            nodeDic.Remove(prevState.localId);
            nodeDic[nextState.localId] = dataNode;
        }
        RepairNode(dataNode, false);
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
        RepairNode(dataNode, true);
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
    /// 修复Node缓存数据
    /// </summary>
    private void RepairNode(DataNode node, bool initOutputFields) {
        node.graph = this;
        node.value.SetDataNode(node);
        if (initOutputFields) {
            InitOutputFields(node);
        }
    }

    /// <summary>
    /// 修正对象图数据
    /// (数据层好像暂时没什么特殊逻辑，因为我们并不会直接清理无效引用)
    /// </summary>
    private void RepairGraph(DataGraphChange graphChange) {
        // nodeList.Sort(CompareNode); // Sort会导致资产文件不必要的变动
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
    /// </summary>
    private void UpdateUndoQueue() {
        if (undoQueue.Count < 100) {
            return;
        }
        Command headCommand = undoQueue.PeekFirst();
        double backupTime = headCommand.time;
        if (_tickTime - backupTime < 60) {
            return;
        }
        // 同一批的操作同时删除以保证原子性
        do {
            Command command = undoQueue.RemoveFirst();
            if (command.prevState != null) {
                mementoPool.Release(command.prevState);
            }
        } while (undoQueue.TryPeekFirst(out headCommand)
                 && Math.Abs(headCommand.time - backupTime) < 0.001f);
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
        BeginModify();
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
        Command command = new Command()
        {
            time = _tickTime,
            type = CommandType.Update,
            prevState = prevState,
            nextState = nextState
        };
        ClearRedoQueue();
        undoQueue.TryAddLast(command);
        // 如果ID变化，需要纠正引用；也正因此，需要压制递归抛出的事件
        _graphChange.updateNodes.Add(node);
        if (prevState.localId != nextState.localId) {
            FixPointer(node, prevState.localId, nextState.localId, true);
            nodeDic.Remove(prevState.localId);
            nodeDic[nextState.localId] = node;
            _graphChange.prevLocalIds[node.localId] = prevState.localId;
        }
        EndModify();
    }

    private void FixPointer(DataNode node, long prevLocalId, long nextLocalId, bool applyModifiers) {
        node.localId = prevLocalId;
        List<Variable> list = _variableListPool.Acquire();
        GetInputs(node, list);
        node.localId = nextLocalId;
        //
        foreach (Variable variable in list) {
            ObjectPath objectPath = variable.objectPathValue;
            objectPath.localId = nextLocalId;
            variable.objectPathValue = objectPath;
            if (applyModifiers) {
                variable.ApplyModifiedProperties();
            }
        }
        _variableListPool.Release(list);
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