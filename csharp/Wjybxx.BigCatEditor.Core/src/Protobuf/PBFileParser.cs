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
using System.Diagnostics;
using System.IO;
using Wjybxx.BigCatEditor.Core;
using Wjybxx.Commons;

namespace Wjybxx.BigCatEditor.Protobuf
{
/// <summary>
/// pb文件解析器
/// </summary>
public sealed class PBFileParser
{
#nullable disable
    private readonly FileInfo file;
    private readonly IEnumerator<string> lineIterator;

    /** 当前递归深度 */
    private int recursionDepth;
    /** 当前上下文 */
    private Context context;
    /** 最终输出 */
    private readonly PBFile pbFile;

    public PBFileParser(FileInfo file, IEnumerator<string> lineIterator) {
        this.file = file;
        this.lineIterator = lineIterator;

        this.pbFile = new PBFile();
        this.context = new Context(null, ContextType.File, pbFile);
        this.context.started = true;
    }

    /// <summary>
    /// 解析protobuf文件为内存结构
    /// </summary>
    /// <param name="fileInfo">protobuf文件</param>
    /// <returns></returns>
    public static PBFile Parse(FileInfo fileInfo) {
        using (IEnumerator<string> lineIterator = File.ReadLines(fileInfo.FullName).GetEnumerator()) {
            PBFileParser parser = new PBFileParser(fileInfo, lineIterator);
            parser.Parse();
            return parser.pbFile;
        }
    }

#nullable enable

    private void Parse() {
        pbFile.FileName = file.Name;
        pbFile.SimpleName = Path.GetFileNameWithoutExtension(file.Name);

        LineInfo curLine = LineInfo.EMPTY;
        try {
            int ln = 0;
            while (lineIterator.MoveNext()) {
                curLine = LineInfo.Parse(++ln, lineIterator.Current!);
                switch (context.contextType) {
                    case ContextType.File: {
                        FileReadLine(curLine);
                        break;
                    }
                    case ContextType.Service: {
                        ServiceReadLine(curLine);
                        break;
                    }
                    case ContextType.Message: {
                        MessageReadLine(curLine);
                        break;
                    }
                    case ContextType.Enum: {
                        EnumReadLine(curLine);
                        break;
                    }
                    case ContextType.Oneof: {
                        OneofReadLine(curLine);
                        break;
                    }
                    default: {
                        throw new AssertionError();
                    }
                }
            }
        }
        catch (Exception ex) {
            throw new PBParserException($"fileName: {file.Name}, ln: {curLine.ln}", ex);
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
            case PBKeywords.SERVICE: {
                ReadStartContainer(ContextType.Service, lineInfo);
                return;
            }
            case PBKeywords.MESSAGE: {
                ReadStartContainer(ContextType.Message, lineInfo);
                return;
            }
            case PBKeywords.ENUM: {
                ReadStartContainer(ContextType.Enum, lineInfo);
                return;
            }
            // 各类options
            case PBKeywords.SYNTAX: {
                context.ClearCommentLines();
                // syntax = "proto3";
                int startIdx = content.IndexOf('"');
                int endIdx = content.LastIndexOf('"');
                context.AsFile().Syntax = content.Substring2(startIdx, endIdx);
                return;
            }
            case PBKeywords.IMPORT: {
                context.ClearCommentLines();
                // import public "new.proto";
                // import "other.proto";
                int startIdx = content.IndexOf('"');
                int endIdx = content.LastIndexOf('"');
                string fileName = content.Substring2(startIdx, endIdx);
                string? modifier = content.Substring2(firstWord.Length, startIdx);
                if (string.IsNullOrWhiteSpace(modifier)) {
                    modifier = null;
                }
                context.AsFile().AddImport(fileName, modifier);
                return;
            }
            case PBKeywords.OPTION: {
                context.ClearCommentLines();
                // option optimize_for = CODE_SIZE;
                // option java_package = "com.example.foo";
                var pair = ParseOption(content);
                context.container.AddOption(pair.Key, pair.Value);
                return;
            }
            default: {
                context.ClearCommentLines();
                return;
            }
        }
    }

    #endregion

    #region message

    private void MessageReadLine(LineInfo lineInfo) {
        if (!context.started) {
            CheckStart(lineInfo);
            return;
        }
        if (!lineInfo.HasContent) {
            TryAddComment(lineInfo);
            return;
        }
        string content = lineInfo.content;
        string firstWord = FirstWord(content);
        switch (firstWord) {
            case PBKeywords.SERVICE: { // 消息内不可嵌套服务
                throw new PBParserException("Services should not be nested within messages");
            }
            case PBKeywords.MESSAGE: {
                ReadStartContainer(ContextType.Message, lineInfo);
                return;
            }
            case PBKeywords.ENUM: {
                ReadStartContainer(ContextType.Enum, lineInfo);
                return;
            }
            case PBKeywords.ONE_OF: {
                ReadStartContainer(ContextType.Oneof, lineInfo);
                return;
            }
            case PBKeywords.OPTION: {
                context.ClearCommentLines();
                var pair = ParseOption(content);
                context.container.AddOption(pair.Key, pair.Value);
                return;
            }
            case PBKeywords.RESERVED: {
                context.ClearCommentLines();
                ParseRevered(context.AsMessage(), content);
                return;
            }
            case "}": {
                // 结束行
                context.ClearCommentLines();
                ReadEndContainer(lineInfo);
                return;
            }
            default: {
                // 判断是否字段 type name = number;
                if (content.IndexOf('=') > 0) {
                    PBField field = ParseField(context.PopCommentLines(), lineInfo);
                    context.container.AddEnclosedElement(field);
                } else {
                    context.ClearCommentLines();
                }
                return;
            }
        }
    }

    /** 解析字段 */
    private static PBField ParseField(List<string> commentLines, LineInfo lineInfo) {
        string content = lineInfo.content;
        EnsureEndWithSemicolon(content);

        PBField field = new PBField();
        // modifiers type name = number [options];
        while (true) {
            int modifierEnd = content.IndexOf(' ');
            string word = content.Substring2(0, modifierEnd);
            if (!PBKeywords.IsFieldModifier(word)) {
                break;
            }
            field.AddModifier(word);
            content = content.Substring(modifierEnd).TrimStart();
        }

        int genericEndIdx = content.IndexOf('>'); // map泛型结束符
        int typeEndIdx = genericEndIdx > 0 ? genericEndIdx + 1 : content.IndexOf(' ');
        int eqIdx = content.IndexOf('=');
        int opIdx = content.IndexOf('['); // 可选项开始符

        string type = Util.DeleteWhitespace(content.Substring2(0, typeEndIdx)); // 删除空白字符
        string name = content.Substring2(typeEndIdx + 1, eqIdx).Trim();
        int number;
        {
            string numberString = opIdx > 0
                ? content.Substring2(eqIdx + 1, opIdx)
                : content.Substring2(eqIdx + 1, content.Length - 1); // -1去掉 ';'
            number = int.Parse(numberString.Trim());
        }
        //
        field.Type = type;
        field.Number = number;
        field.SimpleName = name;
        field.StartLine = lineInfo.ln;
        field.EndLine = lineInfo.ln;
        // 追加注释
        DrainCommentLine(field, commentLines, lineInfo.comment);
        return field;
    }

    /** 解析保留字段信息 */
    private static void ParseRevered(PBTypeElement typeElement, string content) {
        EnsureEndWithSemicolon(content);
        // 不可在同一行reserved声明中同时声明域名字和tag number。
        // reversed 1, 2, 3 to 10;
        // reversed "age", "env";
        // 去掉reserved和分号，再按逗号分割
        string[] values = content.Substring2(PBKeywords.RESERVED.Length, content.Length - 1)
            .Split(',');
        for (int idx = 0; idx < values.Length; idx++) {
            string value = values[idx].Trim();
            if (value[0] == '"') {
                typeElement.AddReservedName(Util.Unquote(value));
                continue;
            }
            // 判断是否有to关键字 -- 是否是范围
            int toIdx = value.IndexOf("to", StringComparison.Ordinal);
            if (toIdx < 0) {
                int number = int.Parse(value);
                typeElement.AddReservedNumber(number);
                continue;
            }
            int start = int.Parse(value.Substring2(0, toIdx).Trim());
            int end = int.Parse(value.Substring(toIdx + 2).Trim()); // 跳过to
            typeElement.AddReservedNumber(start, end);
        }
    }

    #endregion

    #region oneof

    private void OneofReadLine(LineInfo lineInfo) {
        if (!context.started) {
            CheckStart(lineInfo);
            return;
        }
        if (!lineInfo.HasContent) {
            TryAddComment(lineInfo);
            return;
        }
        string content = lineInfo.content;
        string firstWord = FirstWord(content);
        if (firstWord == "}") {
            // 结束行
            context.ClearCommentLines();
            ReadEndContainer(lineInfo);
        } else {
            // 判断是否字段 type name = number;
            if (content.IndexOf('=') > 0) {
                PBField field = ParseField(context.PopCommentLines(), lineInfo);
                context.container.AddEnclosedElement(field);
            } else {
                context.ClearCommentLines();
            }
        }
    }

    #endregion


    #region service

    private void ServiceReadLine(LineInfo lineInfo) {
        if (!context.started) {
            CheckStart(lineInfo);
            return;
        }
        if (!lineInfo.HasContent) {
            TryAddComment(lineInfo);
            return;
        }
        string content = lineInfo.content;
        string firstWord = FirstWord(content);
        switch (firstWord) {
            // 内嵌结构
            case PBKeywords.SERVICE: { // 服务内禁止嵌套服务
                throw new PBParserException("Services should not be nested within service");
            }
            case PBKeywords.MESSAGE: {
                ReadStartContainer(ContextType.Message, lineInfo);
                return;
            }
            case PBKeywords.ENUM: {
                ReadStartContainer(ContextType.Enum, lineInfo);
                return;
            }
            case PBKeywords.OPTION: {
                context.ClearCommentLines();
                var pair = ParseOption(content);
                context.container.AddOption(pair.Key, pair.Value);
                return;
            }
            // Rpc
            case PBKeywords.RPC: {
                PBMethod method = ParseMethod(context.PopCommentLines(), lineInfo);
                context.container.AddEnclosedElement(method);
                return;
            }
            default: {
                // 可能是结束行
                context.ClearCommentLines();
                if (firstWord == "}") {
                    ReadEndContainer(lineInfo);
                }
                return;
            }
        }
    }

    private static PBMethod ParseMethod(List<string> commentLines, LineInfo lineInfo) {
        string content = lineInfo.content;
        EnsureEndWithSemicolon(content);
        // 我们的语法支持为参数命名，还支持无参和无返回结果
        // 'rpc Search(SearchRequest request) returns (SearchResponse);'
        string name;
        string? argType;
        string? argName;
        int argEnd;
        {
            int argStart = content.IndexOf('(');
            name = content.Substring2(4, argStart).Trim(); // 跳过'rpc '

            argEnd = content.IndexOf(')');
            string args = content.Substring2(argStart + 1, argEnd).Trim(); // 去掉两端空格
            if (string.IsNullOrWhiteSpace(args)) {
                argType = null;
                argName = null;
            } else {
                int splitIdx = args.IndexOf(' ');
                if (splitIdx < 0) {
                    argType = args;
                    argName = null;
                } else {
                    argType = args.Substring2(0, splitIdx).Trim();
                    argName = args.Substring(splitIdx + 1).Trim();
                }
            }
        }
        string? resultType;
        {
            int rStart = content.IndexOf('(', argEnd);
            int rEnd = content.IndexOf(')', rStart);
            string results = content.Substring2(rStart + 1, rEnd).Trim();
            if (string.IsNullOrWhiteSpace(results)) {
                resultType = null;
            } else {
                resultType = results;
            }
        }
        PBMethod method = new PBMethod()
        {
            ParameterType = argType,
            ParameterName = argName,
            ResultType = resultType,
            SimpleName = name,
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
        if (!context.started) {
            CheckStart(lineInfo);
            return;
        }
        if (!lineInfo.HasContent) {
            TryAddComment(lineInfo);
            return;
        }
        string content = lineInfo.content;
        string firstWord = FirstWord(content);
        switch (firstWord) {
            case PBKeywords.OPTION: {
                context.ClearCommentLines();
                var pair = ParseOption(content);
                context.container.AddOption(pair.Key, pair.Value);
                return;
            }
            case PBKeywords.RESERVED: {
                context.ClearCommentLines();
                ParseRevered(context.AsEnum(), content);
                return;
            }
            case "}": {
                // 结束行
                context.ClearCommentLines();
                ReadEndContainer(lineInfo);
                return;
            }
            default: {
                // 判断是否是枚举 name = number;
                if (content.IndexOf('=') > 0) {
                    PBEnumValue enumValue = ParseEnumValue(context.PopCommentLines(), lineInfo);
                    context.container.AddEnclosedElement(enumValue);
                } else {
                    context.ClearCommentLines();
                }
                return;
            }
        }
    }

    private PBEnumValue ParseEnumValue(List<string> commentLines, LineInfo lineInfo) {
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
            number = int.Parse(numberString.Trim());
        }
        PBEnumValue enumValue = new PBEnumValue()
        {
            SimpleName = name,
            Number = number,
            StartLine = lineInfo.ln,
            EndLine = lineInfo.ln
        };
        // 追加注释
        DrainCommentLine(enumValue, commentLines, lineInfo.comment);
        return enumValue;
    }

    #endregion

    /** 检查开始行 */
    private void CheckStart(LineInfo lineInfo) {
        // '{' 之前不能出现其它内容
        if (!lineInfo.HasContent) {
            return;
        }
        if (lineInfo.content != "{") {
            throw new PBParserException("invalid start line : " + lineInfo.content);
        }
        context.started = true;
    }

    /** 将注释行追加到context */
    private void TryAddComment(LineInfo lineInfo) {
        if (lineInfo.IsCommentLine) {
            context.AddCommentLine(lineInfo.comment);
        } else {
            // 空白行中断注释
            context.ClearCommentLines();
        }
    }

    #region container

    /// <summary>
    /// 
    /// </summary>
    /// <param name="contextType">新上下文类型</param>
    /// <param name="lineInfo">文件行</param>
    private void ReadStartContainer(ContextType contextType, LineInfo lineInfo) {
        if (recursionDepth > 32) throw new IllegalStateException("proto had too many levels of nesting");

        Context parent = this.context;
        Context context;
        switch (contextType) {
            case ContextType.Service: {
                context = new Context(parent, ContextType.Service, new PBService());
                break;
            }
            case ContextType.Message: {
                context = new Context(parent, ContextType.Message, new PBMessage());
                break;
            }
            case ContextType.Enum: {
                context = new Context(parent, ContextType.Enum, new PBEnum());
                break;
            }
            case ContextType.Oneof: {
                context = new Context(parent, ContextType.Oneof, new PBOneof());
                break;
            }
            default: throw new AssertionError();
        }
        context.container.StartLine = lineInfo.ln;
        context.container.SimpleName = ParseContainerName(lineInfo.content, out context.started);

        parent.container.AddEnclosedElement(context.container);
        DrainCommentLine(context.container, parent.PopCommentLines(), lineInfo.comment);
        recursionDepth++;
        this.context = context;
    }

    private void ReadEndContainer(LineInfo lineInfo) {
        if (context.parent == null || !context.started) {
            throw new IllegalStateException();
        }
        context.container.EndLine = lineInfo.ln;

        recursionDepth--;
        this.context = context.parent;
    }

    /** 解析容器的名字 */
    private static string ParseContainerName(string content, out bool hasBeginToken) {
        int startIdx = content.IndexOf(' '); // 跳过关键词
        int endIdx = content.LastIndexOf('{');
        string name;
        if (endIdx > 0) {
            name = content.Substring2(startIdx, endIdx).Trim();
        } else {
            name = content.Substring(startIdx).Trim();
        }
        hasBeginToken = endIdx >= 0;
        return name;
    }

    #endregion

    #region util

    /** 将注释添加到目标元素 */
    private static void DrainCommentLine(PBElement element, List<string> commentLines, string? trailingComment) {
        foreach (string commentLine in commentLines) {
            element.AddComment(commentLine);
            PBAnnotation? annotation = TryParseAnnotation(commentLine);
            if (annotation != null) {
                element.AddAnnotation(annotation);
            }
        }
        // 行尾注释不包含注解
        if (trailingComment != null) {
            element.AddComment(trailingComment);
        }
    }

    /** 解析注解 */
    private static PBAnnotation? TryParseAnnotation(string comment) {
        // 允许'//'和'@'符号之间有空格，但'@'符号后面的类名无空格，类名和'{}'可以有空格
        // '//@RpcService{}'
        int atIdx = Util.IndexOfNonWhitespace(comment, 2);
        if (atIdx < 0 || comment[atIdx] != '@') {
            return null; // '@'符号前面有其它内容
        }
        int valueStartIndex = comment.IndexOf('{');
        int valueEndIndex = comment.LastIndexOf('}');
        if (valueStartIndex < 0 || valueStartIndex >= valueEndIndex) {
            return null;
        }
        string type = comment.Substring2(atIdx + 1, valueStartIndex).Trim();
        if (string.IsNullOrWhiteSpace(type)) {
            return null; // 类型信息为空
        }
        string value = comment.Substring2(valueStartIndex, valueEndIndex + 1);
        PBAnnotation annotation = new PBAnnotation(type, value);
        if (annotation.DsonValue == null) { // 提前检查dson文本格式
            throw new PBParserException("invalid dson value");
        }
        return annotation;
    }

    /** 是否是注解类型注释 */
    public static bool IsAnnotationComment(string comment) {
        int atIdx = Util.IndexOfNonWhitespace(comment, 2);
        if (atIdx < 0 || comment[atIdx] != '@') {
            return false; // '@'符号前面有其它内容
        }
        int valueStartIndex = comment.IndexOf('{');
        int valueEndIndex = comment.LastIndexOf('}');
        if (valueStartIndex < 0 || valueStartIndex >= valueEndIndex) {
            return false;
        }
        string type = comment.Substring2(atIdx + 1, valueStartIndex).Trim();
        if (string.IsNullOrWhiteSpace(type)) {
            return false; // 类型信息为空
        }
        return true;
    }

    /** 解析Option -- 不适用字段 */
    private static KeyValuePair<string, string> ParseOption(string content) {
        int startIdx = content.IndexOf(' '); // 跳过 'option'
        int eqIdx = content.IndexOf('=');
        int lastIdx = content.LastIndexOf(';'); // 去掉末尾分号

        string name = content.Substring2(startIdx + 1, eqIdx).Trim();
        string value = content.Substring2(eqIdx + 1, lastIdx).Trim();
        value = Util.Unquote(value); // 去掉双引号
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
        return content;
    }

    /** 确保内容行以分号 ';' 结尾 */
    private static void EnsureEndWithSemicolon(string content) {
        if (content[content.Length - 1] != ';') {
            throw new PBParserException(content);
        }
    }

    #endregion

#nullable disable

    private class Context
    {
        public readonly Context parent;
        public readonly ContextType contextType;
        public readonly PBElement container;

        /** 是否已读取到开始符号 '{' */
        public bool started;
        /** 下一个元素的注释缓存 -- 内容执行了trim */
        public readonly List<string> commentLines = new();

        public Context(Context parent, ContextType contextType, PBElement container) {
            this.parent = parent;
            this.contextType = contextType;
            this.container = container;
        }

        public PBFile AsFile() {
            return (PBFile)container;
        }

        public PBMessage AsMessage() {
            return (PBMessage)container;
        }

        public PBEnum AsEnum() {
            return (PBEnum)container;
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
}
}