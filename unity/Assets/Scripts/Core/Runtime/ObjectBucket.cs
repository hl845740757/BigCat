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
public sealed class ObjectBucket : ScriptableObject, ISerializationCallbackReceiver
{
    /// <summary>
    /// 对象桶资产范畴
    /// (int类型的表意不够)
    /// </summary>
    public string category;
    /// <summary>
    /// 
    /// </summary>
    [Tooltip("是否可通过[name引用]代替[路径引用]，如果name具有唯一性，则可以勾选")]
    public bool preferName;
    /// <summary>
    /// 最终序列化的数据
    /// </summary>
    [SerializeField]
    private List<ObjectBytes> bytesList = new List<ObjectBytes>();

    /// <summary>
    /// 字符串id到字节数组的缓存(保持插入序)
    /// </summary>
    public readonly LinkedDictionary<string, ObjectBytes> id2BytesDic = new();
    /// <summary>
    /// 数字id到字节数组的缓存(保持插入序)
    /// </summary>
    /// <returns></returns>
    public readonly LinkedDictionary<long, ObjectBytes> numberId2BytesDic = new();
    /// <summary>
    /// name到Bytes的映射
    ///
    /// 注：命名的对象通常都是特殊对象，name通常唯一；当多个对象name相同时，后加载的数据覆盖前面的。
    /// </summary>
    public readonly Dictionary<string, ObjectBytes> name2BytesDic = new();

    public void OnBeforeSerialize() {
        // 用户自己转换才是最准确的
    }

    public void OnAfterDeserialize() {
#if !UNITY_EDITOR
        id2BytesDic.Clear();
        numberId2BytesDic.Clear();
        name2BytesDic.Clear();
        //
        foreach (ObjectBytes objectBytes in bytesList) {
            if (!string.IsNullOrWhiteSpace(objectBytes.objectId)) {
                if (id2BytesDic.Count == 0) {
                    id2BytesDic.EnsureCapacity(bytesList.Count);
                }
                id2BytesDic.Add(objectBytes.objectId, objectBytes);
            }
            if (long.TryParse(objectBytes.objectId, out long numberId)) {
                if (numberId2BytesDic.Count == 0) {
                    numberId2BytesDic.EnsureCapacity(bytesList.Count);
                }
                numberId2BytesDic.Add(numberId, objectBytes);
            }
            if (!string.IsNullOrWhiteSpace(objectBytes.name)) {
                if (name2BytesDic.Count == 0) {
                    name2BytesDic.EnsureCapacity(bytesList.Count);
                }
                name2BytesDic[objectBytes.name] = objectBytes; // 重复时覆盖
            }
        }
#endif
    }
}
}