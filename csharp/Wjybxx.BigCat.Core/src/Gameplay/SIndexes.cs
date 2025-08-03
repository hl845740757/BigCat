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
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Collections;
using static Wjybxx.Commons.Collections.IIndexedElement;

namespace Wjybxx.BigCat.Gameplay
{
/// <summary>
/// <see cref="Scene"/>在每个队列中的索引
/// </summary>
internal struct SIndexes
{
    private int v0; // 主队列
    private int v1, v2, v3, v4, v5; // 缓存队列

    public static SIndexes Create() {
        SIndexes r = new SIndexes();
        r.Clear();
        return r;
    }

    public int this[int queueId] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            return queueId switch
            {
                0 => v0,
                1 => v1,
                2 => v2,
                3 => v3,
                4 => v4,
                5 => v5,
                _ => throw new System.ArgumentOutOfRangeException()
            };
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            switch (queueId) {
                case 0: v0 = value; break;
                case 1: v1 = value; break;
                case 2: v2 = value; break;
                case 3: v3 = value; break;
                case 4: v4 = value; break;
                case 5: v5 = value; break;
                default: throw new System.ArgumentOutOfRangeException();
            }
        }
    }

    public void Clear() {
        v0 = IndexNotFound;
        v1 = v2 = v3 = v4 = v5 = IndexNotFound;
    }
}

/// <summary>
/// <see cref="Scene"/>索引辅助类
/// (场景通常不会有太多缓存列表)
/// </summary>
internal sealed class SIndexHelper : IIndexedElementHelper<Scene>
{
    private readonly int queueId;

    private SIndexHelper(int queueId) {
        this.queueId = queueId;
    }

    public int CollectionIndex(object collection, Scene element) {
        return element.indexes[queueId];
    }

    public void CollectionIndex(object collection, Scene element, int index) {
        element.indexes[queueId] = index;
    }

    private static readonly SIndexHelper[] CACHE = new SIndexHelper[6];

    static SIndexHelper() {
        for (int index = 0; index < CACHE.Length; index++) {
            CACHE[index] = new SIndexHelper(index);
        }
    }

    public static SIndexHelper GetInst(int queueId) {
        if (queueId < 0 || queueId >= CACHE.Length) {
            throw new ArgumentException("queueId: " + queueId);
        }
        return CACHE[queueId];
    }
}

/// <summary>
/// <see cref="SComponent"/>的索引辅助类
/// (场景组件可能存在于多个Update列表中)
/// </summary>
public sealed class SComponentIndexHelper : IIndexedElementHelper<SComponent>
{
    private readonly int queueId;

    private SComponentIndexHelper(int queueId) {
        this.queueId = queueId;
    }

    public int CollectionIndex(object collection, SComponent element) {
        return element.indexes[queueId];
    }

    public void CollectionIndex(object collection, SComponent element, int index) {
        element.indexes[queueId] = index;
    }

    private static readonly SComponentIndexHelper[] CACHE = new SComponentIndexHelper[6];

    static SComponentIndexHelper() {
        for (int index = 0; index < CACHE.Length; index++) {
            CACHE[index] = new SComponentIndexHelper(index);
        }
    }

    public static SComponentIndexHelper GetInst(int queueId) {
        if (queueId < 0 || queueId >= CACHE.Length) {
            throw new ArgumentException("queueId: " + queueId);
        }
        return CACHE[queueId];
    }
}
}