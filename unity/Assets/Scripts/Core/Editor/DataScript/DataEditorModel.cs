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
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

namespace Wjybxx.BigCat.CoreEditor
{
/// <summary>
/// 数据编辑器数据模型
///
/// 注：继承Unity的Object以使用<see cref="SerializedObject"/>提供的Redo和Undo功能，
/// 但该对象只应该存在于内存中，不应该被保存为资产对象。
/// 
/// 注：
/// 1.主要负责<see cref="DataNode"/>和<see cref="DataVariable"/>的创建，和数据修复工作。
/// 2.对于字典类型，如果Key是Int32、Int64、String类型（即标准Map），则导出为DsonObject，否则导出为DsonArray。
/// </summary>
public class DataEditorModel : ScriptableObject
{
    /// <summary>
    /// 编辑器关联的DataScript仓库
    /// </summary>
    [NonSerialized] public DSRepository repository = new DSRepository();
    /// <summary>
    /// Key为菜单路径
    /// Value为创建Node的模板元素
    /// </summary>
    [NonSerialized] public readonly LinkedDictionary<string, DSNamedType> templates = new();

    /// <summary>
    /// 所有需要序列化的Node
    ///
    /// 注：该字段用于Unity的序列化保存。
    /// </summary>
    public List<DataNode> serializedNodes = new();
    /// <summary>
    /// 当前的所有Nodes，部分Node是不序列化的。
    ///
    /// 注：该List可以用于迭代，因为添加和删除是在循环外处理的。
    /// </summary>
    [NonSerialized]
    public List<DataNode> nodeList = new();
    /// <summary>
    /// 当前的所有Node(保持插入序)
    /// </summary>
    [NonSerialized]
    public readonly LinkedDictionary<long, DataNode> nodeDic = new();
    /// <summary>
    /// 用于实现Undo和Redo
    /// </summary>
    private SerializedObject _serializedObject;

    /// <summary>
    /// Dson文本生成设置
    /// </summary>
    public DsonTextWriterSettings writerSettings = (DsonTextWriterSettings)new DsonTextWriterSettings.Builder()
    {
        NumberStyle = NumberStyles.Simple
    }.Build();

    private readonly ObjectPool<List<DSField>> _fieldListCache = ObjectPoolUtil.NewListPool<DSField>(4);
    private readonly StringBuilder _sbCache = new(16);

    /// <summary>
    /// 
    /// </summary>
    public void OnEnable() {
        // TODO
    }


    /// <summary>
    /// 添加Node
    /// </summary>
    /// <param name="node"></param>
    public void AddNode(DataNode node) {
        nodeList.Add(node);
        nodeDic[node.localId] = node;
    }

    #region NODE

    /// <summary>
    /// 创建Node
    /// </summary>
    /// <param name="position">node的初始坐标</param>
    /// <param name="namedType">node的初始类型</param>
    /// <returns></returns>
    public DataNode CreateNode(Rect position, DSNamedType namedType = null) {
        DataNode node = new DataNode();
        node.localId = 0;
        node.position = position;
        if (namedType != null) {
            node.value = CreateVariable(namedType);
        }
        AddNode(node);
        return node;
    }

    /// <summary>
    /// 重置Node的数据，所有指清理为默认值
    /// </summary>
    public void ResetNode(DataNode node) {
        DisconnectPorts(node); // Port是双向绑定的，因此需要先解绑
        ResetVariable(node.value);
    }

    /// <summary>
    /// 切换Node绑定的数据结构
    ///
    /// 注：切换Node时保存数据可能不太符合直觉。
    /// </summary>
    /// <param name="node"></param>
    /// <param name="newType">目标类型，可以为null</param>
    /// <param name="inheritData">是否继承当前数据</param>
    public void ChangeNodeType(DataNode node, DSNamedType newType, bool inheritData = true) {
        if (newType == null) {
            node.value = null;
            return;
        }
        DataVariable value = node.value;
        if (value == null) {
            node.value = CreateVariable(newType);
            return;
        }
        if (!ChangeVariableType(value, newType, inheritData)) {
            return;
        }
        // TODO 收集所有port
        node.ports.Clear();
    }

    /// <summary>
    /// 断开Node发起的Port连接(除Top区域)
    /// </summary>
    /// <param name="node"></param>
    private void DisconnectPorts(DataNode node) {
        // TODO
    }

    #endregion


    #region variable

    /// <summary>
    /// 创建变量
    /// </summary>
    public DataVariable CreateVariable(DSElement defineInfo, DataDisplayCfg displayCfg = null) {
        return CreateVariable(defineInfo, GetDeclaredType(defineInfo), displayCfg);
    }

    /// <summary>
    /// 创建变量
    /// 
    /// 1.此时可能尚未完成初始化，因此尚不绑定Drawer
    /// 2.多态类型的字段，在创建时应当传入目标类型
    /// 3.集合创建元素变量时，应当传入元素的展示配置
    /// </summary>
    /// <param name="defineInfo">变量的定义信息</param>
    /// <param name="type">变量的初始类型</param>
    /// <param name="displayCfg">变量的展示配置</param>
    /// <returns></returns>
    public DataVariable CreateVariable(DSElement defineInfo, DSNamedType type, DataDisplayCfg displayCfg = null) {
        displayCfg ??= GetDisplayCfg(defineInfo);
        DataVariable variable = new DataVariable
        {
            defineInfo = defineInfo,
            displayCfg = displayCfg,
            type = type ?? throw new ArgumentNullException(nameof(type)),
            isNull = displayCfg.initNull || DSUtil.IsNullableType(type)
        };
        if (!variable.isNull) {
            CreateValues(variable);
        }
        return variable;
    }

    /// <summary>
    /// 创建变量的Values
    ///
    /// 注：创建变量的Values意味着变量不再为null。
    /// </summary>
    /// <param name="variable"></param>
    public void CreateValues(DataVariable variable) {
        DSNamedType varType = variable.type;
        if (DSUtil.IsAtomicType(varType)) {
            return;
        }
        if (DSUtil.IsCollectionOrMapType(varType)) {
            variable.values = new List<DataVariable>();
            return;
        }
        if (DSUtil.IsNullableType(varType)) {
            DSField valueField = variable.type.GetField("value")!;
            DataDisplayCfg elementCfg = variable.displayCfg.elementCfg;
            DataVariable value = CreateVariable(valueField, (DSNamedType)valueField.Type, elementCfg);
            variable.values = new List<DataVariable>(1) { value };
            variable.isNull = false;
            return;
        }
        // 递归创建Value
        List<DSField> fields = varType.GetFields(true, _fieldListCache.Acquire());
        variable.values = new List<DataVariable>(fields.Count);
        variable.isNull = false;
        foreach (DSField field in fields) {
            variable.values.Add(CreateVariable(field, (DSNamedType)field.Type));
        }
        _fieldListCache.Release(fields);
    }

    /// <summary>
    /// 重置变量
    ///
    /// 注：Nullable类型会自动置为null，其它结构需手动指定。
    /// </summary>
    /// <param name="variable">要重置的变量</param>
    /// <param name="resetNull">是否重置为null</param>
    public void ResetVariable(DataVariable variable, bool resetNull = false) {
        if (variable == null) return;
        variable.longValue = 0;
        variable.doubleValue = 0;
        variable.stringValue = null;
        if (variable.values == null) {
            return;
        }
        // List和字典直接清空
        if (DSUtil.IsCollectionOrMapType(variable.type)) {
            variable.values.Clear();
            return;
        }
        // Nullable固定重置为null
        if (DSUtil.IsNullableType(variable.type)) {
            variable.values = null;
            variable.isNull = true;
            return;
        }
        // 普通Object由用户选择是否重置为null
        if (resetNull) {
            variable.values = null;
            variable.isNull = true;
        } else {
            foreach (DataVariable nestValue in variable.values) {
                ResetVariable(nestValue);
            }
        }
    }

    /// <summary>
    /// 重置变量为指定值
    /// </summary>
    /// <param name="variable"></param>
    /// <param name="dsonValue"></param>
    public void ResetVariable(DataVariable variable, DsonValue dsonValue) {
        if (dsonValue.DsonType == DsonType.Null) {
            ResetVariable(variable, true);
            return;
        }
        if (variable.isNull) {
            CreateValues(variable);
        } else {
            ResetVariable(variable);
        }
        Decode(variable, dsonValue);
    }

    /// <summary>
    /// 拷贝变量
    /// (通常只应该集合视图调用)
    /// </summary>
    /// <param name="variable"></param>
    /// <returns></returns>
    public DataVariable Duplicate(DataVariable variable) {
        DsonValue dsonValue = Encode(variable);
        DataVariable newVariable = CreateVariable(variable.defineInfo, variable.type, variable.displayCfg);
        ResetVariable(newVariable, dsonValue); // 虽然这样写可能创建不必要的中间对象，但代码最容易维护
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
    public void Duplicate(DataVariable variable, int count, List<DataVariable> outList) {
        DsonValue dsonValue = Encode(variable);
        for (int i = 0; i < count; i++) {
            DataVariable newVariable = CreateVariable(variable.defineInfo, variable.type, variable.displayCfg);
            ResetVariable(newVariable, dsonValue);
            outList.Add(newVariable);
        }
    }

    /// <summary>
    /// 切换变量的数据结构类型
    /// </summary>
    /// <param name="variable"></param>
    /// <param name="newType">新类型</param>
    /// <param name="inheritData">是否继承当前数据</param>
    /// <returns>是否切换了类型</returns>
    public bool ChangeVariableType(DataVariable variable, DSNamedType newType, bool inheritData = true) {
        if (Equals(variable.type, newType)) {
            return false;
        }
        DsonValue dsonValue = inheritData ? Encode(variable) : null;
        ResetVariable(variable);
        // 按照新类型再初始化
        variable.type = newType;
        CreateValues(variable);
        if (inheritData) {
            Decode(variable, dsonValue);
        }
        return true;
    }

    /// <summary>
    /// 获取类型的编辑器配置
    /// 
    /// 注：不可以修改返回对象的数据。
    /// </summary>
    /// <param name="element"></param>
    public DataDisplayCfg GetDisplayCfg(DSElement element) {
        DSElement originDefine = element.OriginDefine;
        if (originDefine.editorContext == null) {
            originDefine.editorContext = DataDisplayCfg.Parse(originDefine);
            // 匹配关联的实例 - 只匹配顶层类型
            if (originDefine.Kind.IsNamedType()
                && !DSUtil.IsAtomicType(originDefine)
                && originDefine.EnclosingElement.Kind == DSElementKind.File) {
                InitSupportedInsts((DSNamedType)originDefine);
            }
        }
        return (DataDisplayCfg)originDefine.editorContext;
    }

    private void InitSupportedInsts(DSNamedType namedType) {
        DataDisplayCfg displayCfg = (DataDisplayCfg)namedType.editorContext;
        foreach (DSFile dsFile in repository.FileMap.Values) {
            foreach (var pair in dsFile.InstMap) {
                if (!MatchInstName(namedType, pair.Key)) {
                    continue;
                }
                displayCfg.supportedInsts ??= new List<DSInst>();
                displayCfg.supportedInsts.Add(pair.Value);
            }
        }
        displayCfg.supportedInsts?.TrimExcess();
    }

    private static bool MatchInstName(DSNamedType namedType, string instName) {
        string typeName = namedType.SimpleName;
        if (instName == typeName) return true;
        // inst MyClass/A {}
        return instName.StartsWith(typeName) && instName[typeName.Length] == '/';
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
    /// 将DsonValue赋值给当前变量（反序列化）
    ///
    /// 1.对于集合类型字段，默认会清空当前所有数据，再填充数据。
    /// 2.对于自定义结构，只赋值（覆盖）DsonValue中存在的字段。
    /// 3.该接口通常只应该在反序列化、切换Node或多态字段绑定的数据类型时调用（将既有数据赋值给新对象）。
    /// </summary>
    /// <param name="variable"></param>
    /// <param name="dsonValue"></param>
    public void Decode(DataVariable variable, DsonValue dsonValue) {
        DSNamedType varType = variable.type;
        // 原子值
        switch (varType.SimpleName) {
            case DSKeywords.TYPE_INT32:
            case DSKeywords.TYPE_INT64: {
                if (dsonValue.DsonType == DsonType.String) { // 可能是字典的key
                    long.TryParse(dsonValue.AsString(), out variable.longValue);
                } else if (dsonValue.IsNumber) {
                    variable.longValue = dsonValue.AsNumber().LongValue;
                }
                return;
            }
            case DSKeywords.TYPE_FLOAT:
            case DSKeywords.TYPE_DOUBLE: {
                if (dsonValue.DsonType == DsonType.String) {
                    double.TryParse(dsonValue.AsString(), out variable.doubleValue);
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
                    DsonType.Int64 => dsonValue.AsInt64().ToString(), // 浮点数不能简单处理
                    DsonType.String => dsonValue.AsString(),
                    DsonType.Binary => dsonValue.AsBinary().ToString(),
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
        DataDisplayCfg displayCfg = variable.displayCfg;
        if (DSUtil.IsDateTimeType(varType) || DSUtil.IsTimestampType(varType)
                                           || displayCfg.dsonType == DsonType.DateTime
                                           || displayCfg.dsonType == DsonType.Timestamp) {
            if (dsonValue.DsonType == DsonType.DateTime) {
                ExtDateTime dateTime = dsonValue.AsDateTime();
                variable.values[0].longValue = dateTime.Seconds;
                variable.values[1].longValue = dateTime.Nanos;
            } else if (dsonValue.DsonType == DsonType.Timestamp) {
                Timestamp timestamp = dsonValue.AsTimestamp();
                variable.values[0].longValue = timestamp.Seconds;
                variable.values[1].longValue = timestamp.Nanos;
            }
            return;
        }
        // ObjectPtr
        if (displayCfg.dsonType == DsonType.Pointer) {
            if (dsonValue.DsonType == DsonType.Pointer) {
                ObjectPtr objectPtr = dsonValue.AsPointer();
                variable.values[0].stringValue = objectPtr.Collection;
                variable.values[1].stringValue = objectPtr.LocalPath;
                variable.values[2].longValue = objectPtr.LocalId;
                variable.values[3].longValue = objectPtr.Type;
            }
            return;
        }
        // Nullable
        if (DSUtil.IsNullableType(varType)) {
            ResetVariable(variable); // 强制清理，确保正确覆盖
            if (dsonValue.DsonType == DsonType.Null) {
                return;
            }
            CreateValues(variable);
            Decode(variable.values[0], dsonValue);
            return;
        }
        // 集合
        if (DSUtil.IsCollectionType(varType)) {
            ResetVariable(variable); // 强制清理，确保正确覆盖
            if (dsonValue.DsonType != DsonType.Array) { // 不支持导入Object，无法为key创建定义
                return;
            }
            DSField valuesField = varType.GetField("values")!;
            DsonArray<string> dsonArray = dsonValue.AsArray();
            variable.values.EnsureCapacity(dsonArray.Count);
            foreach (DsonValue nestValue in dsonArray) {
                DataVariable nestedVar = CreateVariable(valuesField, variable.displayCfg);
                Decode(nestedVar, nestValue);
                variable.values.Add(nestedVar);
            }
            return;
        }
        if (DSUtil.IsMapType(varType)) {
            ResetVariable(variable); // 强制清理，确保正确覆盖
            if (dsonValue.DsonType != DsonType.Object) { // 不支持导入Array，无法保证Key的兼容性...
                return;
            }
            DSField keysField = varType.GetField("keys")!;
            DSField valuesField = varType.GetField("values")!;
            DsonObject<string> dsonObject = dsonValue.AsObject();
            variable.values.EnsureCapacity(dsonObject.Count * 2);
            foreach (var pair in dsonObject) {
                DataVariable keyVar = CreateVariable(keysField, variable.displayCfg);
                Decode(keyVar, new DsonString(pair.Key));
                variable.values.Add(keyVar);
                //
                DataVariable valueVar = CreateVariable(valuesField, variable.displayCfg);
                Decode(valueVar, pair.Value);
                variable.values.Add(valueVar);
            }
            return;
        }
        // 自定义结构，按照字段名进行匹配，选择性覆盖
        if (dsonValue.DsonType == DsonType.Null) {
            ResetVariable(variable, true);
        } else if (dsonValue.DsonType == DsonType.Object) {
            DsonObject<string> dsonObject = dsonValue.AsObject();
            foreach (DataVariable nestedVar in variable.values) {
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
    public DsonValue Encode(DataVariable variable) {
        DSNamedType varType = variable.type;
        // 原子值
        switch (varType.SimpleName) {
            case DSKeywords.TYPE_INT32: return new DsonInt32(variable.intValue);
            case DSKeywords.TYPE_INT64: return new DsonInt64(variable.longValue);
            case DSKeywords.TYPE_FLOAT: return new DsonFloat(variable.floatValue);
            case DSKeywords.TYPE_DOUBLE: return new DsonDouble(variable.doubleValue);
            case DSKeywords.TYPE_BOOL: return new DsonBool(variable.boolValue);
            case DSKeywords.TYPE_STRING: {
                string stringValue = variable.stringValue;
                if (variable.stringValue == null) {
                    return DsonNull.NULL;
                }
                return stringValue.Length == 0 ? DsonString.EMPTY : new DsonString(stringValue);
            }
            case DSKeywords.TYPE_BYTES: {
                string stringValue = variable.stringValue;
                if (stringValue == null) {
                    return DsonNull.NULL;
                }
                stringValue = stringValue.Trim();
                if (stringValue.Length == 0) {
                    return DsonBinary.EMPTY;
                }
                return new DsonBinary(Binary.FromHexString(stringValue));
            }
        }
        // Enum 固定导出为数字
        if (varType.Kind == DSElementKind.Enum) {
            return new DsonInt32(variable.intValue);
        }
        // 测试类型的投影类型
        // DateTime
        DataDisplayCfg displayCfg = GetDisplayCfg(varType);
        if (displayCfg.dsonType == DsonType.DateTime || DSUtil.IsDateTimeType(varType)) {
            long seconds = variable.values[0].longValue;
            int nanos = variable.values[1].intValue;
            return new DsonDateTime(new ExtDateTime(seconds, nanos));
        }
        if (displayCfg.dsonType == DsonType.Timestamp || DSUtil.IsTimestampType(varType)) {
            long seconds = variable.values[0].longValue;
            int nanos = variable.values[1].intValue;
            return new DsonTimestamp(new Timestamp(seconds, nanos));
        }
        // ObjectPtr
        if (displayCfg.dsonType == DsonType.Pointer || DSUtil.IsPointerType(varType)) {
            string collection = variable.values[0].stringValue;
            string localPath = variable.values[1].stringValue;
            long localId = variable.values[2].longValue;
            int type = variable.values[3].intValue;
            return new DsonPointer(new ObjectPtr(collection, localPath, localId, type));
        }
        // null
        if (variable.isNull) {
            return DsonNull.NULL;
        }
        // Nullable - 导出时拆箱
        if (DSUtil.IsNullableType(varType)) {
            return Encode(variable.values[0]);
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

    private DsonValue EncodeStructAsDsonObject(DataVariable variable) {
        DsonObject<string> dsonObject = new DsonObject<string>(variable.values.Count);
        foreach (DataVariable fieldValue in variable.values) {
            DsonValue dsonValue = Encode(fieldValue);
            DSNamedType fieldDeclaredType = GetDeclaredType(fieldValue.defineInfo);
            WriteClassNameHeader(fieldDeclaredType, fieldValue.type, dsonValue);
            //
            string fieldName = fieldValue.defineInfo.SimpleName;
            dsonObject[fieldName] = dsonValue;
        }
        return dsonObject;
    }

    private DsonValue EncodeCollectionAsDsonArray(DataVariable variable) {
        DSNamedType valueDeclaredType = (DSNamedType)variable.type.TypeArguments[0];
        DsonArray<string> dsonArray = new DsonArray<string>(variable.values.Count);
        foreach (DataVariable value in variable.values) {
            DsonValue dsonValue = Encode(value);
            WriteClassNameHeader(valueDeclaredType, value.type, dsonValue);
            dsonArray.Add(dsonValue);
        }
        return dsonArray;
    }

    private DsonArray<string> EncodeMapAsDsonArray(DataVariable variable) {
        // 我们暂时认为Key都不是多态的
        // DSNamedType keyDeclaredType = (DSNamedType)variable.type.TypeArguments[0];
        DSNamedType valueDeclaredType = (DSNamedType)variable.type.TypeArguments[1];
        DsonArray<string> dsonArray = new DsonArray<string>(variable.values.Count);
        for (int index = 0; index < variable.values.Count; index += 2) {
            DataVariable key = variable.values[index];
            DataVariable value = variable.values[index + 1];
            //
            DsonValue dsonK = Encode(key);
            DsonValue dsonV = Encode(value);
            WriteClassNameHeader(valueDeclaredType, value.type, dsonV);
            dsonArray.Add(dsonK);
            dsonArray.Add(dsonV);
        }
        return dsonArray;
    }

    private DsonObject<string> EncodeMapAsDsonObject(DataVariable variable, bool isStringKey) {
        DSNamedType valueDeclaredType = (DSNamedType)variable.type.TypeArguments[1];
        DsonObject<string> dsonObject = new DsonObject<string>(variable.values.Count / 2);
        for (int index = 0; index < variable.values.Count; index += 2) {
            DataVariable key = variable.values[index];
            DataVariable value = variable.values[index + 1];
            //
            DsonValue dsonValue = Encode(value);
            WriteClassNameHeader(valueDeclaredType, value.type, dsonValue);
            if (isStringKey) {
                Debug.Assert(key.stringValue != null, "key.stringValue == null");
                dsonObject[key.stringValue] = dsonValue; // key不能是null
            } else {
                dsonObject[key.longValue.ToString()] = dsonValue;
            }
        }
        return dsonObject;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="declaredType">变量的声明类型</param>
    /// <param name="varType">变量的真实类型</param>
    /// <param name="exportedValue">变量导出的DsonValue</param>
    private void WriteClassNameHeader(DSNamedType declaredType, DSNamedType varType, DsonValue exportedValue) {
        if (exportedValue.DsonType != DsonType.Object && exportedValue.DsonType != DsonType.Array) {
            return;
        }
        if (DSUtil.IsNullableType(declaredType)) { // Nullable的值无多态
            return;
        }
        if (Equals(varType, declaredType)) { // 真实类型和声明类型一致，开销较大
            return;
        }
        StringBuilder clsName = varType.DsonTypeName.ToString(_sbCache.Clear());
        if (exportedValue is DsonObject<string> dsonObject) {
            dsonObject.Header[DsonHeader.Names_ClassName] = new DsonString(clsName.ToString());
        } else {
            DsonArray<string> dsonArray = exportedValue.AsArray();
            dsonArray.Header[DsonHeader.Names_ClassName] = new DsonString(clsName.ToString());
        }
    }

    #endregion

    private class NodeIndexHelper : IIndexedElementHelper<DataNode>
    {
        public static NodeIndexHelper Inst { get; } = new NodeIndexHelper();

        public int CollectionIndex(object collection, DataNode element) {
            return element.qIndex;
        }

        public void CollectionIndex(object collection, DataNode element, int index) {
            element.qIndex = index;
        }
    }
}
}