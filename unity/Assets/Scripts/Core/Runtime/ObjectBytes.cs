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
/// 对象的二进制数据
/// </summary>
[Serializable]
public sealed class ObjectBytes
{
    /// <summary>
    /// 对象归属的文件（分组）
    ///
    /// 注：如果folder不为空.外部应当通过<code>folder/name</code>的方式引用。
    /// </summary>
    public string folder;

    /// <summary>
    /// 对象归类，非详细类型
    ///
    /// 1.真实对象类型在data中，该字段用于确定大致属于哪类资产。
    /// 2.尽量保持在0~255区间。
    /// </summary>
    public int category;
    /// <summary>
    /// 对象本地id(桶内id)
    ///
    /// 注：至少需要保证桶内唯一，即使是分块数据桶。
    /// </summary>
    public long localId;
    /// <summary>
    /// 用户分配的对象名(可选)
    ///
    /// 注：name可以带有一层下划线，表示分组。
    /// </summary>
    public string name;
    /// <summary>
    /// 数据
    ///
    /// 注：数据可能是编码过的，或是经过压缩的。
    /// </summary>
    [HideInInspector]
    public byte[] data;
    /// <summary>
    /// 共享对象缓存
    ///
    /// 注：当Object可以共享时，直接存储在这里；也可以保存DsonValue加速反序列化。
    /// </summary>
    [NonSerialized]
    public object cachedObject;

    public ObjectBytes() {
    }
}
}