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

namespace Wjybxx.BigCatTool.DataScript
{
/// <summary>
/// 数据脚本的元素类型
/// </summary>
public enum DSElementKind
{
    /// <summary>
    /// 文件
    /// </summary>
    File = 0,

    /// <summary>
    /// Class--引用类型（支持继承）
    ///
    /// <code>class _name_ {}</code>
    /// </summary>
    Class = 1,
    /// <summary>
    /// 结构体--值类型（禁止继承）
    ///
    /// <code>struct _name_ {}</code>
    /// </summary>
    Strut = 2,
    /// <summary>
    /// 枚举类--值类型（禁止继承）
    /// 注意：枚举不应该定义在泛型类中。
    ///
    /// <code>enum _name_ {}</code>
    /// </summary>
    Enum = 3,
    /// <summary>
    /// 服务
    /// </summary>
    Service = 4,
    /// <summary>
    /// 实例--class和struct的实例
    /// 注意：inst必须定义在顶层。
    ///
    /// <code>inst _name_ from t1, t2, t3</code>
    /// </summary>
    Inst = 5,

    /// <summary>
    /// 字段--class和struct的字段定义
    /// </summary>
    Field = 6,
    /// <summary>
    /// 枚举值
    /// </summary>
    EnumValue = 7,
    /// <summary>
    /// 方法
    /// </summary>
    Method = 8,

    /// <summary>
    /// 泛型参数
    /// </summary>
    TypeParameter = 9,
}
}