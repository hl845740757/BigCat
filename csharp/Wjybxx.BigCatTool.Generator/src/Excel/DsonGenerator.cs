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
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.BigCatTool.Excel;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;
using static Wjybxx.BigCatTool.Generator.Excel.ExcelConstants;

namespace Wjybxx.BigCatTool.Generator.Excel
{
/// <summary>
/// 将表格导出为Dson文本和二进制
///
/// <h3>数据脚本的作用</h3>
/// 为了减少开销，生成的表格对象是顺序读取二进制内容的，调整表格字段顺序会导致错误；
/// 这个问题有多种解决方案，但最佳方案是根据ds文件中的字段顺序导出。
/// 此外，我们会在每张表格的开头写入一个根据所有字段的类型和名字计算出的hash值，以和生成的class文件中的hash值进行比较，
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
    private readonly DsonGeneratorCfg _cfg;
    private readonly RequireMode _requireMode;

    private readonly byte[] _buffer;
    private readonly StringBuilder _sb = new(8192);
    private readonly List<DsonValue> _valueListCache = new(10);
    private readonly ObjectPool<List<DSField>> fieldListPool = PoolUtil.NewListPool<DSField>(8);
    private readonly ObjectPool<LinkedDictionary<string, DsonValue>> dictionaryPool = PoolUtil.NewLinkedDictionaryPool<string, DsonValue>(8);
    private readonly DsonTextWriterSettings _textWriterSettings;
    private Sheet? _curSheet; // 用于打印日志

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository">要处理的文件</param>
    /// <param name="dsRepository">数据脚本仓库</param>
    /// <param name="cfg">生成器配置</param>
    /// <param name="requireMode">要导出的内容</param>
    public DsonGenerator(SheetRepository repository, DSRepository dsRepository, DsonGeneratorCfg cfg, RequireMode requireMode) {
        _repository = repository;
        _dsRepository = dsRepository;
        _cfg = cfg;
        _requireMode = requireMode;

        _buffer = new byte[cfg.bufferLen];
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
                throw new Exception($"curSheet: {_curSheet?.sheetName}", ex);
            }
            if (collection == null) {
                continue;
            }
            if (_cfg.enableText) {
                string dsonText = ToDsonText(collection, isParamSheet);
                File.WriteAllText(_cfg.outPath + "/" + grouping.Key + ".dson", dsonText);
            }
            if (_cfg.enableBinary) {
                File.WriteAllBytes(_cfg.outPath + "/" + grouping.Key + _cfg.fileExtension, ToDsonBytes(collection));
            }
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
        using var output = DsonOutputs.NewInstance(_buffer);
        using DsonBinaryWriter<string> writer = new DsonBinaryWriter<string>(DsonWriterSettings.Default, output);
        Dsons.WriteCollection(writer, collection);
        return ArrayUtil.CopyOf(_buffer, 0, output.Position);
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
            _curSheet = sheet;
            foreach (Header header in sheet.headers.Values) {
                if (header.name.Contains('#') || !IsRequired(header.options, _requireMode)) {
                    continue;
                }
                DSTypeElement fieldType = _dsRepository.ResolveTypeSymbol(null, header.type);
                DsonValue value;
                if (sheet.GetHeader(header.name + "#1") != null) {
                    value = MergeCellValue(sheet, header, (DSNamedType)fieldType, CollectElementHeaders(sheet, header));
                } else {
                    value = GetValue(fieldType, sheet.GetValue(header.name), true);
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
            { STRING_SERIAL_VERSION, new DsonInt32(DataScriptGenerator.GetHashCode(fields)) }
        });
        // 按脚本的字段定义顺序构建DsonObject -- 参数表构建为Object
        DsonObject<string> paramObject = new DsonObject<string>(fields.Count);
        foreach (DSField field in fields) {
            if (!valueMap.TryGetValue(field.SimpleName, out DsonValue value)) {
                value = GetValue(field.Type, "", true); // 根据空白字符串计算默认值
            }
            paramObject.Add(field.SimpleName, value);
        }
        collection.Add(paramObject);
        return collection;
    }

    /// <summary>
    /// 与参数表不同，普通表存在子类型表；所有分块表，以及子类型表都会被合并到同一个Dson文件
    /// </summary>
    private DsonArray<string> ProcessNormalSheet(IGrouping<string, Sheet> grouping, List<Sheet> sheets) {
        // header, values, header, values
        int expectedCount = sheets.Sum(e => e.valueRows.Count) + sheets.Count;
        DsonArray<string> collection = new DsonArray<string>(expectedCount);
        foreach (Sheet sheet in sheets) {
            _curSheet = sheet;
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
                { STRING_SERIAL_VERSION, new DsonInt32(DataScriptGenerator.GetHashCode(fields)) },
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
                        value = GetValue(field.Type, "", true); // 根据空白字符串计算默认值
                    } else if (headerCache.elemHeaders != null) {
                        value = MergeCellValue(sheetRow, headerCache.header, (DSNamedType)headerCache.fieldType, headerCache.elemHeaders);
                    } else {
                        value = GetValue(headerCache.fieldType, sheetRow.GetValue(field.SimpleName), true);
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

    private DsonValue MergeCellValue(IValueProvider valueProvider, Header fieldHeader, DSNamedType fieldType, List<Header> elemHeaders) {
        string fieldName = fieldHeader.name;
        DsonObject<string> options = ParseOptions(fieldHeader.options);
        CheckOriginalCell(options, fieldName, valueProvider.GetValue(fieldName));

        DSTypeElement elementType = GetElementType(fieldType);
        List<DsonValue> values = _valueListCache.ClearAndReturn();
        if (GetBool(options, KEY_IS_RECORD)) {
            // 合并所有列的值
            foreach (Header elemHeader in elemHeaders) {
                string? rawValue = valueProvider.GetValue(elemHeader.name);
                values.Add(GetValue(elementType, rawValue));
            }
        } else {
            // 遇见空白列中断
            foreach (Header elemHeader in elemHeaders) {
                string? rawValue = valueProvider.GetValue(elemHeader.name);
                if (IsBreakMerge(rawValue, elementType)) {
                    break;
                }
                values.Add(GetValue(elementType, rawValue));
            }
        }
        if (IsCollectionType(fieldHeader.type)) {
            DsonArray<string> dsonArray = new DsonArray<string>(values.Count);
            dsonArray.AddAll(values);
            return dsonArray;
        }
        // Map
        DsonObject<string> dsonObject = new DsonObject<string>(values.Count);
        foreach (DsonValue pair in values) {
            if (pair.DsonType == DsonType.Object) {
                DsonObject<string> pairObject = (DsonObject<string>)pair;
                Debug.Assert(pairObject.Count == 1, pairObject.Count.ToString());
                dsonObject.AddAll(pairObject);
                continue;
            }
            throw new Exception($"invalid pair value: {pair}");
        }
        return dsonObject;
    }

    private static void CheckOriginalCell(DsonObject<string> options, string fieldName, string? value) {
        if (!GetBool(options, KEY_NO_CHECK) && !string.IsNullOrWhiteSpace(value)) {
            throw new Exception($"the original field value must be empty or check disabled, fieldName: {fieldName}");
        }
    }

    private DSTypeElement GetElementType(DSNamedType fieldType) {
        if (IsCollectionType(fieldType.SimpleName)) {
            return fieldType.TypeArguments[0];
        }
        DSNamedType pairType = _dsRepository.GetBuiltinType(DSKeywords.TYPE_PAIR);
        Debug.Assert(pairType != null);
        return _dsRepository.MakeGenericType(pairType, new List<DSTypeElement>(fieldType.TypeArguments));
    }

    private static bool IsBreakMerge([NotNullWhen(false)] string? value, DSTypeElement elementType) {
        return IsStringType(elementType.SimpleName)
            ? string.IsNullOrEmpty(value)
            : string.IsNullOrWhiteSpace(value);
    }

    #endregion

    #region get-value

    /// <summary>
    /// 获取字符串对应的DsonValue
    /// </summary>
    /// <returns></returns>
    private DsonValue GetValue(DSTypeElement type, string? rawValue, bool root = false) {
        // 由于我们并未支持数组，因此这里应该都是NamedType
        DSNamedType namedType = (DSNamedType)type;
        // 字符串需要保留原始值
        if (DSUtil.IsStringType(namedType)) {
            return string.IsNullOrEmpty(rawValue) ? DsonString.EMPTY : new DsonString(rawValue);
        }
        // 空白字符串返回默认值
        if (string.IsNullOrWhiteSpace(rawValue)) {
            return namedType.SimpleName switch
            {
                DSKeywords.TYPE_INT32 => DsonInt32.ZERO,
                DSKeywords.TYPE_INT64 => DsonInt64.ZERO,
                DSKeywords.TYPE_FLOAT => DsonFloat.ZERO,
                DSKeywords.TYPE_DOUBLE => DsonDouble.ZERO,
                DSKeywords.TYPE_BOOL => DsonBool.FALSE,
                // 集合类型默认返回空集合，而不是null
                DSKeywords.TYPE_LIST => root ? new DsonArray<string>() : DsonNull.NULL,
                DSKeywords.TYPE_HASH_SET => root ? new DsonArray<string>() : DsonNull.NULL,
                DSKeywords.TYPE_MAP => root ? new DsonObject<string>() : DsonNull.NULL,
                _ => DsonNull.NULL
            };
        }
        // Nullable类型，需要按照真实类型获取Value
        if (DSUtil.IsNullableType(namedType)) {
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

            DSKeywords.TYPE_DATETIME => new DsonDateTime(ParseDateTime(rawValue)),
            DSKeywords.TYPE_TIMESTAMP => new DsonTimestamp(Timestamp.Parse(rawValue)),
            DSKeywords.TYPE_PAIR => ParsePair(rawValue),
            _ => namedType.IsEnum ? ParseEnum(namedType, rawValue) : ParseDefault(namedType, rawValue)
        };
        return RepairFieldValue(namedType, value);
    }

    private static DsonValue ParseDefault(DSNamedType namedType, string rawValue) {
        rawValue = rawValue.Trim();
        if (rawValue[0] == '[' || rawValue[0] == '{') {
            return Dsons.FromDson(rawValue);
        }
        // 默认解码为String以允许自定义纠正
        return new DsonString(rawValue);
    }

    /// <summary>
    /// 修正字段的值（还包括字段顺序纠正）
    ///
    /// 1.容器(List/HashSet/Map)值需要递归。
    /// 2.如果是容器类型，字符串类型也可能被误解析为数字，策划配置时需要手动加引号。
    /// 3.只有字符串单元格才不需要处理特殊字符。
    /// 4.不能使用<see cref="ExcelConstants"/>中的类型测试方法，因为存在第三方数据结构。
    /// </summary>
    /// <param name="namedType"></param>
    /// <param name="container"></param>
    private DsonValue RepairFieldValue(DSNamedType namedType, DsonValue container) {
        if (CodeGeneratorHelper.IsCollectionType(namedType)) {
            // 修正List的Value
            DsonArray<string> dsonArray = (DsonArray<string>)container;
            DSTypeElement elementType = namedType.TypeArguments[0];
            for (int i = 0; i < dsonArray.Count; i++) {
                DsonValue element = dsonArray[i];
                if (element.DsonType == DsonType.Array || element.DsonType == DsonType.Object) {
                    dsonArray[i] = RepairFieldValue((DSNamedType)elementType, element);
                } else {
                    CheckValue(elementType, element);
                }
            }
            return container;
        }
        if (CodeGeneratorHelper.IsDictionaryType(namedType)) {
            // 修正Map的Value - 覆盖数据不会导致迭代抛出异常
            DsonObject<string> dsonObject = (DsonObject<string>)container;
            DSTypeElement elementType = namedType.TypeArguments[1];
            foreach (var pair in dsonObject) {
                DsonValue element = pair.Value;
                if (element.DsonType == DsonType.Array || element.DsonType == DsonType.Object) {
                    dsonObject[pair.Key] = RepairFieldValue((DSNamedType)elementType, element);
                } else {
                    CheckValue(elementType, element);
                }
            }
            return container;
        }
        // 修正自定义结构（或内置结构）中的字段
        // 如果是多态数据，需要从Container中拿到真实的类型名，再拿到真实的类型，再根据真实类型修正数据
        string clsName = GetClsName(container);
        if (clsName != null) {
            namedType = _dsRepository.ResolveDsonTypeName(clsName) ?? throw new Exception($"invalid serial name: {clsName}");
        }
        List<DSField> fields = namedType.GetFields(true, fieldListPool.Acquire());
        if (fields.Count == 0) {
            goto release;
        }
        if (container.DsonType == DsonType.Object) {
            DsonObject<string> dsonObject = (DsonObject<string>)container;
            foreach (DSField field in fields) {
                if (!dsonObject.TryGetValue(field.SimpleName, out DsonValue element)) {
                    continue;
                }
                DSTypeElement elementType = field.Type;
                if (element.DsonType == DsonType.String && !DSUtil.IsStringType(elementType)) {
                    element = GetValue(elementType, element.AsString());
                    dsonObject[field.SimpleName] = element;
                } else if (element.DsonType == DsonType.Array || element.DsonType == DsonType.Object) {
                    dsonObject[field.SimpleName] = RepairFieldValue((DSNamedType)elementType, element);
                } else {
                    CheckValue(elementType, element);
                }
            }
            // 如果是内置类型，可能没有字段声明 -- 重排序字段可能导致数据丢失，如Pair
            if (fields.Count > 1) {
                ResortField(dsonObject, namedType, fields);
            }
        } else if (container.DsonType == DsonType.Array) {
            DsonArray<string> dsonArray = (DsonArray<string>)container;
            // 自定义结构被解析为数组是可以正常解码的 -- 数据长度可能小于字段长度
            for (int i = 0, count = Math.Min(fields.Count, dsonArray.Count); i < count; i++) {
                DsonValue element = dsonArray[i];
                DSTypeElement elementType = fields[i].Type;
                if (element.DsonType == DsonType.String && !DSUtil.IsStringType(elementType)) {
                    element = GetValue(elementType, element.AsString());
                    dsonArray[i] = element;
                } else if (element.DsonType == DsonType.Array || element.DsonType == DsonType.Object) {
                    dsonArray[i] = RepairFieldValue((DSNamedType)elementType, element);
                } else {
                    CheckValue(elementType, element);
                }
            }
        }
        release:
        fieldListPool.Release(fields);

        // 自定义结构和内建结构都支持修正
        DSTypeHandler handler = _dsRepository.GetTypeHandler(namedType.OriginDefine.FullName);
        return handler != null ? handler.ConvertValue(_dsRepository, namedType, container) : container;
    }

    private static string? GetClsName(DsonValue container) {
        DsonHeader<string> header = container switch
        {
            DsonObject<string> dsonObject => dsonObject.Header,
            DsonArray<string> dsonArray => dsonArray.Header,
            _ => null
        };
        if (header == null || !header.TryGetValue(DsonHeader.Names_ClassName, out DsonValue value)) {
            return null;
        }
        return value.AsString();
    }

    /** 检查数据兼容性 */
    private static void CheckValue(DSTypeElement elementType, DsonValue dsonValue) {
        if (DSUtil.IsNumberType(elementType)) {
            if (!dsonValue.DsonType.IsNumber()) {
                throw new Exception($"invalid value: {dsonValue}");
            }
            return;
        }
        if (DSUtil.IsStringType(elementType)) {
            if (dsonValue.DsonType != DsonType.String) {
                throw new Exception($"invalid value: {dsonValue}");
            }
            return;
        }
        if (DSUtil.IsBoolType(elementType)) {
            if (dsonValue.DsonType != DsonType.Bool && !dsonValue.DsonType.IsNumber()) {
                throw new Exception($"invalid value: {dsonValue}");
            }
            return;
        }
        // Nullable要检查？
    }

    /// <summary>
    /// 根据Class的字段顺序重排序Dson数据 -- 使得解码时能顺序解码以避免内存碎片
    ///
    /// 1.该逻辑依赖于正确处理了多态，否则可能导致子类数据丢失。
    /// 2.理论上还可以加载类型的默认实例，初始化为对应的默认值，但表格模块应该不太需要。
    /// </summary>
    /// <param name="dsonObject"></param>
    /// <param name="namedType"></param>
    /// <param name="fields"></param>
    private void ResortField(DsonObject<string> dsonObject, DSNamedType namedType, List<DSField> fields) {
        LinkedDictionary<string, DsonValue> dictionary = dictionaryPool.Acquire();
        foreach (DSField field in fields) {
            if (dsonObject.TryGetValue(field.SimpleName, out DsonValue element)) {
                dictionary[field.SimpleName] = element;
            } else {
                dictionary[field.SimpleName] = GetValue(field.Type, null); // 填充默认值
            }
        }
        dsonObject.Clear();
        dsonObject.PutAll(dictionary);
        dictionaryPool.Release(dictionary);
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

    private static DsonInt32 ParseEnum(DSNamedType namedType, string rawValue) {
        int value = 0;
        if (rawValue.IndexOf('|') > 0) { // A | B | C
            foreach (string e in rawValue.Split('|', StringSplitOptions.TrimEntries)) {
                value |= GetEnumValue(namedType, e);
            }
        } else {
            value = GetEnumValue(namedType, rawValue);
        }
        return new DsonInt32(value);
    }

    private static int GetEnumValue(DSNamedType namedType, string rawValue) {
        rawValue = rawValue.Trim();
        if (int.TryParse(rawValue, out int value)) {
            return value;
        }
        // 虽然不推荐通过名字表达枚举，但还是兼容一下
        DSEnumValue enumValue = namedType.GetEnumValue(rawValue, ignoreCase: true);
        if (enumValue == null) {
            throw new Exception($"invalid enumValue {rawValue}, type: {namedType.SimpleName}");
        }
        return enumValue.Number;
    }

    private static ExtDateTime ParseDateTime(string rawValue) {
        // 比标准的Dson增加了空格支持
        string format = rawValue.IndexOf('T') > 0 ? "yyyy-MM-ddTHH:mm:ss" : "yyyy-MM-dd HH:mm:ss";
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