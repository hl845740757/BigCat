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
using System.IO;
using Wjybxx.Commons;

namespace Wjybxx.BigCatEditor.Core
{
/// <summary>
/// 文本行信息
/// 每一行都可能既有内容又有注释，注释仅支持'//'注释
/// </summary>
public sealed class LineInfo
{
    /** 行号 */
    public readonly int ln;
    /** 原始内容 */
    public readonly string rawLine;

    /** 内容部分 -- 空字符串表示未声明内容；执行了trim */
    public readonly string content;
    /** 注释部分 -- //开头；执行了trim；为null表示未声明注释 */
    public readonly string? comment;

    public LineInfo(int ln, string rawLine, string content, string? comment) {
        this.ln = ln;
        this.rawLine = rawLine;
        this.content = content ?? throw new ArgumentNullException(nameof(content));
        this.comment = comment;
    }

    /// <summary>
    /// 是否是空白行
    /// </summary>
    public bool IsEmptyLine => string.IsNullOrWhiteSpace(rawLine);
    /// <summary>
    /// 是否是注释行
    /// </summary>
    public bool IsCommentLine => content.Length == 0 && comment != null;

    /// <summary>
    /// 是否有内容
    /// </summary>
    public bool HasContent => content.Length > 0;
    /// <summary>
    /// 是否有注释
    /// </summary>
    public bool HasComment => comment != null;

    public override string ToString() {
        return $"{nameof(ln)}: {ln}, {nameof(rawLine)}: {rawLine}";
    }

    #region internal

    // 空行
    public static readonly LineInfo EMPTY = new LineInfo(0, "", "", null);

    /// <summary>
    /// 解析行信息（默认方案）
    /// </summary>
    /// <param name="ln">行号--不约束</param>
    /// <param name="rawLine">原始行数据</param>
    /// <returns></returns>
    public static LineInfo Parse(int ln, string rawLine) {
        if (rawLine == null) throw new ArgumentNullException(nameof(rawLine));

        string content;
        string? comment;
        int slashIdx = rawLine.IndexOf('/');
        if (slashIdx < 0) {
            content = rawLine.Trim();
            comment = null;
        } else {
            if (rawLine[slashIdx + 1] != '/') {
                throw new IOException("incorrect comment format, ln: " + ln);
            }
            if (slashIdx == 0) {
                content = "";
                comment = rawLine;
            } else {
                content = rawLine.Substring2(0, slashIdx).Trim();
                comment = rawLine.Substring(slashIdx); // 保留斜杠
            }
        }
        return new LineInfo(ln, rawLine, content, comment);
    }

    #endregion
}
}