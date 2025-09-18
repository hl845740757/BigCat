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
using Wjybxx.BigCat.UnityCore;

namespace Wjybxx.BitCat.Core.Core.Runtime
{
/// <summary>
/// 对象的二进制数据
/// </summary>
[Serializable]
public sealed class ObjectBytes
{
    /// <summary>
    /// 对象归类，非详细类型
    ///
    /// 1.真实对象类型在data中，该字段用于确定大致属于哪类资产。
    /// 2.尽量保持在0~255区间。
    /// </summary>
    public int category;
    /// <summary>
    /// 对象id
    ///
    /// 1.至少需要保证桶内唯一，即使是分块数据桶。
    /// 2.为保证较好的兼容性，我们统一使用string类型存储 -- 逻辑上可能是long。
    /// </summary>
    public string objectId;
    /// <summary>
    /// 用户分配的对象名
    /// (可选，尽量唯一)
    /// </summary>
    public string name;
    /// <summary>
    /// 编码类型
    ///
    /// 注：unity默认是将字节数组转换为16进制的字符串保存的。
    /// </summary>
    public byte coder;
    /// <summary>
    /// 数据
    ///
    /// 注：数据可能是编码过的，或是经过压缩的。
    /// </summary>
    [HideInInspector]
    public byte[] data;
#if UNITY_EDITOR
    /// <summary>
    /// 用于编辑器下预览Data字段，但数据量过大时可能导致Unity卡死；为避免Unity卡死，我们将显示行数限制为999行
    /// </summary>
    [TextArea(1, 999)]
    [ReadOnly]
    public string dataString;
#endif

    public ObjectBytes() {
    }

    public ObjectBytes(string objectId, byte[] data) {
        this.objectId = objectId;
        this.data = data;
    }
}
}