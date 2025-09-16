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

namespace Wjybxx.BitCat.Core.Core.Runtime
{
/// <summary>
/// 对象桶（用于存储任意的数据）
///
/// 注：
/// 1.项目按照自己需求，用扩展方法扩展。
/// 2.资产名很重要，最好可表达对象桶的用途 —— 我们不使用额外的字段标注，因为不如资产名直观。
/// </summary>
[CreateAssetMenu(menuName = "Object/ObjectBucket", fileName = "NewBucket")]
public sealed class ObjectBucket : ScriptableObject, ISerializationCallbackReceiver
{
    /// <summary>
    /// 对象桶资产范畴
    /// </summary>
    public int category;
    /// <summary>
    /// 需要伴随加载的资产
    /// </summary>
    public List<UnityEngine.Object> preloadAssets = new();

    /// <summary>
    /// 最终序列化的数据
    /// </summary>
    [SerializeField]
    private List<ObjectBytes> bytesList = new List<ObjectBytes>();
    /// <summary>
    /// guid到字节数组的缓存(保持插入序)
    /// </summary>
    public readonly LinkedDictionary<string, ObjectBytes> guid2BytesDic = new();
    /// <summary>
    /// localId到字节数组的缓存(保持插入序)
    /// </summary>
    /// <returns></returns>
    public readonly LinkedDictionary<long, ObjectBytes> localId2BytesDic = new();
    /// <summary>
    /// name到Bytes的映射
    ///
    /// 注：多个对象name相同时，后加载的数据覆盖前面的。
    /// </summary>
    public readonly Dictionary<string, ObjectBytes> name2BytesDic = new();

    public void OnBeforeSerialize() {
        // 用户可能在编辑期没有同步修改List，通过字典覆盖 - 其实用户自己转换才是最准确的
        // bytesList.Clear();
        // if (guid2BytesDic.Count > localId2BytesDic.Count) {
        //     bytesList.AddRange(guid2BytesDic.Values);
        // } else {
        //     bytesList.AddRange(localId2BytesDic.Values);
        // }
    }

    public void OnAfterDeserialize() {
        guid2BytesDic.Clear();
        localId2BytesDic.Clear();
        name2BytesDic.Clear();
        //
        foreach (ObjectBytes objectBytes in bytesList) {
            if (!string.IsNullOrWhiteSpace(objectBytes.guid)) {
                if (guid2BytesDic.Count == 0) {
                    guid2BytesDic.EnsureCapacity(bytesList.Count);
                }
                guid2BytesDic.Add(objectBytes.guid, objectBytes);
            }
            if (objectBytes.localId != 0) {
                if (localId2BytesDic.Count == 0) {
                    localId2BytesDic.EnsureCapacity(bytesList.Count);
                }
                localId2BytesDic.Add(objectBytes.localId, objectBytes);
            }
            if (!string.IsNullOrWhiteSpace(objectBytes.name)) {
                if (name2BytesDic.Count == 0) {
                    name2BytesDic.EnsureCapacity(bytesList.Count);
                }
                name2BytesDic[objectBytes.name] = objectBytes; // 重复时覆盖
            }
        }
    }
}
}