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
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
/// 该类主要负责数据的编解码逻辑，这部分逻辑相对独立，但代码量较大。
/// </summary>
internal class DataGraphHelper
{
    private const string KEY_TYPE_SYMBOL = "typeSymbol";
    private const string KEY_NAME = "name";
    private const string KEY_FOLDER = "folder";
    private const string KEY_COMMENT = "comment";
    private const string KEY_FEATURES = "features";
    private const string KEY_POSITION = "position";

    private readonly DataGraph _graph;
    private readonly StringBuilder _sbCache = new StringBuilder();

    private readonly Dictionary<DSNamedType, string> typeSymbolCache = new();
    private readonly Dictionary<string, DSNamedType> typeSymbolResolveCache = new();

    public DataGraphHelper(DataGraph graph) {
        _graph = graph;
    }

    private string GetTypeSymbol(DSNamedType type) {
        if (typeSymbolCache.TryGetValue(type, out string symbol)) {
            return symbol;
        }
        symbol = DSUtil.ToDisplayString(type.TypeName);
        typeSymbolCache[type] = symbol;
        return symbol;
    }

    public DSNamedType ResolveTypeSymbol(string symbol) {
        if (typeSymbolResolveCache.TryGetValue(symbol, out DSNamedType type)) {
            return type;
        }
        type = (DSNamedType)_graph.repository.ResolveTypeSymbol(null, symbol);
        typeSymbolResolveCache[symbol] = type;
        return type;
    }

    private static DsonHeader<string> GetHeader(DsonValue dsonValue) {
        return dsonValue.DsonType switch
        {
            DsonType.Object => dsonValue.AsObject().Header,
            DsonType.Array => dsonValue.AsArray().Header,
            DsonType.Header => throw new InvalidOperationException("unexpected method call"),
            _ => null
        };
    }

    #region variable

    /// <summary>
    /// 将DsonValue赋值给当前变量（反序列化）
    /// 
    /// 1.对于集合类型字段，默认会清空当前所有数据，再填充数据。
    /// 2.对于自定义结构，只赋值（覆盖）DsonValue中存在的字段。
    /// 3.该接口通常只应该在反序列化、切换Node或多态字段绑定的数据类型时调用（将既有数据赋值给新对象）。
    /// 4.应当在初始化OutputField后再解码数据，否则可能因类型不匹配导致数据丢失。
    /// 5.Decode并不传递Node的引用，外部需要在Decode之后刷新子节点的Node引用。
    ///
    /// <h3>类型恢复</h3>
    /// 该功能主要用于从资产文件中加载数据节点时，恢复变量的多态类型；
    /// 当变量类型和写入的类型一致时，意味着变量可以完整接收写入的数据，因此其子变量会启动启用数据类型纠正；
    /// 当变量类型和写入的类型不同时，则由用户决定是否应用写入的类型；
    /// 通常来说用户不应该指定该变量，顶层变量的复制不需要纠正类型，因为用户可以在接收数据前手动修改变量类型。
    ///
    /// 注：复制粘贴文本同理。
    /// </summary>
    /// <param name="dsonValue"></param>
    /// <param name="variable"></param>
    /// <param name="applySerializedType">是否应用于序列化中的类型数据</param>
    public void Decode(Variable variable, DsonValue dsonValue, bool applySerializedType = false) {
        // 处理类型变更
        DsonHeader<string> header = GetHeader(dsonValue);
        if (header != null && header.TryGetValue(KEY_TYPE_SYMBOL, out DsonValue boxedTypeSymbol)) {
            string typeSymbol = boxedTypeSymbol.AsString();
            if (GetTypeSymbol(variable.type) == typeSymbol) {
                applySerializedType = true;
            }
            // 需要进行类型兼容测试
            else if (applySerializedType && variable.cfg.ContainsTypeSymbol(typeSymbol)) {
                DSNamedType newType = ResolveTypeSymbol(typeSymbol);
                _graph.ChangeVariableType(variable, newType);
            }
        }
        // Null
        if (dsonValue.DsonType == DsonType.Null) {
            _graph.ResetVariable(variable);
            variable.isNull = true;
            return;
        }
        variable.isNull = false;
        DSNamedType varType = variable.type;
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
            if (dsonValue.DsonType == DsonType.Timestamp) {
                variable.timestampValue = dsonValue.AsTimestamp();
            } else if (dsonValue.DsonType == DsonType.DateTime) {
                ExtDateTime dateTime = dsonValue.AsDateTime();
                variable.timestampValue = new Timestamp(dateTime.Seconds, dateTime.Nanos);
            }
            return;
        }
        // ObjectPtr/ObjectPath
        if (variableCfg.dsonType == DsonType.Pointer) {
            if (dsonValue.DsonType == DsonType.Pointer) {
                variable.objectPathValue = dsonValue.AsPointer();
            }
            return;
        }
        // Nullable
        if (DSUtil.IsNullableType(varType)) {
            // ResetVariable(variable); // 强制清理，确保正确覆盖 - Nullable的路径是稳定的，可不清理
            Decode(variable[0], dsonValue, applySerializedType);
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
                Variable nestedVar = _graph.CreateListItem(variable);
                Decode(nestedVar, nestValue, applySerializedType);
                variable.Add(nestedVar);
            }
            return;
        }
        // 字典 - 从DsonArray恢复时，可能出现兼容问题，因此不予支持
        if (DSUtil.IsMapType(varType)) {
            variable.ClearArray(); // 强制清理，确保正确覆盖
            if (dsonValue.DsonType != DsonType.Object) {
                return;
            }
            DsonObject<string> dsonObject = dsonValue.AsObject();
            variable.values.EnsureCapacity(dsonObject.Count);
            foreach (var pair in dsonObject) {
                Variable varPair = _graph.CreateMapItem(variable);
                Decode(varPair[0], new DsonString(pair.Key));
                Decode(varPair[1], pair.Value, applySerializedType);
                variable.Add(varPair);
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
                Decode(nestedVar, fieldValue, applySerializedType);
            }
        }
    }

    #endregion

    #region node

    public DataNode DecodeNode(DsonValue dsonValue) {
        DsonHeader<string> header = GetHeader(dsonValue);
        if (header == null) {
            throw new InvalidDataException();
        }
        // 要支持编辑器解析的数据，header至少需要包含localId和typeSymbol
        DsonValue tempValue;
        if (!header.TryGetValue(DsonHeader.Names_LocalId, out tempValue)) {
            throw new InvalidDataException("localId is absent");
        }
        long localId = tempValue.AsNumber().LongValue;
        DataNode node = new DataNode(localId);
        if (header.TryGetValue(KEY_NAME, out tempValue)) {
            node.name = tempValue.AsString();
        }
        if (header.TryGetValue(KEY_FOLDER, out tempValue)) {
            node.folder = tempValue.AsString();
        }
        if (header.TryGetValue(KEY_COMMENT, out tempValue)) {
            node.comment = tempValue.AsString();
        }
        if (header.TryGetValue(KEY_FEATURES, out tempValue)) { // 16进制
            node.features = (Features)tempValue.AsNumber().IntValue;
        }
        if (header.TryGetValue(KEY_POSITION, out tempValue)) {
            float x = tempValue.AsObject()["x"].AsNumber().FloatValue;
            float y = tempValue.AsObject()["y"].AsNumber().FloatValue;
            node.position.x = x;
            node.position.y = y;
        }
        // 要支持编辑器解析的数据，必须包含typeSymbol属性
        if (!header.TryGetValue(KEY_TYPE_SYMBOL, out tempValue)) {
            throw new InvalidDataException("typeSymbol is absent");
        }
        string typeSymbol = tempValue.AsString();
        node.value = _graph.CreateVariable(ResolveTypeSymbol(typeSymbol));
        // 需要先纠正字段类型，才能解码
        _graph.InitOutputFields(node);
        Decode(node.value, dsonValue, true);
        //
        node.graph = _graph;
        node.value.SetDataNode(node);
        return node;
    }

    /// <summary>
    /// 输出到文件
    /// </summary>
    public void Write(DsonTextWriter textWriter, DataNode node) {
        if ((node.features & Features.MemoryOnly) != 0) {
            throw new InvalidOperationException("memoryOnly");
        }
        if (node.value.isNull) {
            throw new InvalidOperationException("root value cant be null");
        }
        Write(textWriter, node.value, null);
    }

    /// <summary>
    /// 写入变量到Writer
    /// </summary>
    internal void Write(IDsonWriter<string> writer, Variable variable, string name) {
        // 我们暂时只执行Null跳过，0值仍然输出，否则可能产生维护性问题(字段缺失容易让人产生疑惑)
        if (variable.isNull) {
            if (!writer.IsAtName || IsWriteNullValue(variable)) {
                writer.WriteNull(name);
            }
            return;
        }
        if (writer.IsAtName) {
            writer.WriteName(name);
        }
        DSNamedType varType = variable.type;
        SerializeFeatures features = variable.cfg.encodeFeatures;
        // 原子值
        switch (varType.SimpleName) {
            case DSKeywords.TYPE_INT32: {
                writer.WriteInt32(variable.intValue, features.ToNumberStyle());
                return;
            }
            case DSKeywords.TYPE_INT64: {
                writer.WriteInt64(variable.longValue, features.ToNumberStyle());
                return;
            }
            case DSKeywords.TYPE_FLOAT: {
                writer.WriteFloat(variable.floatValue, features.ToNumberStyle());
                return;
            }
            case DSKeywords.TYPE_DOUBLE: {
                writer.WriteDouble(variable.doubleValue, features.ToNumberStyle());
                return;
            }
            case DSKeywords.TYPE_BOOL: {
                writer.WriteBool(variable.boolValue);
                return;
            }
            case DSKeywords.TYPE_STRING: {
                string stringValue = variable.stringValue;
                if (string.IsNullOrEmpty(stringValue)) {
                    writer.WriteString("", features.ToStringStyle());
                } else {
                    writer.WriteString(stringValue);
                }
                return;
            }
            case DSKeywords.TYPE_BYTES: {
                string stringValue = variable.stringValue;
                if (string.IsNullOrWhiteSpace(stringValue)) {
                    writer.WriteBinary(Binary.EMPTY);
                } else {
                    stringValue = ObjectUtil.DeleteWhitespace(stringValue);
                    writer.WriteBinary(Binary.FromHexString(stringValue));
                }
                return;
            }
        }
        // Enum
        if (varType.Kind == DSElementKind.Enum) {
            if ((features & SerializeFeatures.EnumAsString) != 0) {
                DSEnumValue enumValue = varType.GetEnumValue(variable.intValue);
                if (enumValue == null) {
                    throw new InvalidOperationException($"enumValue {variable.intValue} is absent");
                }
                writer.WriteString(enumValue.SimpleName);
            } else {
                NumberStyle style = DSUtil.IsFlagEnum(varType) ? NumberStyle.UnsignedHex : NumberStyle.Simple;
                writer.WriteInt32(variable.intValue, style);
            }
            return;
        }
        // 测试类型的投影类型
        // DateTime
        VariableCfg variableCfg = _graph.GetVariableCfg(varType);
        if (variableCfg.dsonType == DsonType.DateTime || DSUtil.IsDateTimeType(varType)) {
            Timestamp timestamp = variable.timestampValue;
            writer.WriteDateTime(new ExtDateTime(timestamp.Seconds, timestamp.Nanos));
            return;
        }
        if (variableCfg.dsonType == DsonType.Timestamp || DSUtil.IsTimestampType(varType)) {
            writer.WriteTimestamp(variable.timestampValue);
            return;
        }
        // ObjectPtr
        if (variableCfg.dsonType == DsonType.Pointer || DSUtil.IsPointerType(varType)) {
            writer.WritePtr(variable.objectPathValue);
            return;
        }
        // Nullable - 导出时拆箱
        if (DSUtil.IsNullableType(varType)) {
            Write(writer, variable[0], name);
            return;
        }
        // 普通集合
        if (DSUtil.IsCollectionType(varType)) {
            WriteCollection(writer, variable);
            return;
        }
        // Map
        if (DSUtil.IsMapType(varType)) {
            WriteMap(writer, variable);
            return;
        }
        // 普通结构，导出为DsonObject
        WriteObject(writer, variable);
    }

    private void WriteObject(IDsonWriter<string> writer, Variable variable) {
        writer.WriteStartObject(GetObjectStyle(variable));
        WriteHeader(writer, variable);
        foreach (Variable nestedVar in variable.values) {
            string fieldName = nestedVar.defineInfo.SimpleName;
            Write(writer, nestedVar, fieldName);
        }
        writer.WriteEndObject();
    }

    private void WriteMap(IDsonWriter<string> writer, Variable variable) {
        bool isStringKey = DSUtil.IsStringType(variable.type.TypeArguments[0]);
        //
        writer.WriteStartObject(GetObjectStyle(variable));
        WriteHeader(writer, variable);
        for (int index = 0; index < variable.values.Count; index++) {
            Variable nestedVar = variable.values[index];
            Variable keyVar = nestedVar[0];
            Variable valueVar = nestedVar[1];
            //
            string keyString = isStringKey ? (keyVar.stringValue ?? "") : keyVar.longValue.ToString();
            writer.WriteName(keyString); // Map中的null和0值不可跳过
            Write(writer, valueVar, keyString);
        }
        writer.WriteEndObject();
    }

    private void WriteCollection(IDsonWriter<string> writer, Variable variable) {
        writer.WriteStartArray(GetObjectStyle(variable));
        WriteHeader(writer, variable);
        foreach (Variable nestedVar in variable.values) {
            Write(writer, nestedVar, null);
        }
        writer.WriteEndArray();
    }

    private void WriteHeader(IDsonWriter<string> writer, Variable variable) {
        DataNode node = variable.dataNode;
        if (node != null && variable == node.value) {
            writer.WriteStartHeader();
            //
            string clsName = GetCodecName(node.value.type);
            writer.WriteString(DsonHeader.Names_ClassName, clsName);
            writer.WriteInt64(DsonHeader.Names_LocalId, node.localId, NumberStyle.Simple);
            // localPath只在name有效的情况下才导出
            if (!string.IsNullOrWhiteSpace(node.name)) {
                string localPath = !string.IsNullOrWhiteSpace(node.folder)
                    ? node.folder + "/" + node.name
                    : node.name;
                writer.WriteString(DsonHeader.Names_LocalPath, localPath);
            }
            if (!string.IsNullOrWhiteSpace(node.name)) {
                writer.WriteString(KEY_NAME, node.name);
            }
            if (!string.IsNullOrWhiteSpace(node.folder)) {
                writer.WriteString(KEY_FOLDER, node.folder);
            }
            if (!string.IsNullOrWhiteSpace(node.comment)) {
                writer.WriteString(KEY_COMMENT, node.comment);
            }
            // 编辑器相关数据也存储在Header中，虽然可能导致不必要的运行时数据，但可以大幅降低维护难度
            string typeSymbol = GetTypeSymbol(node.value.type);
            writer.WriteString(KEY_TYPE_SYMBOL, typeSymbol, StringStyle.Quote);
            writer.WriteInt32(KEY_FEATURES, (int)node.features, NumberStyle.UnsignedHex);
            {
                writer.WriteStartObject(KEY_POSITION, ObjectStyle.Flow);
                writer.WriteFloat("x", node.position.x, NumberStyle.Simple);
                writer.WriteFloat("y", node.position.y, NumberStyle.Simple);
                writer.WriteEndObject();
            }
            writer.WriteEndHeader();
        } else {
            // 处理多态：编辑器数据恢复时，需要根据TypeSymbol进行，不能依赖反序列化别名
            DSNamedType declaredType = DataGraph.GetDeclaredType(variable.defineInfo);
            if (declaredType.IsValueType || Equals(variable.type, declaredType)) {
                return;
            }
            string clsName = GetCodecName(variable.type);
            string typeSymbol = GetTypeSymbol(variable.type);
            writer.WriteStartHeader();
            writer.WriteString(DsonHeader.Names_ClassName, clsName);
            writer.WriteString(KEY_TYPE_SYMBOL, typeSymbol, StringStyle.Quote);
            writer.WriteEndHeader();
        }
    }

    private string GetCodecName(DSNamedType namedType) {
        return namedType.CodecTypeName.ToString(_sbCache.Clear()).ToString();
    }

    private ObjectStyle GetObjectStyle(Variable variable) {
        // 优先执行字段特征值，再执行类型特征值
        SerializeFeatures features = variable.cfg.encodeFeatures;
        if ((features & SerializeFeatures.ObjectFlow) != 0) return ObjectStyle.Flow;
        if ((features & SerializeFeatures.ObjectIndent) != 0) return ObjectStyle.Indent;
        //
        features = _graph.GetVariableCfg(variable.type).encodeFeatures;
        return (features & SerializeFeatures.ObjectFlow) != 0
            ? ObjectStyle.Flow
            : ObjectStyle.Indent;
    }

    private bool IsWriteNullValue(Variable variable) {
        SerializeFeatures features = variable.cfg.encodeFeatures;
        if ((features & SerializeFeatures.WriteNullValue) != 0) return true;
        if ((features & SerializeFeatures.SkipNullValue) != 0) return false;
        //
        if (variable.defineInfo is DSField field) {
            DSElement typeElement = field.OriginDefine.EnclosingElement;
            features = _graph.GetVariableCfg(typeElement).encodeFeatures;
            if ((features & SerializeFeatures.WriteNullValue) != 0) return true;
            if ((features & SerializeFeatures.SkipNullValue) != 0) return false;
        }
        return false;
    }

    private bool IsWriteZeroValue(Variable variable) {
        SerializeFeatures features = variable.cfg.encodeFeatures;
        if ((features & SerializeFeatures.WriteZeroValue) != 0) return true;
        if ((features & SerializeFeatures.SkipZeroValue) != 0) return false;
        //
        if (variable.defineInfo is DSField field) {
            DSElement typeElement = field.OriginDefine.EnclosingElement;
            features = _graph.GetVariableCfg(typeElement).encodeFeatures;
            if ((features & SerializeFeatures.WriteZeroValue) != 0) return true;
            if ((features & SerializeFeatures.SkipZeroValue) != 0) return false;
        }
        return false;
    }

    #endregion
}
}