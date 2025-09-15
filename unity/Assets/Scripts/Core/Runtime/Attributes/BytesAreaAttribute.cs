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

using UnityEngine;

namespace Wjybxx.BigCat.UnityCore
{
/// <summary>
/// 用于将字节数组展示为文本块，只读。
/// 
/// 1.<see cref="TextAreaAttribute"/>
/// 2.编辑器需要提供常见的视图支持，如：16进制，Dson文本...
/// </summary>
public sealed class BytesAreaAttribute : PropertyAttribute
{
    public readonly int maxLines;

    public BytesAreaAttribute(int maxLines) {
        this.maxLines = maxLines;
    }
}
}