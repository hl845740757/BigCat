#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Wjybxx.BigCatEditor.DataScript;
using Wjybxx.BigCatEditor.Excel;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;
using static Wjybxx.BigCatEditor.Generator.Excel.ExcelConstants;

namespace Wjybxx.BigCatEditor.Generator.Excel
{
/// <summary>
/// 将表格导出为Dson文本和二进制
///
/// <h3>数据脚本的作用</h3>
/// 为了减少开销，生成的表格对象是顺序读取二进制内容的，调整表格字段顺序会导致错误；
/// 这个问题有多种解决方案，但最佳方案是根据ds文件中的字段顺序导出。
/// 此外，我们会在每张表格的开头写入一个根据所有字段名计算出的hash值，以和生成的class文件中的hash值进行比较，
///
/// 注意：
/// 1.Sheet是不能被直接合并的，因为每个表的元数据可能不同。
/// 2.追加的元数据不是<see cref="DsonHeader{TK}"/>类型，因为多Header可能造成奇怪的问题。
/// 3.程序使用的是二进制文件，文本文件更多是用于Diff。
/// </summary>
public class DsonGenerator : ISheetProcessor
{
    private readonly SheetRepository _repository;
    private readonly DSRepository _dsRepository;
    private readonly RequireMode _requireMode;
    private readonly string _outDir;
    private readonly bool _enableText;
    private readonly string _extension;

    private readonly List<DsonValue> _cacheList = new(10);
    private readonly byte[] buffer = new byte[8 * 1024 * 1024];
    private readonly StringBuilder _sb = new(8192);
    private readonly DsonTextWriterSettings _textWriterSettings;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository">要处理的文件</param>
    /// <param name="dsRepository">数据脚本仓库</param>
    /// <param name="requireMode">要导出的内容</param>
    /// <param name="outDir">输出目录</param>
    /// <param name="enableText">是否生成文本文件</param>
    /// <param name="extension">二进制文件的扩展名</param>
    public DsonGenerator(SheetRepository repository, DSRepository dsRepository, RequireMode requireMode,
                         string outDir, bool enableText = false, string extension = ".dson2") {
        _repository = repository;
        _requireMode = requireMode;
        _dsRepository = dsRepository;
        _outDir = outDir;
        _enableText = enableText;
        _extension = extension;

        // 尽量一行
        _textWriterSettings = new DsonTextWriterSettings.Builder
            {
                SoftLineLength = 500
            }
            .Build();
    }

    public void Execute() {
        foreach (IGrouping<string, Sheet> grouping in _repository.GetSortedSheets().GroupBy(e => GetFirstSheetName(e.sheetName))) {
            List<Sheet> sheets = grouping.ToList();
            DsonArray<string> collection;
            bool isParamSheet = sheets[0].isParamSheet;
            try {
                if (isParamSheet) {
                    collection = ProcessParamSheet(grouping, sheets);
                } else {
                    collection = ProcessNormalSheet(grouping, sheets);
                }
            }
            catch (Exception ex) {
                throw new Exception($"sheetGroup: {grouping.Key}", ex);
            }
            if (collection == null) {
                continue;
            }
            if (_enableText) {
                string dsonText = ToDsonText(collection, isParamSheet);
                File.WriteAllText(_outDir + "/" + grouping.Key + ".dson", dsonText);
            }
            File.WriteAllBytes(_outDir + "/" + grouping.Key + _extension, ToDsonBytes(collection));
        }
    }

    private string ToDsonText(DsonArray<string> collection, bool isParamSheet) {
        StringBuilder sb = _sb.Clear();
        using (DsonTextWriter writer = new DsonTextWriter(_textWriterSettings, new StringWriter(sb))) {
            if (isParamSheet) {
                Dsons.WriteTopDsonValue(writer, collection[0], ObjectStyle.Flow);
                Dsons.WriteTopDsonValue(writer, collection[1]);
            } else {
                foreach (DsonValue element in collection) {
                    if (sb.Length > 0 && element.DsonType == DsonType.Object) {
                        sb.AppendLine(); // 元数据
                    }
                    Dsons.WriteTopDsonValue(writer, element, ObjectStyle.Flow);
                }
            }
        }
        return sb.ToString();
    }

    private byte[] ToDsonBytes(DsonArray<string> collection) {
        using var output = DsonOutputs.NewInstance(buffer);
        using DsonBinaryWriter<string> writer = new DsonBinaryWriter<string>(DsonWriterSettings.Default, output);
        Dsons.WriteCollection(writer, collection);
        return ArrayUtil.CopyOf(buffer, 0, output.Position);
    }

    #region build

    private DsonArray<string>? ProcessParamSheet(IGrouping<string, Sheet> grouping, List<Sheet> sheets) {
        string mergedSheetName = grouping.Key;
        string className = DataScriptGenerator.GetClassName(mergedSheetName);
        DSNamedType namedType = _dsRepository.GetType(className);
        if (namedType == null) { // 不存在对应的Class
            return null;
        }
        // 合并所有的value
        LinkedDictionary<string, DsonValue> valueMap = new();
        foreach (Sheet sheet in sheets) {
            foreach (Header header in sheet.headers.Values) {
                if (header.name.Contains('#') || !IsRequired(header.options, _requireMode)) {
                    continue;
                }
                DSTypeElement fieldType = _dsRepository.ResolveTypeSymbol(null, header.type);
                DsonValue value;
                if (sheet.GetHeader(header.name + "#1") != null) {
                    value = MergeParamSheetCell(sheet, header, (DSNamedType)fieldType, CollectElementHeaders(sheet, header));
                } else {
                    value = GetValue(fieldType, sheet.GetValue(header.name));
                }
                valueMap.Add(header.name, value);
            }
        }

        List<DSField> fields = namedType.GetFields();
        // 2 = header + object
        DsonArray<string> collection = new DsonArray<string>(2);
        collection.Add(new DsonObject<string>(2)
        {
            { STRING_CLS_NAME, new DsonString(namedType.SimpleName) },
            { STRING_SERIAL_VERSION, new DsonInt32(GetHashCode(fields)) }
        });
        // 按脚本的字段定义顺序构建DsonObject -- 参数表构建为Object
        DsonObject<string> paramObject = new DsonObject<string>(fields.Count);
        foreach (DSField field in fields) {
            if (!valueMap.TryGetValue(field.SimpleName, out DsonValue value)) {
                value = GetValue(field.Type, ""); // 根据空白字符串计算默认值
            }
            paramObject.Add(field.SimpleName, value);
        }
        collection.Add(paramObject);
        return collection;
    }

    private DsonValue MergeParamSheetCell(Sheet sheet, Header fieldHeader, DSNamedType fieldType, List<Header> elemHeaders) {
        string fieldName = fieldHeader.name;
        DsonObject<string> options = ParseOptions(fieldHeader.options);
        CheckOriginalCell(options, fieldName, sheet.GetValue(fieldName));

        DSTypeElement elementType = GetElementType(fieldType);
        List<DsonValue> values = GetCachedList();
        if (GetBool(options, KEY_IS_RECORD)) {
            // 合并所有行的值
            foreach (Header elemHeader in elemHeaders) {
                string? rawValue = sheet.GetValue(elemHeader.name);
                values.Add(GetValue(elementType, rawValue));
            }
        } else {
            // 合并非空白行，遇见空白行中断
            foreach (Header elemHeader in elemHeaders) {
                string? rawValue = sheet.GetValue(elemHeader.name);
                if (IsBreakMerge(rawValue, elementType)) {
                    break;
                }
                values.Add(GetValue(elementType, rawValue));
            }
        }
        return MergeValues(fieldHeader, values);
    }


    /// <summary>
    /// 与参数表不同，普通表存在子类型表；所有分块表，以及子类型表都会被合并到同一个Dson文件
    /// </summary>
    private DsonArray<string> ProcessNormalSheet(IGrouping<string, Sheet> grouping, List<Sheet> sheets) {
        // header, values, header, values
        int expectedCount = sheets.Sum(e => e.valueRows.Count) + sheets.Count;
        DsonArray<string> collection = new DsonArray<string>(expectedCount);
        foreach (Sheet sheet in sheets) {
            string mergedSheetName = GetMergedSheetName(sheet.sheetName);
            string className = DataScriptGenerator.GetClassName(mergedSheetName);
            DSNamedType namedType = _dsRepository.GetType(className);
            if (namedType == null) { // 不存在对应的Class
                continue;
            }
            // 建立表头缓存
            LinkedDictionary<string, HeaderCache> headerCaches = new();
            foreach (var header in sheet.headers.Values) {
                if (header.name.Contains('#') || !IsRequired(header.options, _requireMode)) {
                    continue;
                }
                DSTypeElement fieldType = _dsRepository.ResolveTypeSymbol(null, header.type);
                List<Header>? elemHeaders = null;
                if (sheet.GetHeader(header.name + "#1") != null) {
                    elemHeaders = CollectElementHeaders(sheet, header);
                }
                headerCaches.Add(header.name, new HeaderCache(header, fieldType, elemHeaders));
            }

            List<DSField> fields = namedType.GetFields();
            // 追加header -- 由于可能存在空白行，我们不能直接记录Sheet的行数
            DsonObject<string> headerObject = new DsonObject<string>(3)
            {
                { STRING_CLS_NAME, new DsonString(namedType.SimpleName) },
                { STRING_SERIAL_VERSION, new DsonInt32(GetHashCode(fields)) },
            };
            collection.Add(headerObject);
            // 追加一行字段名数据 -- 方便文本查看，解码时可直接SkipValue
            DsonArray<string> nameArray = new DsonArray<string>(fields.Count);
            foreach (DSField field in fields) {
                nameArray.Add(new DsonString(field.SimpleName));
            }
            collection.Add(nameArray);

            // 按脚本的字段定义顺序构建DsonArray -- 普通表构建为Array
            int count1 = collection.Count;
            foreach (SheetRow sheetRow in sheet.valueRows) {
                DsonArray<string> dsonArray = new DsonArray<string>(fields.Count);
                foreach (DSField field in fields) {
                    DsonValue value;
                    if (!headerCaches.TryGetValue(field.SimpleName, out HeaderCache headerCache)) {
                        value = GetValue(field.Type, ""); // 根据空白字符串计算默认值
                    } else if (headerCache.elemHeaders != null) {
                        value = MergeNormalSheetCell(sheetRow, headerCache.header, (DSNamedType)headerCache.fieldType, headerCache.elemHeaders);
                    } else {
                        value = GetValue(headerCache.fieldType, sheetRow.GetValue(field.SimpleName));
                    }
                    dsonArray.Add(value);
                }
                collection.Add(dsonArray);
            }
            // 更新header元数据
            headerObject.Add(STRING_ROW_COUNT, new DsonInt32(collection.Count - count1));
        }
        return collection;
    }

    /// <summary>
    /// 普通表需要先缓存List和Map的成员列表头信息
    /// </summary>
    /// <returns></returns>
    private DsonValue MergeNormalSheetCell(SheetRow sheetRow, Header fieldHeader, DSNamedType fieldType, List<Header> elemHeaders) {
        string fieldName = fieldHeader.name;
        DsonObject<string> options = ParseOptions(fieldHeader.options);
        CheckOriginalCell(options, fieldName, sheetRow.GetValue(fieldName));

        DSTypeElement elementType = GetElementType(fieldType);
        List<DsonValue> values = GetCachedList();
        if (GetBool(options, KEY_IS_RECORD)) {
            // 合并所有列的值
            foreach (Header elemHeader in elemHeaders) {
                string? rawValue = sheetRow.GetValue(elemHeader.name);
                values.Add(GetValue(elementType, rawValue));
            }
            return MergeValues(fieldHeader, values);
        } else {
            // 合并非空白列，遇见空白列中断
            foreach (Header elemHeader in elemHeaders) {
                string? rawValue = sheetRow.GetValue(elemHeader.name);
                if (IsBreakMerge(rawValue, elementType)) {
                    break;
                }
                values.Add(GetValue(elementType, rawValue));
            }
            return MergeValues(fieldHeader, values);
        }
    }

    private List<DsonValue> GetCachedList() {
        _cacheList.Clear();
        return _cacheList;
    }

    private DSTypeElement GetElementType(DSNamedType fieldType) {
        if (IsListType(fieldType.SimpleName)) {
            return fieldType.TypeArguments[0];
        }
        DSNamedType pairType = _dsRepository.GetBuiltinType(DSKeywords.TYPE_PAIR);
        Debug.Assert(pairType != null);
        return _dsRepository.MakeGenericType(pairType, new List<DSTypeElement>(fieldType.TypeArguments));
    }

    private static List<Header> CollectElementHeaders(Sheet sheet, Header fieldHeader) {
        string fieldName = fieldHeader.name;
        bool isListType = IsListType(fieldHeader.type);
        bool isMapType = IsMapType(fieldHeader.type);
        if (!isListType && !isMapType) {
            throw new Exception($"the field {fieldName} must be List or Map");
        }
        // 收集所有列名 -- 注意，配置表中索引1开始
        List<Header> elemHeaders = new List<Header>();
        for (int index = 1; index <= ELEMENT_LIMIT; index++) {
            Header? elemHeader = sheet.GetHeader(fieldName + "#" + index);
            if (elemHeader == null) {
                break;
            }
            if (isMapType && !string.IsNullOrWhiteSpace(elemHeader.type) && !IsPairType(elemHeader.type)) {
                throw new Exception($"the filed {fieldName} is map type, but the element {elemHeader.name} is not pair type");
            }
            elemHeaders.Add(elemHeader);
        }
        return elemHeaders;
    }

    private static void CheckOriginalCell(DsonObject<string> options, string fieldName, string? value) {
        if (!GetBool(options, KEY_NO_CHECK) && !string.IsNullOrWhiteSpace(value)) {
            throw new Exception($"the original field value must be empty or check disabled, fieldName: {fieldName}");
        }
    }

    private static bool IsBreakMerge([NotNullWhen(false)] string? value, DSTypeElement elementType) {
        return IsStringType(elementType.SimpleName)
            ? string.IsNullOrEmpty(value)
            : string.IsNullOrWhiteSpace(value);
    }

    private static DsonValue MergeValues(Header fieldHeader, List<DsonValue> values) {
        if (IsListType(fieldHeader.type)) {
            DsonArray<string> dsonArray = new DsonArray<string>(values.Count);
            dsonArray.AddAll(values);
            return dsonArray;
        }
        DsonObject<string> dsonObject = new DsonObject<string>(values.Count);
        foreach (DsonValue pair in values) {
            if (pair.DsonType == DsonType.Object) {
                DsonObject<string> pairObject = (DsonObject<string>)pair;
                Debug.Assert(pairObject.Count == 1);
                dsonObject.AddAll(pairObject);
                continue;
            }
            throw new Exception($"invalid pair value: {pair}");
        }
        return dsonObject;
    }

    /// <summary>
    /// 字段元数据的Hash
    /// 
    /// 我们需要将其添加到生成的Class类型信息中，或是注解-或是静态字段
    /// </summary>
    /// <param name="fields"></param>
    /// <returns></returns>
    public static int GetHashCode(List<DSField> fields) {
        int hash = 0;
        foreach (DSField field in fields) {
            hash = hash * 31 + field.TypeSymbol.GetHashCode();
            hash = hash * 31 + field.SimpleName.GetHashCode();
        }
        return hash;
    }

    #endregion

    #region get-value

    private static readonly DsonInt32 INT32_ZERO = new DsonInt32(0);
    private static readonly DsonInt64 INT64_ZERO = new DsonInt64(0);
    private static readonly DsonFloat FLOAT_ZERO = new DsonFloat(0);
    private static readonly DsonDouble DOUBLE_ZERO = new DsonDouble(0);

    /// <summary>
    /// 获取字符串对应的DsonValue，List和Object会递归处理
    /// </summary>
    /// <returns></returns>
    private DsonValue GetValue(DSTypeElement type, string? rawValue) {
        // 由于我们并未支持数组，因此这里应该都是NamedType
        DSNamedType namedType = (DSNamedType)type;
        // 字符串需要保留原始值
        if (IsStringType(namedType.SimpleName)) {
            return new DsonString(rawValue ?? "");
        }
        // 空白字符串返回默认值
        if (string.IsNullOrWhiteSpace(rawValue)) {
            return namedType.SimpleName switch
            {
                DSKeywords.TYPE_INT32 => INT32_ZERO,
                DSKeywords.TYPE_INT64 => INT64_ZERO,
                DSKeywords.TYPE_FLOAT => FLOAT_ZERO,
                DSKeywords.TYPE_DOUBLE => DOUBLE_ZERO,
                DSKeywords.TYPE_BOOL => DsonBool.FALSE,

                DSKeywords.TYPE_LIST => new DsonArray<string>(0),
                DSKeywords.TYPE_MAP => new DsonObject<string>(0),
                _ => DsonNull.NULL
            };
        }
        // Nullable类型，需要按照真实类型获取Value
        if (IsNullableType(namedType.SimpleName)) {
            namedType = (DSNamedType)namedType.TypeArguments[0];
        }
        rawValue = rawValue.Trim();
        DsonValue value = namedType.SimpleName switch
        {
            DSKeywords.TYPE_INT32 => new DsonInt32(ParseInt32(rawValue)),
            DSKeywords.TYPE_INT64 => new DsonInt64(ParseInt64(rawValue)),
            DSKeywords.TYPE_FLOAT => new DsonFloat(DsonTexts.ParseFloat(rawValue)),
            DSKeywords.TYPE_DOUBLE => new DsonDouble(DsonTexts.ParseDouble(rawValue)),
            DSKeywords.TYPE_BOOL => new DsonBool(DsonTexts.ParseBool(rawValue)),

            DSKeywords.TYPE_PTR => new DsonPointer(ParsePointer(rawValue)),
            DSKeywords.TYPE_LPTR => new DsonLitePointer(ParseLitePointer(rawValue)),
            DSKeywords.TYPE_DATETIME => new DsonDateTime(ParseDateTime(rawValue)),
            DSKeywords.TYPE_TIMESTAMP => new DsonTimestamp(Timestamp.Parse(rawValue)),
            DSKeywords.TYPE_PAIR => ParsePair(rawValue),
            _ => namedType.IsEnum ? ParseEnum(rawValue) : Dsons.FromDson(rawValue)
        };
        return RepairFieldValue(namedType, value);
    }

    /// <summary>
    /// 修正被误解析为String类型的字段
    /// (容器值需要递归)
    /// </summary>
    /// <param name="namedType"></param>
    /// <param name="container"></param>
    private DsonValue RepairFieldValue(DSNamedType namedType, DsonValue container) {
        if (IsListType(namedType.SimpleName)) {
            // 修正List的Value
            DSTypeElement elementType = namedType.TypeArguments[0];
            if (IsStringType(elementType.SimpleName)) {
                return container;
            }
            DsonArray<string> dsonArray = (DsonArray<string>)container;
            for (int i = 0; i < dsonArray.Count; i++) {
                DsonValue element = dsonArray[i];
                if (element.DsonType == DsonType.String) {
                    element = GetValue(elementType, element.AsString());
                    dsonArray[i] = GetValue(elementType, element.AsString());
                } else if (element.DsonType == DsonType.Array) {
                    RepairFieldValue((DSNamedType)elementType, element);
                } else if (element.DsonType == DsonType.Object) {
                    RepairFieldValue((DSNamedType)elementType, element);
                }
            }
            return container;
        }
        if (IsMapType(namedType.SimpleName)) {
            // 修正Map的Value
            DSTypeElement elementType = namedType.TypeArguments[1];
            if (IsStringType(elementType.SimpleName)) {
                return container;
            }
            // 覆盖数据不会导致迭代抛出异常
            DsonObject<string> dsonObject = (DsonObject<string>)container;
            foreach (var pair in dsonObject) {
                DsonValue element = pair.Value;
                if (element.DsonType == DsonType.String) {
                    element = GetValue(elementType, element.AsString());
                    dsonObject[pair.Key] = element;
                } else if (element.DsonType == DsonType.Array) {
                    RepairFieldValue((DSNamedType)elementType, element);
                } else if (element.DsonType == DsonType.Object) {
                    RepairFieldValue((DSNamedType)elementType, element);
                }
            }
            return container;
        }
        // 修正自定义结构中的字段
        // 理论上，由于支持多态，应当按照Header中的真实类型信息修正数据；
        // 但不走正常的序列化是很难处理的，因为类型系统不一致，类型信息的处理很麻烦 -- 只能说用户手写Dson的时候按标准写，不要用特殊语法；
        if (container.DsonType == DsonType.Object) {
            DsonObject<string> dsonObject = (DsonObject<string>)container;
            foreach (DSField field in namedType.GetFields()) {
                if (!dsonObject.TryGetValue(field.SimpleName, out DsonValue element)) {
                    continue;
                }
                DSTypeElement elementType = field.Type;
                if (!IsStringType(elementType.SimpleName) && element.DsonType == DsonType.String) {
                    element = GetValue(elementType, element.AsString());
                    dsonObject[field.SimpleName] = element;
                } else if (element.DsonType == DsonType.Array) {
                    RepairFieldValue((DSNamedType)elementType, element);
                } else if (element.DsonType == DsonType.Object) {
                    RepairFieldValue((DSNamedType)elementType, element);
                }
            }
        } else if (container.DsonType == DsonType.Array) {
            DsonArray<string> dsonArray = (DsonArray<string>)container;
            List<DSField> fields = namedType.GetFields();
            // 自定义结构被解析为数组是可以正常解码的 -- 数据长度可能小于字段长度
            for (int i = 0, count = Math.Min(fields.Count, dsonArray.Count); i < count; i++) {
                DsonValue element = dsonArray[i];
                DSTypeElement elementType = fields[i].Type;
                if (!IsStringType(elementType.SimpleName) && element.DsonType == DsonType.String) {
                    element = GetValue(elementType, element.AsString());
                    dsonArray[i] = element;
                } else if (element.DsonType == DsonType.Array) {
                    RepairFieldValue((DSNamedType)elementType, element);
                } else if (element.DsonType == DsonType.Object) {
                    RepairFieldValue((DSNamedType)elementType, element);
                }
            }
        }
        // 自定义结构和内建结构都支持修正
        DSTypeHandler handler = _dsRepository.GetTypeHandler(namedType.OriginDefine.SimpleName);
        return handler != null ? handler.ConvertValue(_dsRepository, namedType, container) : container;
    }

    private static int ParseInt32(string rawValue) {
        if (rawValue.IndexOf('|') > 0) { // A | B | C
            int value = 0;
            foreach (string e in rawValue.Split('|', StringSplitOptions.TrimEntries)) {
                value |= int.Parse(e);
            }
            return value;
        }
        return DsonTexts.ParseInt32(rawValue);
    }

    private static long ParseInt64(string rawValue) {
        if (rawValue.IndexOf('|') > 0) { // A | B | C
            long value = 0;
            foreach (string e in rawValue.Split('|', StringSplitOptions.TrimEntries)) {
                value |= long.Parse(e);
            }
            return value;
        }
        return DsonTexts.ParseInt64(rawValue);
    }

    private static DsonValue ParseEnum(string rawValue) {
        int value = 0;
        if (rawValue.IndexOf('|') > 0) { // A | B | C
            foreach (string e in rawValue.Split('|', StringSplitOptions.TrimEntries)) {
                value |= int.Parse(e);
            }
        } else {
            value = int.Parse(rawValue);
        }
        return new DsonInt32(value);
    }

    private static ObjectPtr ParsePointer(string rawValue) {
        if (rawValue[0] == '{') {
            rawValue = rawValue.Replace("@ptr", "");

            DsonObject<string> dsonObject = (DsonObject<string>)Dsons.FromDson(rawValue);
            dsonObject.TryGetValue(ObjectPtr.NamesNamespace, out DsonValue p1);
            dsonObject.TryGetValue(ObjectPtr.NamesLocalId, out DsonValue p2);
            dsonObject.TryGetValue(ObjectPtr.NamesType, out DsonValue p3);
            dsonObject.TryGetValue(ObjectPtr.NamesPolicy, out DsonValue p4);
            return new ObjectPtr(
                p1 == null ? "" : p1.AsString(),
                p2 == null ? "" : p2.AsString(),
                p3 == null ? (byte)0 : (byte)p3.AsInt32(),
                p4 == null ? (byte)0 : (byte)p4.AsInt32()
            );
        }
        return new ObjectPtr(rawValue);
    }

    private static ObjectLitePtr ParseLitePointer(string rawValue) {
        if (rawValue[0] == '{') {
            rawValue = rawValue.Replace("@lptr", "");

            DsonObject<string> dsonObject = (DsonObject<string>)Dsons.FromDson(rawValue);
            dsonObject.TryGetValue(ObjectPtr.NamesNamespace, out DsonValue p1);
            dsonObject.TryGetValue(ObjectPtr.NamesLocalId, out DsonValue p2);
            dsonObject.TryGetValue(ObjectPtr.NamesType, out DsonValue p3);
            dsonObject.TryGetValue(ObjectPtr.NamesPolicy, out DsonValue p4);
            return new ObjectLitePtr(
                p1 == null ? 0 : p1.AsInt64(),
                p2 == null ? "" : p2.AsString(),
                p3 == null ? (byte)0 : (byte)p3.AsInt32(),
                p4 == null ? (byte)0 : (byte)p4.AsInt32()
            );
        }
        return new ObjectLitePtr(long.Parse(rawValue));
    }

    private static ExtDateTime ParseDateTime(string rawValue) {
        // 比校准的Dson增加了空格支持
        string format = rawValue.IndexOf(' ') > 0 ? "yyyy-MM-dd HH:mm:ss" : "yyyy-MM-ddTHH:mm:ss";
        DateTime dateTime = DateTime.ParseExact(rawValue, format, CultureInfo.InvariantCulture);
        return ExtDateTime.OfDateTime(dateTime);
    }

    private static DsonObject<string> ParsePair(string rawValue) {
        if (rawValue[0] != '{') {
            throw new Exception($"invalid pair value: {rawValue}");
        }
        DsonObject<string> pairObject = (DsonObject<string>)Dsons.FromDson(rawValue);
        if (pairObject.Count != 1) {
            throw new Exception($"invalid pair value: {rawValue}");
        }
        return pairObject;
    }

    #endregion

    private class HeaderCache
    {
        internal readonly Header header;
        internal readonly DSTypeElement fieldType;
        internal readonly List<Header>? elemHeaders;

        public HeaderCache(Header header, DSTypeElement fieldType, List<Header>? elemHeaders) {
            this.header = header;
            this.elemHeaders = elemHeaders;
            this.fieldType = fieldType;
        }
    }
}
}