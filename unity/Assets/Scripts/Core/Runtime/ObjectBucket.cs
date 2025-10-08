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
using UnityEngine;
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCat.UnityCore
{
/// <summary>
/// 对象桶（用于存储任意的数据）
///
/// 注：
/// 1.项目按照自己需求，用扩展方法扩展。
/// 2.资产名很重要，最好可表达对象桶的用途 —— 我们不使用额外的字段标注，因为不如资产名直观。
/// </summary>
[CreateAssetMenu(menuName = "BigCat/ObjectBucket", fileName = "NewBucket")]
public sealed class ObjectBucket : ScriptableObject
{
    /// <summary>
    /// 对象桶资产范畴
    /// (int类型的表意不够)
    /// </summary>
    public string category;
    /// <summary>
    /// 是否优先通过name进行引用
    /// </summary>
    [Tooltip("是否可通过[name引用]代替[路径引用]，如果name具有唯一性，则可以勾选")]
    public bool preferName;
    /// <summary>
    /// 最终序列化的数据
    /// </summary>
    [SerializeField]
    private List<ObjectBytes> bytesList = new List<ObjectBytes>();

    /// <summary>
    /// localId到字节数组的缓存(保持插入序)
    /// </summary>
    public readonly LinkedDictionary<long, ObjectBytes> id2BytesDic = new();
    /// <summary>
    /// localPath到Bytes的映射
    ///
    /// 注：
    /// 1.key为<code>folder/name</code>，如果folder为空，则为name。
    /// 2.当多个对象name相同时，后加载的数据覆盖前面的。
    /// </summary>
    public readonly Dictionary<string, ObjectBytes> path2BytesDic = new();
}
}