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
using System.Globalization;
using System.Text;
using UnityEngine;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson;
using Wjybxx.Dson.Types;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
/// 该类主要负责数据的编解码逻辑，这部分逻辑相对独立，但代码量较大。
/// </summary>
internal class DataGraphHelper
{
    private readonly DataGraph _graph;
    private readonly StringBuilder _sbCache = new StringBuilder();

    public DataGraphHelper(DataGraph graph) {
        _graph = graph;
    }

    public DsonValue EncodeNode(DataNode node) {
        DsonValue dsonValue = Encode(node.value);
        DsonHeader<string> header;
        if (dsonValue is DsonObject<string> dsonObject) {
            header = dsonObject.Header;
        } else {
            header = dsonValue.AsArray().Header;
        }
        // 写入各项对象头
        StringBuilder clsName = node.value.type.CodecTypeName.ToString(_sbCache.Clear());
        header[DsonHeader.Names_ClassName] = new DsonString(clsName.ToString());
        header[DsonHeader.Names_LocalId] = new DsonInt64(node.localId);
        // localPath只在name有效的情况下才导出
        if (!string.IsNullOrWhiteSpace(node.name)) {
            string localPath = !string.IsNullOrWhiteSpace(node.folder)
                ? node.folder + "/" + node.name
                : node.name;
            header[DsonHeader.Names_LocalPath] = new DsonString(localPath);
        }
        // 这部分数据直接存储在Node上，虽然可能导致不必要的运行时数据，但可以大幅降低维护难度 - 有洁癖的话可以在打包的时候删除
        if (!string.IsNullOrWhiteSpace(node.name)) {
            header["name"] = new DsonString(node.name);
        }
        if (!string.IsNullOrWhiteSpace(node.folder)) {
            header["folder"] = new DsonString(node.folder);
        }
        if (!string.IsNullOrWhiteSpace(node.comment)) {
            header["comment"] = new DsonString(node.comment);
        }
        header["features"] = new DsonInt32((int)node.features); // 打印时应转16进制
        header["position"] = new DsonObject<string>()
        {
            { "x", new DsonFloat(node.position.x) },
            { "y", new DsonFloat(node.position.y) },
        };
        // clsName是运行时需要的数据，typeSymbol是编译期需要的数据
        header["typeSymbol"] = new DsonString(node.value.typeSymbol);
        return dsonValue;
    }

    public DataNode DecodeNode(DsonValue dsonValue) {
        DsonHeader<string> header;
        if (dsonValue is DsonObject<string> dsonObject) {
            header = dsonObject.Header;
        } else {
            header = dsonValue.AsArray().Header;
        }
        // 要支持编辑器解析的数据，header至少需要包含localId和typeSymbol
        DsonValue tempValue;
        if (!header.TryGetValue(DsonHeader.Names_LocalId, out tempValue)) {
            throw new InvalidOperationException("localId is absent");
        }
        long localId = tempValue.AsNumber().LongValue;
        DataNode node = new DataNode(localId);
        if (header.TryGetValue("name", out tempValue)) {
            node.name = tempValue.AsString();
        }
        if (header.TryGetValue("folder", out tempValue)) {
            node.folder = tempValue.AsString();
        }
        if (header.TryGetValue("comment", out tempValue)) {
            node.comment = tempValue.AsString();
        }
        if (header.TryGetValue("features", out tempValue)) {
            node.features = (Features)tempValue.AsNumber().IntValue;
        }
        if (header.TryGetValue("position", out tempValue)) {
            float x = tempValue.AsObject()["x"].AsNumber().FloatValue;
            float y = tempValue.AsObject()["y"].AsNumber().FloatValue;
            node.position.x = x;
            node.position.y = y;
        }
        // 要支持编辑器解析的数据，必须包含typeSymbol属性
        if (!header.TryGetValue("typeSymbol", out tempValue)) {
            throw new InvalidOperationException("typeSymbol is absent");
        }
        string typeSymbol = tempValue.AsString();
        node.value = _graph.CreateVariable(_graph.repository.ResolveTypeSymbol(null, typeSymbol));
        Decode(node.value, dsonValue);
        return node;
    }

    public void Decode(Variable variable, DsonValue dsonValue) {
        DSNamedType varType = variable.type;
        if (dsonValue.DsonType == DsonType.Null) {
            _graph.ResetVariable(variable);
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
                Variable nestedVar = _graph.CreateListItem(variable);
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
                    Variable varPair = _graph.CreateMapItem(variable);
                    Decode(varPair[0], new DsonString(pair.Key));
                    Decode(varPair[1], pair.Value);
                    variable.Add(varPair);
                }
            } else if (dsonValue.DsonType == DsonType.Array) {
                DsonArray<string> dsonArray = dsonValue.AsArray();
                variable.values.EnsureCapacity(dsonArray.Count / 2);
                for (int index = 0; index < dsonArray.Count; index += 2) {
                    Variable varPair = _graph.CreateMapItem(variable);
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
        VariableCfg variableCfg = _graph.GetVariableCfg(varType);
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
        return EncodeAsDsonObject(variable);
    }

    private DsonValue EncodeAsDsonObject(Variable variable) {
        DsonObject<string> dsonObject = new DsonObject<string>(variable.Count);
        foreach (Variable nestedVar in variable.values) {
            DsonValue dsonValue = Encode(nestedVar);
            if (dsonValue.DsonType == DsonType.Null) {
                continue;
            }
            DSNamedType fieldDeclaredType = DataGraph.GetDeclaredType(nestedVar.defineInfo);
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
        StringBuilder clsName = varType.CodecTypeName.ToString(_sbCache.Clear());
        if (exportedValue is DsonObject<string> dsonObject) {
            dsonObject.Header[DsonHeader.Names_ClassName] = new DsonString(clsName.ToString());
        } else {
            DsonArray<string> dsonArray = exportedValue.AsArray();
            dsonArray.Header[DsonHeader.Names_ClassName] = new DsonString(clsName.ToString());
        }
    }
}
}