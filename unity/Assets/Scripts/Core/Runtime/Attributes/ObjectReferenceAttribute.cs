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
using Wjybxx.BitCat.Core.Core.Runtime;
using Wjybxx.Dson.Types;

namespace Wjybxx.BigCat.UnityCore
{
/// <summary>
/// 数据引用属性 -- <see cref="ObjectBucket"/>
///
/// 注意：只可用于<see cref="ObjectPtr"/>和List[ObjectPtr]类型。
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ObjectReferenceAttribute : PropertyAttribute
{
    public readonly bool preferName;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="preferName">是否name优先</param>
    public ObjectReferenceAttribute(bool preferName = true) {
        this.preferName = preferName;
    }
}
}