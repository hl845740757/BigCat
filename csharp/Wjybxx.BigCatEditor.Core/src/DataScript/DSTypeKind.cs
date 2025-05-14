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

namespace Wjybxx.BigCatEditor.DataScript
{
/// <summary>
/// 类型
/// </summary>
public enum DSTypeKind
{
    /// <summary>
    /// 不能解析的类型
    /// </summary>
    Error = 0,
    /// <summary>
    /// class - 引用类型
    /// </summary>
    Class = 1,
    /// <summary>
    /// 结构体 - 值类型
    /// </summary>
    Struct = 2,
    /// <summary>
    /// 枚举 - 值类型
    /// </summary>
    Enum = 3,
    /// <summary>
    /// 类型参数 
    /// </summary>
    TypeParameter = 4,

    //
    // Service = 5,
}
}