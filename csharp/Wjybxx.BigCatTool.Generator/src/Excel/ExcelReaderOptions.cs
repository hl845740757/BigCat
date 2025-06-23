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
using System.Text;
using System.Text.RegularExpressions;
using Wjybxx.Commons;
using Wjybxx.Commons.Attributes;

namespace Wjybxx.BigCatTool.Generator.Excel
{
/// <summary>
/// ExcelReader的可选项
/// </summary>
[Immutable]
public sealed class ExcelReaderOptions
{
    /// <summary>
    /// 默认的配置
    /// </summary>
    public static ExcelReaderOptions Default { get; } = new Builder().Build();

    /// <summary>
    /// 表格默认的注释行行数
    /// </summary>
    public readonly int skipRows;
    /// <summary>
    /// 内容注释行的首字符
    ///
    /// 当内容行的第一个Cell以给定的字符串开始时，表示注释行
    /// </summary>
    public readonly string commentLinePrefix;

    /// <summary>
    /// 文件的编码格式，默认为UTF8
    /// </summary>
    public readonly Encoding encoding;
    /// <summary>
    /// SheetName解析函数
    /// 参数为：文件名，原始表单名；
    /// 结果为：程序用表单名字；如果返回null，表示表格不需要读取。
    /// </summary>
    public readonly Func<string, string, string?> sheetNameParser;

    private ExcelReaderOptions(Builder builder) {
        this.skipRows = builder.SkipRows;
        this.commentLinePrefix = builder.CommentLinePrefix ?? "#";

        this.encoding = builder.Encoding ?? Encoding.UTF8;
        this.sheetNameParser = builder.SheetNameParser ?? ParseSheetName;
    }

    public struct Builder
    {
#nullable disable
        /// <summary>
        /// 表格默认的注释行行数
        /// </summary>
        public int SkipRows { get; set; }
        /// <summary>
        /// 内容注释行的首字符
        ///
        /// 当内容行的第一个Cell以给定的字符串开始时，表示注释行
        /// </summary>
        public string CommentLinePrefix { get; set; }

        /// <summary>
        /// 文本的代码页<see cref="Encoding"/>
        /// 即文件的编码格式，默认为UTF8
        /// </summary>
        public Encoding Encoding { get; set; }
        /// <summary>
        /// SheetName解析函数
        /// 参数为：文件名，原始表单名；
        /// 结果为：程序用表单名字；如果返回null，表示表格不需要读取。
        /// </summary>
        public Func<string, string, string?> SheetNameParser { get; set; }

        public ExcelReaderOptions Build() {
            return new ExcelReaderOptions(this);
        }
#nullable enable
    }

    #region Parse

    /// <summary>
    /// SheetName的正则表达式
    /// 分表使用下划线'_'或点号'.'分隔。
    /// </summary>
    private static readonly Regex regex = new Regex("^[a-zA-Z][a-zA-Z0-9_\\.]*$", RegexOptions.Compiled);

    /// <summary>
    /// 默认的SheetName解析函数
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="sheetName">表单名</param>
    /// <returns></returns>
    public static string? ParseSheetName(string fileName, string sheetName) {
        if (string.IsNullOrWhiteSpace(sheetName)) return null;
        if (sheetName[0] == '_') return null; // 下划线开头表示私有表格
        if (sheetName.StartsWith("Sheet") || sheetName.StartsWith("sheet")) return null; // 非正式表名
        if (sheetName == "ExamleLang") return null; // Excel自带的隐藏表
        if (!regex.IsMatch(sheetName)) return null; // 不符合命名规范
        return sheetName;
    }

    #endregion
}
}