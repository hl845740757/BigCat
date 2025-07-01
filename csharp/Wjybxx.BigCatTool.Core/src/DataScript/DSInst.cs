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
using System.Text;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson;

namespace Wjybxx.BigCatTool.DataScript
{
/// <summary>
/// 类型实例
///
/// <h3>语法</h3>
/// <code>inst _name_ from t1, t2</code>
///
/// <h3>约束</h3>
/// 1.数组不支持通过from从其它实例初始化
/// 2.不能直接初始化泛型字段
/// 
/// <h3>命名</h3>
/// 当inst使用大驼峰命名法时，表示该实例为对应Type的默认实例；其它情况下都应使用小驼峰或snake风格。
/// </summary>
public class DSInst : DSElement
{
    /** 依赖的模板 -- from后的参数 */
    private readonly ImmutableList<string> templates;
#nullable disable
    /** dson文本 -- 不一定完整，部分数据可能在模板中 */
    private readonly string value;
    /** 解析后的dsonValue -- 由解析器初始化，包含从模板中拷贝的数据 */
    private DsonValue dsonValue;
#nullable enable

    public DSInst(string simpleName, string value, IEnumerable<string> templates)
        : base(simpleName) {
        this.value = value ?? throw new ArgumentNullException(nameof(value));
        this.templates = templates.ToImmutableList2();
    }

#nullable disable

    #region props

    public override DSElementKind Kind => DSElementKind.Inst;

    public ImmutableList<string> Templates => templates;
    public string Value => value;
    public DsonValue DsonValue {
        get => dsonValue;
        set => dsonValue = value;
    }

    #endregion

    protected override void ToString(StringBuilder sb) {
        sb.Append(", templates=");
        CollectionUtil.ToStringHelper(templates, sb);
    }
}
}