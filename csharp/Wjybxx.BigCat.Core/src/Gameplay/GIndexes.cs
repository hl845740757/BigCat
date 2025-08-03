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
/// <see cref="GameUnit"/>在每个队列中的索引
///
/// 1.不考虑无限的扩展性，使用值类型代替数组。
/// 2.应当使用<see cref="Create"/>方法创建实例，default构造的实例是非法的
/// 3.该对象开放给用户，在视野管理等逻辑中也需要缓存索引。
/// </summary>
public struct GIndexes
{
    internal int v0; // 主队列
    private int v1, v2, v3, v4, v5, v6, v7, v8, v9; // 缓存队列

    public static GIndexes Create() {
        GIndexes r = new GIndexes();
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
                6 => v6,
                7 => v7,
                8 => v8,
                9 => v9,
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
                case 6: v6 = value; break;
                case 7: v7 = value; break;
                case 8: v8 = value; break;
                case 9: v9 = value; break;
                default: throw new System.ArgumentOutOfRangeException();
            }
        }
    }

    public void Clear() {
        v0 = IndexNotFound;
        v1 = v2 = v3 = v4 = v5 = v6 = v7 = v8 = v9 = IndexNotFound;
    }
}

/// <summary>
/// <see cref="GameUnit"/>的索引辅助类
/// </summary>
public sealed class GIndexHelper : IIndexedElementHelper<GameUnit>
{
    private readonly int queueId;
    private readonly bool mainQueue;

    private GIndexHelper(int queueId, bool mainQueue = false) {
        this.queueId = queueId;
        this.mainQueue = mainQueue;
    }

    public int CollectionIndex(object collection, GameUnit element) {
        return element.indexes[queueId];
    }

    public void CollectionIndex(object collection, GameUnit element, int index) {
        if (queueId == 0 && mainQueue) {
            int prevIndex = element.indexes.v0;
            if (prevIndex == index) {
                return;
            }
            // 在主列表发生移动时需要通知SceneAgent
            element.indexes.v0 = index;
            element.Scene.Agent?.OnGameUnitIndexChanged(element, prevIndex);
        } else {
            element.indexes[queueId] = index;
        }
    }

    internal static readonly GIndexHelper MAIN_HELPER = new GIndexHelper(0, true);
    private static readonly GIndexHelper[] CACHE = new GIndexHelper[10];

    static GIndexHelper() {
        for (int index = 0; index < CACHE.Length; index++) {
            CACHE[index] = new GIndexHelper(index);
        }
    }

    public static GIndexHelper GetInst(int queueId) {
        if (queueId < 0 || queueId >= CACHE.Length) {
            throw new ArgumentException("queueId: " + queueId);
        }
        return CACHE[queueId];
    }
}

/// <summary>
/// <see cref="GComponent"/>的索引辅助类
/// 注：我们可能会在外部缓存组件列表。
/// </summary>
public class GComponentIndexHelper : IIndexedElementHelper<GComponent>
{
    private readonly int queueId;

    private GComponentIndexHelper(int queueId) {
        this.queueId = queueId;
    }

    public int CollectionIndex(object collection, GComponent element) {
        return element.indexes[queueId];
    }

    public void CollectionIndex(object collection, GComponent element, int index) {
        element.indexes[queueId] = index;
    }

    private static readonly GComponentIndexHelper[] CACHE = new GComponentIndexHelper[10];

    static GComponentIndexHelper() {
        for (int index = 0; index < CACHE.Length; index++) {
            CACHE[index] = new GComponentIndexHelper(index);
        }
    }

    public static GComponentIndexHelper GetInst(int queueId) {
        if (queueId < 0 || queueId >= CACHE.Length) {
            throw new ArgumentException("queueId: " + queueId);
        }
        return CACHE[queueId];
    }
}
}