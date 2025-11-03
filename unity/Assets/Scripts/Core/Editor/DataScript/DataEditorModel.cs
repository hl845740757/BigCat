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
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;
using NumberStyles = Wjybxx.Dson.Text.NumberStyles;
using Object = UnityEngine.Object;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
/// 数据编辑器数据模型
///
/// 注：继承Unity的Object以使用<see cref="SerializedObject"/>提供的Redo和Undo功能，
/// 但该对象只应该存在于内存中，不应该被保存为资产对象。
/// 
/// 注：
/// 1.主要负责<see cref="NodeData"/>和<see cref="Variable"/>的创建和数据修复工作。
/// 2.对于字典类型，如果Key是Int32、Int64、String类型（即标准Map），则导出为DsonObject，否则导出为DsonArray。
/// 3.Node不一定存在于<see cref="GraphView"/>，但所有的数据抽象都是<see cref="NodeData"/>。
/// </summary>
public class DataEditorModel : ScriptableObject
{
    /// <summary>
    /// 编辑器关联的DataScript仓库
    /// </summary>
    [NonSerialized] public DSRepository repository = new DSRepository();
    /// <summary>
    /// Key为菜单路径
    /// Value为创建Node的模板元素，也可以考虑绑定VisualElement，从而实现不同样式。
    /// </summary>
    [NonSerialized] public readonly LinkedDictionary<string, DSNamedType> templates = new();

    /// <summary>
    /// NodeList
    ///
    /// 注：
    /// 1.部分Node是不保存到资产的，Node也不全存在于GraphView。
    /// 2.避免直接修改List，请通过封装的数组相关方法操作。
    /// </summary>
    public List<NodeData> nodeList = new List<NodeData>();
    /// <summary>
    /// 当前的所有Node(保持插入序)
    /// </summary>
    [NonSerialized]
    public readonly LinkedDictionary<long, NodeData> nodeDic = new();

    /// <summary>
    /// 用于分配LocalId（需要和已存在的Node去重）
    /// </summary>
    private long _nextLocalId;
    /// <summary>
    /// 当前工作目录，GraphView只展示当前Folder的Node
    /// </summary>
    private string currentFolder;

    /// <summary>
    /// Dson文本生成设置
    /// </summary>
    public DsonTextWriterSettings writerSettings = (DsonTextWriterSettings)new DsonTextWriterSettings.Builder()
    {
        NumberStyle = NumberStyles.Simple
    }.Build();

    private readonly ObjectPool<List<DSField>> _fieldListCache = ObjectPoolUtil.NewListPool<DSField>(8);
    private readonly ObjectPool<List<DSTypeElement>> _typeListCache = ObjectPoolUtil.NewListPool<DSTypeElement>(2);
    private readonly StringBuilder _sbCache = new(16);

    /// <summary>
    /// 
    /// </summary>
    private void OnEnable() {
        // TODO
    }

    /// <summary>
    /// 该方法在Undo/Redo执行以后调用，用于修复数据
    /// </summary>
    public void OnUndoRedoPerformed() {
        // 删除不在使用的引用
        using var enumerator = nodeDic.GetEnumerator();
        while (enumerator.MoveNext()) {
            var pair = enumerator.Current;
            if (!nodeList.Contains(pair.Value)) {
                enumerator.Remove();
            }
        }
        // 找回丢失的引用(Redo加回的引用)
        foreach (NodeData nodeData in nodeList) {
            if (nodeData) {
                nodeDic[nodeData.localId] = nodeData;
            }
        }
        // 修复Variable数据
        foreach (NodeData nodeData in nodeList) {
            RepairNode(nodeData);
        }
    }

    #region NODE

    /// <summary>
    /// 添加Node
    /// </summary>
    /// <param name="nodeData"></param>
    public void AddNode(NodeData nodeData) {
        if (_nextLocalId <= nodeData.localId) {
            _nextLocalId = nodeData.localId + 1;
        }
        nodeList.Add(nodeData);
        nodeDic[nodeData.localId] = nodeData;
    }

    /// <summary>
    /// 删除node
    ///
    /// 注：此时不会销毁Node，否则会导致无法执行Undo
    /// </summary>
    public NodeData DeleteNode(long localId) {
        if (!nodeDic.TryGetValue(localId, out NodeData node)) {
            return null;
        }
        DisconnectAll(node);
        nodeDic.Remove(localId);
        nodeList.Remove(node);
        return node;
    }

    /// <summary>
    /// 创建Node
    ///
    /// 注：该方法只负责一些公共的初始化工作，不会立即加入到集合。
    /// </summary>
    /// <param name="namedType">node的初始类型</param>
    /// <returns></returns>
    public NodeData CreateNode(DSNamedType namedType = null) {
        NodeData nodeData = ScriptableObject.CreateInstance<NodeData>();
        nodeData.localId = ++_nextLocalId;
        if (namedType != null) {
            nodeData.value = CreateVariable(namedType);
        }
        nodeData.UpdateProperties();
        return nodeData;
    }

    /// <summary>
    /// 断开Node的所有连接（通常用在删除前）
    /// </summary>
    /// <param name="nodeData"></param>
    /// <returns>断开连接的变量</returns>
    public List<Variable> DisconnectAll(NodeData nodeData) {
        List<Variable> list = new List<Variable>(nodeData.inputFields.Count + nodeData.outputFields.Count);
        list.AddRange(nodeData.inputFields);
        list.AddRange(nodeData.outputFields);
        foreach (Variable variable in list) {
            ResetVariable(variable); // 重置数据即可断开连接 - 字段的类型应当为ObjectPath
        }
        nodeData.inputFields.Clear(); // input清理
        return list;
    }

    /// <summary>
    /// 初始化Node上关联的Port字段
    /// 
    /// 注：
    /// 1.会递归所有的静态路径字段，并将标记有PortField注解的字段转换为ObjectPath类型。
    /// 2.该方法应当在决定显示Node前调用，不显示在GraphView中的Node（或者不需要通过连线配置数据）的对象可不调用
    /// </summary>
    public void InitOutputFields(NodeData nodeData) {
        nodeData.outputFields.Clear();
        if (nodeData.value == null || !nodeData.enablePort) {
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
        List<DSField> fields = varType.GetFields(true, _fieldListCache.Acquire());
        variable.values = new List<Variable>(fields.Count);
        foreach (DSField field in fields) {
            variable.Add(CreateVariable(field));
        }
        _fieldListCache.Release(fields);
    }

    /// <summary>
    /// 切换变量的数据结构类型
    /// </summary>
    /// <param name="variable"></param>
    /// <param name="newType">新类型</param>
    /// <param name="inheritData">是否继承当前数据</param>
    /// <returns>是否切换了类型</returns>
    public bool ChangeVariableType(Variable variable, DSNamedType newType, bool inheritData = true) {
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
    /// 重置变量
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

        List<DSTypeElement> list = _typeListCache.Acquire();
        list.Add(keysField.Type);
        list.Add(valuesField.Type);

        DSNamedType pairType = repository.GetBuiltinType(DSKeywords.TYPE_PAIR);
        pairType = repository.MakeGenericType(pairType, list);
        _typeListCache.Release(list);
        return pairType;
    }

    /// <summary>
    /// 拷贝变量
    /// (通常只应该集合视图调用)
    /// </summary>
    /// <param name="variable"></param>
    /// <returns></returns>
    public Variable Duplicate(Variable variable) {
        DsonValue dsonValue = Encode(variable);
        Variable newVariable = CreateVariable(variable.defineInfo, variable.type, variable.cfg);
        Decode(newVariable, dsonValue); // 虽然这样写可能创建不必要的中间对象，但代码最容易维护
        return newVariable;
    }

    /// <summary>
    /// 拷贝变量N次
    /// (批量拷贝可以减少中间对象)
    /// </summary>
    /// <param name="variable"></param>
    /// <param name="count"></param>
    /// <param name="outList"></param>
    /// <returns></returns>
    public void Duplicate(Variable variable, int count, List<Variable> outList) {
        DsonValue dsonValue = Encode(variable);
        for (int i = 0; i < count; i++) {
            Variable newVariable = CreateVariable(variable.defineInfo, variable.type, variable.cfg);
            Decode(newVariable, dsonValue);
            outList.Add(newVariable);
        }
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
    private static DSNamedType GetDeclaredType(DSElement element) {
        return element is DSField field ? (DSNamedType)field.Type : (DSNamedType)element;
    }

    #endregion

    #region 序列化

    /// <summary>
    /// 修复Node的缓存数据
    /// 
    /// 注意：只能修复逻辑层数据，不能修复显示层对象，需要重新构建GraphView。
    /// </summary>
    /// <param name="node"></param>
    public void RepairNode(NodeData node) {
        if (node.value == null) return;
        node.serializedObject.Update(); // 奇怪？序列化层的数组长度不匹配
        node.value.UnbindValuesProperty(); // 此过程不能记录操作，否则会覆盖Undo记录
        RepairVariable(node.value);
        node.value.RebindValuesProperty();
    }

    /// <summary>
    /// 修复Variable的缓存数据
    /// </summary>
    /// <param name="variable">入口必须是Root对象</param>
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
        List<DSField> fields = variable.type.GetFields(true, _fieldListCache.Acquire());
        for (int index = 0; index < variable.values.Count; index++) {
            Variable nestedVar = variable.values[index];
            if (nestedVar == null) {
                throw new AssertionError();
            }
            nestedVar.defineInfo = fields[index];
            RepairVariable(nestedVar);
        }
        _fieldListCache.Release(fields);
    }

    /// <summary>
    /// 将DsonValue赋值给当前变量（反序列化）
    ///
    /// 1.对于集合类型字段，默认会清空当前所有数据，再填充数据。
    /// 2.对于自定义结构，只赋值（覆盖）DsonValue中存在的字段。
    /// 3.该接口通常只应该在反序列化、切换Node或多态字段绑定的数据类型时调用（将既有数据赋值给新对象）。
    /// </summary>
    /// <param name="variable"></param>
    /// <param name="dsonValue"></param>
    public void Decode(Variable variable, DsonValue dsonValue) {
        DSNamedType varType = variable.type;
        if (dsonValue.DsonType == DsonType.Null) {
            ResetVariable(variable);
            variable.isNull = true;
            return;
        }
        variable.isNull = false;
        // 原子值
        switch (varType.SimpleName) {
            case DSKeywords.TYPE_INT32:
            case DSKeywords.TYPE_INT64: {
                if (dsonValue.DsonType == DsonType.String) { // 可能是字典的key
                    if (long.TryParse(dsonValue.AsString(), out long value)) {
                        variable.longValue = value;
                    }
                } else if (dsonValue.IsNumber) {
                    variable.longValue = dsonValue.AsNumber().LongValue;
                }
                return;
            }
            case DSKeywords.TYPE_FLOAT:
            case DSKeywords.TYPE_DOUBLE: {
                if (dsonValue.DsonType == DsonType.String) {
                    if (double.TryParse(dsonValue.AsString(), out double value)) {
                        variable.doubleValue = value;
                    }
                } else if (dsonValue.IsNumber) {
                    variable.doubleValue = dsonValue.AsNumber().DoubleValue;
                }
                return;
            }
            case DSKeywords.TYPE_BOOL: {
                if (dsonValue.DsonType == DsonType.String) {
                    variable.boolValue = dsonValue.AsString() == "true"; // 不测试0和1
                } else if (dsonValue.IsNumber) {
                    variable.boolValue = dsonValue.AsNumber().IntValue != 0;
                }
                return;
            }
            case DSKeywords.TYPE_STRING: {
                variable.stringValue = dsonValue.DsonType switch
                {
                    DsonType.Int32 => dsonValue.AsInt32().ToString(),
                    DsonType.Int64 => dsonValue.AsInt64().ToString(),
                    DsonType.Float => dsonValue.AsFloat().ToString(CultureInfo.InvariantCulture),
                    DsonType.Double => dsonValue.AsDouble().ToString(CultureInfo.InvariantCulture),
                    DsonType.String => dsonValue.AsString(),
                    DsonType.Binary => dsonValue.AsBinary().ToHexString(),
                    _ => null
                };
                return;
            }
            case DSKeywords.TYPE_BYTES: {
                // Binary可以安全转String，但String不能安全转Binary
                if (dsonValue.DsonType == DsonType.Binary) {
                    variable.stringValue = dsonValue.AsBinary().ToHexString();
                }
                return;
            }
        }
        // Enum
        if (varType.Kind == DSElementKind.Enum) {
            if (dsonValue.DsonType == DsonType.String) { // 可能是字典的key
                string stringValue = dsonValue.AsString();
                if (int.TryParse(stringValue, out int intValue)) {
                    variable.longValue = intValue;
                } else {
                    DSEnumValue enumValue = varType.GetEnumValue(stringValue, true);
                    variable.longValue = enumValue == null ? 0 : enumValue.Number;
                }
            } else if (dsonValue.IsNumber) {
                variable.longValue = dsonValue.AsNumber().IntValue;
            }
            return;
        }

        // DateTime
        VariableCfg variableCfg = variable.cfg;
        if (DSUtil.IsDateTimeType(varType) || DSUtil.IsTimestampType(varType)
                                           || variableCfg.dsonType == DsonType.DateTime
                                           || variableCfg.dsonType == DsonType.Timestamp) {
            if (dsonValue.DsonType == DsonType.DateTime) {
                ExtDateTime dateTime = dsonValue.AsDateTime();
                variable[0].longValue = dateTime.Seconds;
                variable[1].longValue = dateTime.Nanos;
            } else if (dsonValue.DsonType == DsonType.Timestamp) {
                Timestamp timestamp = dsonValue.AsTimestamp();
                variable[0].longValue = timestamp.Seconds;
                variable[1].longValue = timestamp.Nanos;
            }
            return;
        }
        // ObjectPtr
        if (variableCfg.dsonType == DsonType.Pointer) {
            if (dsonValue.DsonType == DsonType.Pointer) {
                ObjectPtr objectPtr = dsonValue.AsPointer();
                variable[0].stringValue = objectPtr.Collection;
                variable[1].stringValue = objectPtr.LocalPath;
                variable[2].longValue = objectPtr.LocalId;
                variable[3].longValue = objectPtr.Type;
            }
            return;
        }
        // Nullable
        if (DSUtil.IsNullableType(varType)) {
            // ResetVariable(variable); // 强制清理，确保正确覆盖 - Nullable的路径是稳定的，可不清理
            Decode(variable[0], dsonValue);
            return;
        }
        // 集合 - 不支持导入Object，无法为key创建定义
        if (DSUtil.IsCollectionType(varType)) {
            variable.ClearArray(); // 强制清理，确保正确覆盖
            if (dsonValue.DsonType != DsonType.Array) {
                return;
            }
            DsonArray<string> dsonArray = dsonValue.AsArray();
            variable.values.EnsureCapacity(dsonArray.Count);
            foreach (DsonValue nestValue in dsonArray) {
                Variable nestedVar = CreateListItem(variable);
                Decode(nestedVar, nestValue);
                variable.Add(nestedVar);
            }
            return;
        }
        // 字典 - 从DsonArray恢复时，可能出现兼容问题
        if (DSUtil.IsMapType(varType)) {
            variable.ClearArray(); // 强制清理，确保正确覆盖
            if (dsonValue.DsonType == DsonType.Object) {
                DsonObject<string> dsonObject = dsonValue.AsObject();
                variable.values.EnsureCapacity(dsonObject.Count);
                foreach (var pair in dsonObject) {
                    Variable varPair = CreateMapItem(variable);
                    Decode(varPair[0], new DsonString(pair.Key));
                    Decode(varPair[1], pair.Value);
                    variable.Add(varPair);
                }
            } else if (dsonValue.DsonType == DsonType.Array) {
                DsonArray<string> dsonArray = dsonValue.AsArray();
                variable.values.EnsureCapacity(dsonArray.Count / 2);
                for (int index = 0; index < dsonArray.Count; index += 2) {
                    Variable varPair = CreateMapItem(variable);
                    Decode(varPair[0], dsonArray[index]);
                    Decode(varPair[1], dsonArray[index + 1]);
                    variable.Add(varPair);
                }
            }
            return;
        }
        // 自定义结构，按照字段名进行匹配，选择性覆盖
        if (dsonValue.DsonType == DsonType.Object) {
            DsonObject<string> dsonObject = dsonValue.AsObject();
            foreach (Variable nestedVar in variable.values) {
                string fieldName = nestedVar.defineInfo.SimpleName;
                if (!dsonObject.TryGetValue(fieldName, out DsonValue fieldValue)) {
                    continue;
                }
                Decode(nestedVar, fieldValue);
            }
        }
    }

    /// <summary>
    /// 将内存中的对象导出为Dson对象（序列化）
    ///
    /// 注；Node调用该方法后，应当总是追加类型信息到Header。
    /// </summary>
    /// <param name="variable"></param>
    /// <returns></returns>
    public DsonValue Encode(Variable variable) {
        DSNamedType varType = variable.type;
        if (variable.isNull) {
            return DsonNull.NULL;
        }
        // 原子值
        switch (varType.SimpleName) {
            case DSKeywords.TYPE_INT32: return new DsonInt32(variable.intValue);
            case DSKeywords.TYPE_INT64: return new DsonInt64(variable.longValue);
            case DSKeywords.TYPE_FLOAT: return new DsonFloat(variable.floatValue);
            case DSKeywords.TYPE_DOUBLE: return new DsonDouble(variable.doubleValue);
            case DSKeywords.TYPE_BOOL: return new DsonBool(variable.boolValue);
            case DSKeywords.TYPE_STRING: {
                string stringValue = variable.stringValue;
                if (string.IsNullOrEmpty(stringValue)) {
                    return DsonString.EMPTY;
                }
                return new DsonString(stringValue);
            }
            case DSKeywords.TYPE_BYTES: {
                string stringValue = variable.stringValue;
                if (string.IsNullOrWhiteSpace(stringValue)) {
                    return DsonBinary.EMPTY;
                }
                stringValue = ObjectUtil.DeleteWhitespace(stringValue);
                return new DsonBinary(Binary.FromHexString(stringValue));
            }
        }
        // Enum 固定导出为数字
        if (varType.Kind == DSElementKind.Enum) {
            return new DsonInt32(variable.intValue);
        }
        // 测试类型的投影类型
        // DateTime
        VariableCfg variableCfg = GetVariableCfg(varType);
        if (variableCfg.dsonType == DsonType.DateTime || DSUtil.IsDateTimeType(varType)) {
            long seconds = variable[0].longValue;
            int nanos = variable[1].intValue;
            return new DsonDateTime(new ExtDateTime(seconds, nanos));
        }
        if (variableCfg.dsonType == DsonType.Timestamp || DSUtil.IsTimestampType(varType)) {
            long seconds = variable[0].longValue;
            int nanos = variable[1].intValue;
            return new DsonTimestamp(new Timestamp(seconds, nanos));
        }
        // ObjectPtr
        if (variableCfg.dsonType == DsonType.Pointer || DSUtil.IsPointerType(varType)) {
            string collection = variable[0].stringValue;
            string localPath = variable[1].stringValue;
            long localId = variable[2].longValue;
            int type = variable[3].intValue;
            return new DsonPointer(new ObjectPtr(collection, localPath, localId, type));
        }
        // Nullable - 导出时拆箱
        if (DSUtil.IsNullableType(varType)) {
            return Encode(variable[0]);
        }
        // 普通集合
        if (DSUtil.IsCollectionType(varType)) {
            return EncodeCollectionAsDsonArray(variable);
        }
        // Map
        if (DSUtil.IsMapType(varType)) {
            // 如果是标准字典类型(key为int32、int64、string)，则导出为DsonObject
            DSTypeElement keyType = varType.TypeArguments[0];
            if (keyType.SimpleName == DSKeywords.TYPE_INT32
                || keyType.SimpleName == DSKeywords.TYPE_INT64
                || keyType.Kind == DSElementKind.Enum) {
                return EncodeMapAsDsonObject(variable, false);
            }
            if (keyType.SimpleName == DSKeywords.TYPE_STRING) {
                return EncodeMapAsDsonObject(variable, true);
            }
            return EncodeMapAsDsonArray(variable);
        }
        // 普通结构，导出为DsonObject
        return EncodeStructAsDsonObject(variable);
    }

    private DsonValue EncodeStructAsDsonObject(Variable variable) {
        DsonObject<string> dsonObject = new DsonObject<string>(variable.Count);
        foreach (Variable nestedVar in variable.values) {
            DsonValue dsonValue = Encode(nestedVar);
            if (dsonValue.DsonType == DsonType.Null) {
                continue;
            }
            DSNamedType fieldDeclaredType = GetDeclaredType(nestedVar.defineInfo);
            WriteClassNameHeader(fieldDeclaredType, nestedVar.type, dsonValue);
            //
            string fieldName = nestedVar.defineInfo.SimpleName;
            dsonObject[fieldName] = dsonValue;
        }
        return dsonObject;
    }

    private DsonValue EncodeCollectionAsDsonArray(Variable variable) {
        DSNamedType valueDeclaredType = (DSNamedType)variable.type.TypeArguments[0];
        DsonArray<string> dsonArray = new DsonArray<string>(variable.Count);
        foreach (Variable nestedVar in variable.values) {
            DsonValue dsonValue = Encode(nestedVar);
            WriteClassNameHeader(valueDeclaredType, nestedVar.type, dsonValue);
            dsonArray.Add(dsonValue);
        }
        return dsonArray;
    }

    private DsonArray<string> EncodeMapAsDsonArray(Variable variable) {
        // 我们暂时认为Key都不是多态的
        // DSNamedType keyDeclaredType = (DSNamedType)variable.type.TypeArguments[0];
        DSNamedType valueDeclaredType = (DSNamedType)variable.type.TypeArguments[1];
        DsonArray<string> dsonArray = new DsonArray<string>(variable.Count);
        foreach (Variable varPair in variable.values) {
            Variable varKey = varPair[0];
            Variable varValue = varPair[1];
            //
            DsonValue dsonK = Encode(varKey);
            DsonValue dsonV = Encode(varValue);
            WriteClassNameHeader(valueDeclaredType, varValue.type, dsonV);
            dsonArray.Add(dsonK);
            dsonArray.Add(dsonV);
        }
        return dsonArray;
    }

    private DsonObject<string> EncodeMapAsDsonObject(Variable variable, bool isStringKey) {
        DSNamedType valueDeclaredType = (DSNamedType)variable.type.TypeArguments[1];
        DsonObject<string> dsonObject = new DsonObject<string>(variable.Count / 2);
        foreach (Variable varPair in variable.values) {
            Variable varKey = varPair[0];
            Variable varValue = varPair[1];
            //
            DsonValue dsonValue = Encode(varValue);
            WriteClassNameHeader(valueDeclaredType, varValue.type, dsonValue);
            if (isStringKey) {
                Debug.Assert(varKey.stringValue != null, "key.stringValue == null");
                dsonObject[varKey.stringValue] = dsonValue; // key不能是null
            } else {
                dsonObject[varKey.longValue.ToString()] = dsonValue;
            }
        }
        return dsonObject;
    }

    /// <summary>
    /// 注：顶层对象不仅要写clsName还需要写localId和localPath
    /// </summary>
    /// <param name="declaredType">变量的声明类型</param>
    /// <param name="varType">变量的真实类型</param>
    /// <param name="exportedValue">变量导出的DsonValue</param>
    private void WriteClassNameHeader(DSNamedType declaredType, DSNamedType varType, DsonValue exportedValue) {
        if (exportedValue.DsonType != DsonType.Object && exportedValue.DsonType != DsonType.Array) {
            return;
        }
        if (declaredType.IsValueType || Equals(varType, declaredType)) {
            return;
        }
        StringBuilder clsName = varType.DsonTypeName.ToString(_sbCache.Clear());
        if (exportedValue is DsonObject<string> dsonObject) {
            dsonObject.Header[DsonHeader.Names_ClassName] = new DsonString(clsName.ToString());
            // dsonObject.Header[DsonHeader.Names_Count] = new DsonInt32(dsonObject.Count);
        } else {
            DsonArray<string> dsonArray = exportedValue.AsArray();
            dsonArray.Header[DsonHeader.Names_ClassName] = new DsonString(clsName.ToString());
            // dsonArray.Header[DsonHeader.Names_Count] = new DsonInt32(dsonArray.Count);
        }
    }

    #endregion

    private class NodeIndexHelper : IIndexedElementHelper<NodeData>
    {
        public static NodeIndexHelper Inst { get; } = new NodeIndexHelper();

        public int CollectionIndex(object collection, NodeData element) {
            return element.qIndex;
        }

        public void CollectionIndex(object collection, NodeData element, int index) {
            element.qIndex = index;
        }
    }
}
}