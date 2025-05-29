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
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCatEditor.DataScript
{
/// <summary>
/// 数据脚本内置的注解类型
/// </summary>
public static class DSAnnotations
{
    /// <summary>
    /// 命名空间
    /// - 用于覆盖文件中指定的命名空间，用于配置特殊类型(第三方程序集）
    ///
    /// 语法如下：
    /// <code>// @Namespace{java : "xxx", cs: "xxx"} </code>
    /// - java 为java端的命名空间
    /// - cs 为csharp端的命名空间
    ///
    /// 1.由于命名空间可能包含点号，因此需要使用双引号
    /// 2.显式指定单个类型的命名空间时，需要使用全路径
    /// </summary>
    public const string NAMESPACE = "Namespace";
    
    /// <summary>
    /// 表示枚举类型是一个Flags类型
    /// (对应C#的<see cref="FlagsAttribute"/>注解)
    /// </summary>
    public const string ENUM_FLAGS = "Flags";

    /// <summary>
    /// 用于定义类型、字段、枚举值的可选项
    /// </summary>
    public const string OPTIONS = "Options";
    
    /// <summary>
    /// 类型的序列化配置
    /// -支持用于类型和字段
    ///
    /// 语法如下：
    /// <code>// @DsonCodec{style: flow, name: xxx, alias: [xxx, xxx, xxx], elemStyle: flow} </code>
    /// - name 表示类型或字段序列化的名字
    /// - alias 表示类型序列化时支持的别名，别名用于简化Dson文本编写；字段暂不支持别名
    /// - style 表示该类型输出为Dson文本时的默认排版，其值可见<see cref="ObjectStyle"/>和<see cref="NumberStyle"/>和<see cref="StringStyle"/>；不区分大小写，不认识的值将被忽略。
    /// - elemStyle 用于指定数组元素或字典的Value的排版。
    /// </summary>
    public const string CODEC = "Codec";
}
}