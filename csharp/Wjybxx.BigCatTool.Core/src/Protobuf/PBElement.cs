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
using System.Linq;
using System.Text;
using Wjybxx.BigCatTool.Core;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCatTool.Protobuf
{
/// <summary>
/// PB元素
/// </summary>
public abstract class PBElement
{
    private static readonly IDictionary<string, string> EMPTY_OPTIONS = ImmutableDictionary<string, string>.Empty;
#nullable disable
    /** 简单名 */
    private string simpleName;
    /** 定义该元素的元素 */
    private PBElement enclosingElement;
    /** 嵌套定义的元素 -- 任何便捷查询都是筛选后的快照 */
    private readonly List<PBElement> enclosedElements = new();

    /** 注释 -- 包含注解的原始注释 */
    private readonly List<string> comments = new();
    /** 注解数据 -- 同一类型允许重复；可能有解析器动态附加的数据 */
    private readonly List<Annotation> annotations = new();
    /** 可选项 -- 数据脚本中通常不使用该机制，而是使用注解；只建议文件使用options；延迟初始化以减少开销 */
    private IDictionary<string, string> options = EMPTY_OPTIONS;

    /** 定义元素的开始行号 -- -1表示非源码文件定义 */
    private int startLine = -1;
    /** 定义元素的结束行号 -- -1表示非源码文件定义 */
    private int endLine = -1;
#nullable restore

    #region logic

    /// <summary>
    /// 元素的类型
    /// </summary>
    public abstract PBElementKind Kind { get; }

    public PBElement AddEnclosedElement(PBElement enclosed) {
        if (enclosed == null) throw new ArgumentNullException(nameof(enclosed));
        if (this == enclosed) throw new ArgumentException("this == enclosed");

        enclosed.enclosingElement = this;
        enclosedElements.Add(enclosed);
        return this;
    }

    public PBElement AddComment(string comment) {
        comments.Add(comment);
        return this;
    }

    public PBElement AddAnnotation(Annotation annotation) {
        annotations.Add(annotation);
        return this;
    }

    public PBElement AddOption(string key, string value) {
        if (ReferenceEquals(options, EMPTY_OPTIONS)) {
            options = new Dictionary<string, string>(4);
        }
        options[key] = value;
        return this;
    }

    /** 获取指定类型注解 */
    public Annotation? GetAnnotation(string type) {
        return annotations.FirstOrDefault(e => e.type == type);
    }

    /** 获取指定类型所有注解 */
    public List<Annotation> GetAnnotations(string type) {
        return annotations.Where(e => e.type == type).ToList();
    }

    /** 获取可选项的值 */
    public string? GetOption(string name) {
        options.TryGetValue(name, out string? r);
        return r;
    }

    #endregion

#nullable disable

    #region props

    public string SimpleName {
        get => simpleName;
        set => simpleName = value;
    }

    public PBElement EnclosingElement => enclosingElement;
    public List<PBElement> EnclosedElements => enclosedElements;
    public List<string> Comments => comments;
    public List<Annotation> Annotations => annotations;
    public IDictionary<string, string> Options => options;

    public int StartLine {
        get => startLine;
        set => startLine = value;
    }
    public int EndLine {
        get => endLine;
        set => endLine = value;
    }

    #endregion

    public sealed override string ToString() {
        StringBuilder stringBuilder = new StringBuilder()
            .Append(EnumUtil.GetName(Kind)).Append("{")
            .Append("simpleName='").Append(simpleName).Append('\'')
            .Append(", enclosingElement=").Append(enclosingElement == null ? null : enclosingElement.simpleName);

        ToString(stringBuilder);
        return stringBuilder
            .Append(", startLn='").Append(startLine)
            .Append(", endLn='").Append(endLine)
            .Append('}').ToString();
    }

    protected virtual void ToString(StringBuilder sb) {
    }
}
}