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
using Wjybxx.BigCatEditor.Core;
using Wjybxx.Commons;

namespace Wjybxx.BigCatEditor.DataScript
{
/// <summary>
/// 数据脚本元素(DataScriptElement)
/// </summary>
public abstract class DSElement
{
#nullable disable
    /** 简单名 */
    private readonly string simpleName;
    /** 定义该元素的元素 -- 只有元素定义才可以访问 */
    private DSElement enclosingElement;
    /** 嵌套定义的元素 -- 只有元素定义才可以访问 */
    private readonly List<DSElement> enclosedElements = new();

    /** 注释 -- 包含注解的原始注释 */
    private readonly List<string> comments = new();
    /** 注解数据 -- 同一类型允许重复；可能有解析器动态附加的数据 */
    private readonly List<Annotation> annotations = new();
    /** 可选项 -- 数据脚本中通常不使用该机制，而是使用注解 */
    private readonly Dictionary<string, string> options = new();

    /** 定义元素的开始行号 -- -1表示非源码文件定义 */
    private int startLine = -1;
    /** 定义元素的结束行号 -- -1表示非源码文件定义 */
    private int endLine = -1;
#nullable enable

    protected DSElement(string simpleName) {
        this.simpleName = simpleName ?? throw new ArgumentNullException(nameof(simpleName));
    }

    #region logic

    /// <summary>
    /// 元素的类型
    /// </summary>
    public abstract DSElementKind Kind { get; }

    /// <summary>
    /// 获取元素的原始定义，用于获取类型的原始注解等数据：
    /// 主要用于处理泛型元素，泛型元素需要返回原始的元素定义；
    /// </summary>
    public virtual DSElement OriginDefine => this;

    /// <summary>
    /// 添加嵌套元素
    /// </summary>
    public void AddEnclosedElement(DSElement enclosed) {
        if (enclosed == null) throw new ArgumentNullException(nameof(enclosed));
        if (this == enclosed) throw new ArgumentException("this == enclosed");

        enclosed.enclosingElement = this;
        enclosedElements.Add(enclosed);
    }

    /// <summary>
    /// 添加注解，注解属于额外数据
    /// </summary>
    public void AddAnnotation(Annotation annotation) {
        annotations.Add(annotation);
    }

    /// <summary>
    /// 添加注释，注解的对应的注释也会被添加
    /// </summary>
    public void AddComment(string comment) {
        comments.Add(comment);
    }

    /// <summary>
    /// 添加选项
    /// </summary>
    public void AddOption(string name, string value) {
        options[name] = value;
    }

    /// <summary>
    /// 获取指定类型注解
    /// </summary>
    public Annotation? GetAnnotation(string type) {
        return annotations.FirstOrDefault(e => e.type == type);
    }

    /// <summary>
    /// 获取指定类型所有注解
    /// </summary>
    public List<Annotation> GetAnnotations(string type) {
        return annotations.Where(e => e.type == type).ToList();
    }

    /// <summary>
    /// 获取可选项的值，如果不存在则返回null
    /// </summary>
    public string? GetOption(string name) {
        options.TryGetValue(name, out string? r);
        return r;
    }

    #endregion

#nullable disable

    #region props

    public string SimpleName => simpleName;
    public DSElement EnclosingElement => enclosingElement;
    public List<DSElement> EnclosedElements => enclosedElements;
    public List<string> Comments => comments;
    public List<Annotation> Annotations => annotations;
    public Dictionary<string, string> Options => options;

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
            .Append(", startLn=").Append(startLine)
            .Append(", endLn=").Append(endLine)
            .Append('}').ToString();
    }

    protected virtual void ToString(StringBuilder sb) {
    }
}
}