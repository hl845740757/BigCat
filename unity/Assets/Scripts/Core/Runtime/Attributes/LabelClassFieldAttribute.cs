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
using UnityEngine;

namespace Wjybxx.BigCat.UnityCore
{
/// <summary>
/// 标签类字段
///
/// 1.字段只有在标签字段为指定值的情况下才会显示；支持多条配置。
/// 2.标签字段支持：int、string、enum。
///
/// <![CDATA[
/// public class BoxCfg {
///   public int type;
///   // type字段为1时显示，显示别名为radius
///   [LabelClassField("type", 1, "radius")]
///   [LabelClassField("type", 2, "weight")]    
///   public float p1;
///   public float p2;
///   public float p3;        
///  }
/// ]]>
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class LabelClassFieldAttribute : PropertyAttribute
{
    public readonly string label;
    public readonly object value;
    public readonly string alias;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="label">标签字段</param>
    /// <param name="value">标签字段值</param>
    /// <param name="alias">展示用别名</param>
    public LabelClassFieldAttribute(string label, object value, string alias) {
        this.label = label;
        this.value = value;
        this.alias = alias;
    }
}
}