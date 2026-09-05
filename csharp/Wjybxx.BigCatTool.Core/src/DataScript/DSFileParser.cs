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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Wjybxx.BigCatTool.Core;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;
using LineInfo = Wjybxx.BigCatTool.Core.LineInfo;
using TypeName = Wjybxx.Commons.Poet.TypeName;
using DsonLineInfo = Wjybxx.Dson.Text.LineInfo;

namespace Wjybxx.BigCatTool.DataScript
{
/// <summary>
/// 与DS的解析器不同，我们需要尽可能的保证不可变性
/// </summary>
public class DSFileParser
{
#nullable disable
    private readonly FileInfo file;
    private readonly LineEnumerator lineIterator;
    private readonly DSFile dsFile;

    /** 当前递归深度 */
    private int _recursionDepth;
    /** 当前上下文 */
    private Context _context;
    /** 文件支持的宏注解类型 */
    private readonly HashSet<string> macroTypes = new(4);

    private DSFileParser(FileInfo file, bool isVirtual, IEnumerator<string> lineIterator) {
        this.file = file;
        this.lineIterator = new LineEnumerator(lineIterator);

        this.dsFile = new DSFile(file.Name, isVirtual);
        this._context = new Context(null, DSContextType.File, dsFile);
        this._context.started = true;
    }

    /// <summary>
    /// 解析DataScript文件为内存结构
    /// </summary>
    /// <param name="fileInfo">protobuf文件</param>
    /// <returns></returns>
    public static DSFile Parse(FileInfo fileInfo) {
        using (IEnumerator<string> lineIterator = File.ReadLines(fileInfo.FullName).GetEnumerator()) {
            DSFileParser parser = new DSFileParser(fileInfo, false, lineIterator);
            parser.Parse();
            return parser.dsFile;
        }
    }

    /// <summary>
    /// 解析DataScript文件为内存结构
    /// 
    /// 该方法允许直接通过字符串构建最终数据。
    /// </summary>
    /// <param name="fileInfo">文件信息</param>
    /// <param name="text">文件内容</param>
    /// <param name="isVirtual">是否是虚拟文件</param>
    /// <returns></returns>
    public static DSFile Parse(FileInfo fileInfo, string text, bool isVirtual = false) {
        using (IEnumerator<string> lineIterator = ToolUtil.GetLines(text).GetEnumerator()) {
            DSFileParser parser = new DSFileParser(fileInfo, isVirtual, lineIterator);
            parser.Parse();
            return parser.dsFile;
        }
    }

#nullable restore

    private void Parse() {
        try {
            while (lineIterator.MoveNext()) {
                LineInfo curLine = LineInfo.Parse(lineIterator.CurrentLn, lineIterator.Current);
                if (!_context.started) {
                    CheckStart(curLine);
                    continue;
                }
                // @region注解支持
                Annotation annotation;
                if (curLine.IsCommentLine
                    && (annotation = Annotation.TryParseAnnotation(curLine.comment, curLine.ln)) != null
                    && macroTypes.Contains(annotation.type)) {
                    dsFile.AddAnnotation(annotation); // 宏注解固定为文件级别
                    continue;
                }
                switch (_context.contextType) {
                    case DSContextType.File: {
                        FileReadLine(curLine);
                        break;
                    }
                    case DSContextType.Class:
                    case DSContextType.Struct: {
                        ClassOrStructReadLine(curLine);
                        break;
                    }
                    case DSContextType.Service: {
                        ServiceReadLine(curLine);
                        break;
                    }
                    case DSContextType.Enum: {
                        EnumReadLine(curLine);
                        break;
                    }
                    default: {
                        throw new AssertionError();
                    }
                }
            }
        }
        catch (Exception ex) {
            throw new IOException($"fileName: {file.Name}, ln: {lineIterator.CurrentLn}", ex);
        }
    }

    #region file

    private void FileReadLine(LineInfo lineInfo) {
        if (!lineInfo.HasContent) {
            TryAddComment(lineInfo);
            return;
        }
        string content = lineInfo.content;
        string firstWord = FirstWord(content);
        switch (firstWord) {
            // 内嵌结构
            case DSKeywords.CLASS: {
                ReadStartContainer(DSContextType.Class, lineInfo);
                return;
            }
            case DSKeywords.STRUCT: {
                ReadStartContainer(DSContextType.Struct, lineInfo);
                return;
            }
            case DSKeywords.ENUM: {
                ReadStartContainer(DSContextType.Enum, lineInfo);
                return;
            }
            case DSKeywords.SERVICE: {
                ReadStartContainer(DSContextType.Service, lineInfo);
                return;
            }
            case DSKeywords.INST: {
                DSInst inst = ReadInst(lineInfo);
                _context.AsFile().AddEnclosedElement(inst);
                // 文件选项
                if (inst.Name == "@file") {
                    InitFileOptions(inst);
                    break;
                }
                return;
            }
            case DSKeywords.IMPORT: {
                _context.ClearCommentLines();
                // import public "common.proto";
                // import "other.proto";
                int startIdx = content.IndexOf('"');
                int endIdx = content.LastIndexOf('"');
                string fileName = content.Substring2(startIdx + 1, endIdx);
                string? modifier = content.Substring2(firstWord.Length, startIdx).Trim();
                if (string.IsNullOrWhiteSpace(modifier)) {
                    modifier = null;
                }
                _context.AsFile().AddImport(fileName, modifier);
                return;
            }
            default: {
                _context.ClearCommentLines();
                return;
            }
        }
    }

    private void InitFileOptions(DSInst inst) {
        DsonObject<string> options = inst.DsonValue.AsObject();
        dsFile.GetOptions().PutAll(options);
        // 更新宏注解缓存
        if (options.TryGetValue(DSKeywords.MACRO_TYPES, out DsonValue dsonValue) && dsonValue is DsonArray<string> array) {
            foreach (DsonValue ele in array) {
                macroTypes.Add(ele.AsString());
            }
        }
    }

    #region inst

    /// <summary>
    /// 解析实例
    /// </summary>
    /// <param name="lineInfo"></param>
    /// <exception cref="NotImplementedException"></exception>
    private DSInst ReadInst(LineInfo lineInfo) {
        // 注意：实例可能是数组，数组不能有from
        // inst _name from t1, t2 {
        // inst _name [
        // 要想安全正确解析这段语法，先读取到 '{' 和 '[ 可以降低难度
        string content = lineInfo.content;
        while (content.LastIndexOf('{') < 0 && content.LastIndexOf('[') < 0) {
            lineIterator.MoveNext();
            content += lineIterator.Current;
        }
        //
        int startIdx = content.IndexOf(' '); // 跳过关键词
        int endIdx = content.LastIndexOf('{'); // 去除'{'或'['
        bool isArray = endIdx < 0;
        if (isArray) {
            endIdx = content.LastIndexOf('[');
        }
        string name;
        IEnumerable<string> templates;
        int spIdx = content.IndexOf("from", StringComparison.Ordinal);
        if (spIdx < 0) {
            name = content.Substring2(startIdx, endIdx).Trim();
            templates = Array.Empty<string>();
        } else {
            name = content.Substring2(startIdx, spIdx).Trim();
            templates = ObjectUtil.SplitAndTrim(content.Substring2(spIdx + "from".Length, endIdx), ',');
        }
        string firstLine = content.Substring(endIdx);
        string value = ScanDsonValue(firstLine, lineInfo.ln);
        var inst = new DSInst(name, value, templates);
        // 统一@开头的实例为特殊实例
        if (name[0] == '@') {
            inst.DsonValue = Dsons.FromDson(inst.Value);
        }
        return inst;
    }

    private string ScanDsonValue(string firstLine, int ln) {
        StringBuilder sb = ConcurrentObjectPool.SharedStringBuilderPool.Acquire();
        try {
            DsonScanner scanner = Dsons.NewLinesScanner(new DsonLineIterator(lineIterator, firstLine, sb), ln);
            DsonToken firstToken = scanner.NextToken(skipValue: true);
            DsonToken token = firstToken;
            int stack = 1;
            while (stack > 0) {
                token = scanner.NextToken(skipValue: true);
                switch (token.type) {
                    case DsonTokenType.BeginArray:
                    case DsonTokenType.BeginObject:
                    case DsonTokenType.BeginHeader: {
                        stack++;
                        break;
                    }
                    case DsonTokenType.EndArray:
                    case DsonTokenType.EndObject: {
                        if (--stack == 0) {
                            goto end;
                        }
                        break;
                    }
                    case DsonTokenType.Eof: {
                        goto end;
                    }
                }
            }
            end:
            bool match = firstToken.type == DsonTokenType.BeginArray
                ? token.type == DsonTokenType.EndArray
                : token.type == DsonTokenType.EndObject;
            if (!match) {
                throw DsonIOException.InvalidTokenType(DsonContextType.TopLevel, token);
            }
            return sb.ToString();
        }
        finally {
            ConcurrentObjectPool.SharedStringBuilderPool.Release(sb);
        }
    }

    #endregion

    #endregion

    #region class

    private void ClassOrStructReadLine(LineInfo lineInfo) {
        if (!lineInfo.HasContent) {
            TryAddComment(lineInfo);
            return;
        }
        string content = lineInfo.content;
        string firstWord = FirstWord(content);
        switch (firstWord) {
            case DSKeywords.SERVICE: {
                throw new IOException("Services should not be nested within class");
            }
            case DSKeywords.CLASS: {
                ReadStartContainer(DSContextType.Class, lineInfo);
                return;
            }
            case DSKeywords.STRUCT: {
                ReadStartContainer(DSContextType.Struct, lineInfo);
                return;
            }
            case DSKeywords.ENUM: {
                ReadStartContainer(DSContextType.Enum, lineInfo);
                return;
            }
            // 函数
            case DSKeywords.FUNC: {
                DSMethod method = ParseMethod(_context.PopCommentLines(), lineInfo);
                if (!_context.methodNumbers.Add(method.Number)) {
                    throw new IOException("duplicate method number: " + method.Number);
                }
                _context.container.AddEnclosedElement(method);
                return;
            }
            case "}": {
                // 结束行
                _context.ClearCommentLines();
                ReadEndContainer(lineInfo);
                return;
            }
            default: {
                // 判断是否字段 type name = number;
                if (content.IndexOf('=') > 0) {
                    DSField field = ParseField(_context.PopCommentLines(), lineInfo);
                    if (!_context.fieldNumbers.Add(field.Number)) {
                        throw new IOException("duplicate field number: " + field.Number);
                    }
                    _context.container.AddEnclosedElement(field);
                } else {
                    throw new IOException("unrecognized content: " + content);
                }
                return;
            }
        }
    }

    /** 解析字段 */
    private static DSField ParseField(List<string> commentLines, LineInfo lineInfo) {
        string content = lineInfo.content;
        EnsureEndWithSemicolon(content);
        // readonly type name = number;
        // List<int32> itemIds = 1;
        bool isReadonly = FirstWord(content) switch
        {
            DSKeywords.READONLY => true,
            DSKeywords.PUBLIC => throw new Exception("public is not supported"),
            DSKeywords.PRIVATE => throw new Exception("private is not supported"),
            _ => false
        };
        if (isReadonly) {
            content = content.Substring(DSKeywords.READONLY.Length + 1).TrimStart();
        }
        int eqIdx = content.IndexOf('=');
        int genericEndIdx = content.LastIndexOf('>', eqIdx); // map泛型结束符--兼容多层泛型
        int typeEndIdx = genericEndIdx > 0 ? genericEndIdx + 1 : content.IndexOf(' ');
        int opIdx = content.IndexOf('['); // 可选项开始符

        string type = ToolUtil.DeleteWhitespace(content.Substring2(0, typeEndIdx)); // 删除空白字符
        string name = content.Substring2(typeEndIdx + 1, eqIdx).Trim();
        int number;
        {
            string numberString = opIdx > 0
                ? content.Substring2(eqIdx + 1, opIdx)
                : content.Substring2(eqIdx + 1, content.Length - 1); // -1去掉 ';'
            number = int.Parse(numberString.Trim());
        }
        // 
        DSField field = new DSField(name, type, number, isReadonly);
        field.StartLine = lineInfo.ln;
        field.EndLine = lineInfo.ln;
        // 追加注释
        DrainCommentLine(field, commentLines, lineInfo.comment);
        return field;
    }

    #endregion

    #region service

    private void ServiceReadLine(LineInfo lineInfo) {
        if (!lineInfo.HasContent) {
            TryAddComment(lineInfo);
            return;
        }
        string content = lineInfo.content;
        string firstWord = FirstWord(content);
        switch (firstWord) {
            // 内嵌结构
            case DSKeywords.SERVICE: {
                throw new IOException("Services should not be nested within service");
            }
            case DSKeywords.CLASS: {
                ReadStartContainer(DSContextType.Class, lineInfo);
                return;
            }
            case DSKeywords.STRUCT: {
                ReadStartContainer(DSContextType.Struct, lineInfo);
                return;
            }
            case DSKeywords.ENUM: {
                ReadStartContainer(DSContextType.Enum, lineInfo);
                return;
            }
            // 函数
            case DSKeywords.FUNC: {
                DSMethod method = ParseMethod(_context.PopCommentLines(), lineInfo);
                if (!_context.methodNumbers.Add(method.Number)) {
                    throw new IOException("duplicate method number: " + method.Number);
                }
                _context.container.AddEnclosedElement(method);
                return;
            }
            default: {
                // 可能是结束行
                _context.ClearCommentLines();
                if (firstWord == "}") {
                    ReadEndContainer(lineInfo);
                }
                return;
            }
        }
    }

    private static DSMethod ParseMethod(List<string> commentLines, LineInfo lineInfo) {
        string content = lineInfo.content;
        EnsureEndWithSemicolon(content);
        // 我们的语法支持为参数命名，还支持无参和无返回结果
        // func Search(SearchRequest request) returns (SearchResponse) = 1;
        string name;
        string? argType;
        string? argName;
        int argEnd;
        {
            int argStart = content.IndexOf('(');
            name = content.Substring2(DSKeywords.FUNC.Length, argStart).Trim(); // 跳过'func '

            argEnd = content.IndexOf(')');
            string args = content.Substring2(argStart + 1, argEnd).Trim(); // 去掉两端空格
            if (string.IsNullOrWhiteSpace(args)) {
                argType = null;
                argName = null;
            } else {
                int splitIdx = args.LastIndexOf(' '); // 可避免泛型参数<>中的空白
                if (splitIdx < 0) {
                    argType = args;
                    argName = null;
                } else {
                    argType = args.Substring2(0, splitIdx).Trim();
                    argName = args.Substring(splitIdx + 1).Trim();
                }
            }
        }
        string? resultType = null;
        int rEnd;
        {
            int rStart = content.IndexOf('(', argEnd);
            rEnd = content.IndexOf(')', rStart);
            string results = content.Substring2(rStart + 1, rEnd).Trim();
            if (!string.IsNullOrWhiteSpace(results)) {
                resultType = results;
            }
        }
        int number;
        {
            int splitIdx = content.IndexOf('=', rEnd);
            if (splitIdx < 0) {
                throw new IOException("number is absent");
            }
            string str = content.Substring2(splitIdx + 1, content.Length - 1); // -1去掉 ';'
            number = int.Parse(str.Trim());
        }
        DSMethod method = new DSMethod(name, argType, argName, resultType, number)
        {
            StartLine = lineInfo.ln,
            EndLine = lineInfo.ln
        };
        // 追加注释
        DrainCommentLine(method, commentLines, lineInfo.comment);
        return method;
    }

    #endregion

    #region enum

    private void EnumReadLine(LineInfo lineInfo) {
        if (!lineInfo.HasContent) {
            TryAddComment(lineInfo);
            return;
        }
        string content = lineInfo.content;
        string firstWord = FirstWord(content);
        switch (firstWord) {
            case "}": {
                // 结束行
                _context.ClearCommentLines();
                ReadEndContainer(lineInfo);
                return;
            }
            default: {
                // 判断是否是枚举 name = number;
                if (content.IndexOf('=') > 0) {
                    DSEnumValue enumValue = ParseEnumValue(_context.PopCommentLines(), lineInfo);
                    _context.container.AddEnclosedElement(enumValue);
                } else {
                    throw new IOException("unrecognized content: " + content);
                }
                return;
            }
        }
    }

    private DSEnumValue ParseEnumValue(List<string> commentLines, LineInfo lineInfo) {
        string content = lineInfo.content;
        EnsureEndWithSemicolon(content);
        int eqIdx = content.IndexOf('=');
        int opIdx = content.IndexOf('['); // 可选项开始符

        string name = content.Substring2(0, eqIdx).Trim();
        int number;
        {
            string numberString = opIdx > 0
                ? content.Substring2(eqIdx + 1, opIdx)
                : content.Substring2(eqIdx + 1, content.Length - 1); // -1去掉 ';'
            number = ipNumberRegex.IsMatch(numberString)
                ? ParseIpNumber(numberString) // 支持RGBA或IP样式
                : DsonTexts.ParseInt32(numberString.Trim()); // 支持16进制
        }
        DSEnumValue enumValue = new DSEnumValue(name, number)
        {
            StartLine = lineInfo.ln,
            EndLine = lineInfo.ln
        };
        // 追加注释
        DrainCommentLine(enumValue, commentLines, lineInfo.comment);
        return enumValue;
    }

    private static readonly Regex ipNumberRegex = new Regex(@"^(?:\d{1,3}\.){3}\d{1,3}$", RegexOptions.Compiled);

    private static int ParseIpNumber(string strValue) {
        string[] parts = ObjectUtil.SplitAndTrim(strValue, '.');
        uint value = (byte.Parse(parts[0]) & 0xFFU) << 24
                     | (byte.Parse(parts[1]) & 0xFFU) << 16
                     | (byte.Parse(parts[2]) & 0xFFU) << 8
                     | (byte.Parse(parts[3]) & 0xFFU);
        // 枚举只能是int，因此首个byte不能超过127
        int v = (int)value;
        if (v < 0) throw new IOException($"invalid enum number: {strValue}");
        return v;
    }

    #endregion

    /** 检查开始行 */
    private void CheckStart(LineInfo lineInfo) {
        // '{' 之前不能出现其它内容
        if (!lineInfo.HasContent) {
            return;
        }
        if (lineInfo.content != "{") {
            throw new IOException("invalid start line : " + lineInfo.content);
        }
        _context.started = true;
    }

    /** 将注释行追加到context */
    private void TryAddComment(LineInfo lineInfo) {
        if (lineInfo.IsCommentLine) {
            _context.AddCommentLine(lineInfo.comment);
        } else {
            // 空白行中断注释
            _context.ClearCommentLines();
        }
    }

    #region container

    /// <summary>
    /// 
    /// </summary>
    /// <param name="contextType">新上下文类型</param>
    /// <param name="lineInfo">文件行</param>
    private void ReadStartContainer(DSContextType contextType, LineInfo lineInfo) {
        if (_recursionDepth > 32) throw new InvalidOperationException("proto had too many levels of nesting");
        // 解析容器类型名
        ParseTypeName(lineInfo.content,
            out ClassName className,
            out List<DSTypeParameter> typeParameters,
            out string? baseTypeSymbol);
        //
        Context parent = this._context;
        Context context;
        switch (contextType) {
            case DSContextType.Class: {
                context = new Context(parent, DSContextType.Class, DSNamedType.NewClassType(className, typeParameters,
                    baseTypeSymbol));
                break;
            }
            case DSContextType.Struct: {
                Debug.Assert(baseTypeSymbol == null);
                context = new Context(parent, DSContextType.Struct, DSNamedType.NewStructType(className, typeParameters));
                break;
            }
            case DSContextType.Service: {
                Debug.Assert(typeParameters.Count == 0);
                context = new Context(parent, DSContextType.Service, DSNamedType.NewServiceType(className));
                break;
            }
            case DSContextType.Enum: {
                Debug.Assert(baseTypeSymbol == null);
                Debug.Assert(typeParameters.Count == 0);
                context = new Context(parent, DSContextType.Enum, DSNamedType.NewEnumType(className));
                break;
            }
            default: throw new AssertionError();
        }
        context.started = true;
        context.container.StartLine = lineInfo.ln;

        parent.container.AddEnclosedElement(context.container);
        DrainCommentLine(context.container, parent.PopCommentLines(), lineInfo.comment);
        _recursionDepth++;
        _context = context;
    }

    private void ReadEndContainer(LineInfo lineInfo) {
        Context context = _context;
        if (context.parent == null || !context.started) {
            throw new InvalidOperationException();
        }
        context.container.EndLine = lineInfo.ln;

        _recursionDepth--;
        _context = context.parent;
    }

    private void ParseTypeName(string content,
                               out ClassName className,
                               out List<DSTypeParameter> typeParameters,
                               out string? baseTypeSymbol) {
        // class Dictionary<TKey, TValue> : IDictionary<TKey, TValue> where T : struct {
        // 要想安全正确解析这段语法，先读取到 '{' 是很重要的 -- 用户不能在'{'后包含内容
        while (content.LastIndexOf('{') < 0) {
            lineIterator.MoveNext();
            content += lineIterator.Current;
        }
        //
        int startIdx = content.IndexOf(' '); // 跳过关键词
        int endIdx = content.LastIndexOf('{'); // 去除'{'
        string[] tokens = content.Substring2(startIdx, endIdx).Split("where", StringSplitOptions.RemoveEmptyEntries);
        // 名字和超类在第一个字符串
        string nameAndBaseType = tokens[0];
        string name;
        {
            int idx = nameAndBaseType.IndexOf(':');
            if (idx < 0) {
                name = ObjectUtil.DeleteWhitespace(nameAndBaseType);
                baseTypeSymbol = null;
            } else {
                name = ObjectUtil.DeleteWhitespace(nameAndBaseType.Substring2(0, idx));
                baseTypeSymbol = ObjectUtil.DeleteWhitespace(nameAndBaseType.Substring(idx + 1));
            }
        }
        // 先拷贝外部类的泛型变量
        List<TypeName> typeParameterNames = new List<TypeName>();
        typeParameters = new List<DSTypeParameter>();
        DSNamedType? enclosingType = null;
        if (_context.container is DSNamedType namedType) {
            enclosingType = namedType;
            foreach (DSTypeParameter typeParameter in enclosingType.TypeParameters) {
                typeParameterNames.Add(typeParameter.TypeName);
                typeParameters.Add(new DSTypeParameter(typeParameter)); // 拷贝构造函数
            }
        }
        // 解析新增的的泛型变量
        int tpStart = name.IndexOf('<');
        if (tpStart < 0) {
            className = GetClassName(dsFile, enclosingType, name, typeParameterNames);
            return;
        }
        string[] tpNames = ObjectUtil.SplitAndTrim(name.Substring2(tpStart + 1, name.Length - 1), ',');
        foreach (string tpName in tpNames) {
            typeParameterNames.Add(TypeParameterName.Get(tpName));
            typeParameters.Add(new DSTypeParameter(tpName, TypeParameterConstraints.None));
        }
        // 解析泛型变量约束 -- 从1开始，跳过name
        for (int tokenIdx = 1; tokenIdx < tokens.Length; tokenIdx++) {
            string token = tokens[tokenIdx];
            int spIdx = token.IndexOf(':');
            if (spIdx < 0) throw new IOException(content);

            string tpName = token.Substring2(0, spIdx).Trim();
            int tpIdx = typeParameters.FindIndex(e => e.Name == tpName);
            if (tpIdx < 0) throw new IOException(content);

            TypeParameterConstraints tpConstraints = ParseConstraints(token.Substring(spIdx + 1));
            typeParameters[tpIdx] = new DSTypeParameter(tpName, tpConstraints);
        }
        name = name.Substring2(0, tpStart);
        className = GetClassName(dsFile, enclosingType, name, typeParameterNames);
    }

    private static ClassName GetClassName(DSFile dsFile, DSNamedType? enclosingType, string name, List<TypeName> typeParameterNames) {
        if (enclosingType != null) {
            return enclosingType.TypeName.NestedClass(name, typeParameterNames, false);
        }
        // 注意：这里的命名空间是文件简单名
        return ClassName.Get(dsFile.Name, name, typeParameterNames);
    }

    private static TypeParameterConstraints ParseConstraints(string constraintsToken) {
        TypeParameterConstraints constraints = TypeParameterConstraints.None;
        foreach (string s in ObjectUtil.SplitAndTrim(constraintsToken, ',')) {
            if (s == DSKeywords.STRUCT) {
                constraints |= TypeParameterConstraints.ValueTypeConstraint;
            } else if (s == DSKeywords.CLASS) {
                constraints |= TypeParameterConstraints.ReferenceTypeConstraint;
            } else if (s == DSKeywords.NEW) {
                constraints |= TypeParameterConstraints.DefaultConstructorConstraint;
            }
        }
        return constraints;
    }

    #endregion

    #region util

    /** 将注释添加到目标元素 */
    private static void DrainCommentLine(DSElement element, List<string> commentLines, string? trailingComment) {
        foreach (string commentLine in commentLines) {
            element.AddComment(commentLine);
            Annotation? annotation = Annotation.TryParseAnnotation(commentLine);
            if (annotation != null) {
                element.AddAnnotation(annotation);
            }
        }
        if (!string.IsNullOrWhiteSpace(trailingComment)) {
            element.AddComment(trailingComment);
            Annotation? annotation = Annotation.TryParseAnnotation(trailingComment);
            if (annotation != null) {
                element.AddAnnotation(annotation);
            }
        }
    }

    /** 解析Option -- 不适用字段 */
    private static KeyValuePair<string, string> ParseOption(string content) {
        int startIdx = content.IndexOf(' '); // 跳过 'option'
        int eqIdx = content.IndexOf('=');
        int lastIdx = content.LastIndexOf(';'); // 去掉末尾分号

        string name = content.Substring2(startIdx + 1, eqIdx).Trim();
        string value = content.Substring2(eqIdx + 1, lastIdx).Trim();
        value = ToolUtil.Unquote(value); // 去掉双引号
        return new KeyValuePair<string, string>(name, value);
    }

    /** 解析第一个单词 -- 可能是大括号 */
    private static string FirstWord(string content) {
        Debug.Assert(!string.IsNullOrWhiteSpace(content));
        for (int idx = 0; idx < content.Length; idx++) {
            char c = content[idx];
            if (char.IsWhiteSpace(c) || c == '<') { // map<k,v>
                return content.Substring2(0, idx);
            }
        }
        return content; // { or }
    }

    /** 确保内容行以分号 ';' 结尾 */
    private static void EnsureEndWithSemicolon(string content) {
        if (content[content.Length - 1] != ';') {
            throw new IOException(content);
        }
    }

    #endregion

#nullable disable

    private class Context
    {
        public readonly Context parent;
        public readonly DSContextType contextType;
        public readonly DSElement container;

        /** 是否已读取到开始符号 '{' */
        public bool started;
        /** 下一个元素的注释缓存 -- 内容执行了trim */
        public readonly List<string> commentLines = new();
        /** 校验字段number */
        public readonly HashSet<int> fieldNumbers = new();
        /** 校验方法number */
        public readonly HashSet<int> methodNumbers = new();

        public Context(Context parent, DSContextType contextType, DSElement container) {
            this.parent = parent;
            this.contextType = contextType;
            this.container = container;
        }

        public DSFile AsFile() {
            return (DSFile)container;
        }

        public DSNamedType AsTypeElement() {
            return (DSNamedType)container;
        }

        /** 添加注释行 */
        public void AddCommentLine(string comment) {
            commentLines.Add(comment);
        }

        /** 弹出缓存的注释行 */
        public List<string> PopCommentLines() {
            if (commentLines.Count == 0) {
                return new List<string>();
            }
            List<string> result = new(commentLines);
            commentLines.Clear();
            return result;
        }

        /** 清理注释行 */
        public void ClearCommentLines() {
            commentLines.Clear();
        }
    }

    private class DsonLineIterator : IEnumerator<DsonLineInfo>
    {
        private readonly LineEnumerator lineEnumerator;
        private readonly StringBuilder sb;

        private string? firstLine;
        private string? currentLine;
        private int startPos; // 新行的开始位置

        public DsonLineIterator(LineEnumerator lineEnumerator, string firstLine, StringBuilder sb) {
            this.lineEnumerator = lineEnumerator;
            this.firstLine = firstLine;
            this.sb = sb;
        }

        public DsonLineInfo Current => new DsonLineInfo(lineEnumerator.CurrentLn,
            startPos, startPos + currentLine!.Length, DsonLineInfo.StateLf, currentLine);
        object IEnumerator.Current => Current;

        public bool MoveNext() {
            if (currentLine != null) {
                startPos += currentLine.Length + 2;
                sb.Append('\n');
            }
            if (firstLine != null) {
                currentLine = firstLine;
                sb.Append(currentLine);
                firstLine = null;
                return true;
            }
            if (lineEnumerator.MoveNext()) {
                currentLine = lineEnumerator.Current;
                sb.Append(currentLine);
                return true;
            }
            return false;
        }

        public void Reset() {
        }

        public void Dispose() {
        }
    }
}
}