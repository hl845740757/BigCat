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
using System.Runtime.CompilerServices;
using UnityEngine;
using Wjybxx.Commons;

namespace Wjybxx.BigCat.UnityCore
{
/// <summary>
/// 轴对称包围盒
///
/// 注：严格要求max点的坐标大于min点的坐标，否则会出Bug。
/// </summary>
[Serializable]
public struct AABB
{
    /// <summary>
    /// 空间最小坐标
    /// </summary>
    public Vector3 min;
    /// <summary>
    /// 空间最大坐标
    /// </summary>
    public Vector3 max;

    public AABB(Vector3 min, Vector3 max) {
        this.min = min;
        this.max = max;
    }

    public AABB(float minX, float minY, float minZ,
                float maxX, float maxY, float maxZ) {
        min = new Vector3(minX, minY, minZ);
        max = new Vector3(maxX, maxY, maxZ);
    }

    /// <summary>
    /// Cube几何中心
    /// </summary>
    public Vector3 Center {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            float x = (min.x + max.x) / 2;
            float y = (min.y + max.y) / 2;
            float z = (min.z + max.z) / 2;
            return new Vector3(x, y, z);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            Vector3 size = Size;
            min = value - size / 2;
            max = min + size;
        }
    }

    /// <summary>
    /// 底部中心点
    /// </summary>
    public Vector3 Bottom {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            float x = (min.x + max.x) / 2;
            float z = (min.z + max.z) / 2;
            return new Vector3(x, min.y, z);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            Vector3 size = Size;
            min = new Vector3(value.x - size.x / 2, value.y, value.z - size.z / 2);
            max = min + size;
        }
    }

    /// <summary>
    /// Cube大小
    ///
    /// 注意：修改min和max会影响size，因此以min+size模式使用AABB时要小心。
    /// </summary>
    public Vector3 Size {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (max - min);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            Vector3 center = Center;
            Vector3 halfSize = value / 2;
            min = center - halfSize;
            max = center + halfSize;
        }
    }

    public float Width {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => max.x - min.x;
    }
    public float Height {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => max.y - min.y;
    }
    public float Depth {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => max.z - min.z;
    }

    /// <summary>
    /// 获取顶点数据
    ///
    /// 注：从左下角开始，顺时针旋转；不使用索引器，避免歧义。
    /// </summary>
    /// <param name="index"></param>
    public Vector3 GetVertex(int index) {
        return index switch
        {
            0 => min,
            1 => new Vector3(min.x, max.y, min.z),
            2 => new Vector3(max.x, max.y, min.z),
            3 => new Vector3(max.x, min.y, min.z),
            //
            4 => new Vector3(min.x, min.y, max.z),
            5 => new Vector3(min.x, max.y, max.z),
            6 => max,
            7 => new Vector3(max.x, min.y, max.z),
            _ => throw new IndexOutOfRangeException("Invalid AABB index")
        };
    }

    /// <summary>
    /// 按照2D格式翻转左右，Z轴数据保持不变
    /// </summary>
    /// <param name="baseX">翻转轴的X坐标</param>
    /// <returns></returns>
    public AABB FlipX2D(float baseX) {
        float maxX = baseX + (baseX - min.x); // delta
        float minX = maxX - (max.x - min.x); // width
        return new AABB(minX, min.y, min.z, maxX, max.y, max.z);
    }

    /// <summary>
    /// 按照2D格式翻转上下，Z轴数据保持不变
    /// </summary>
    /// <param name="baseY">翻转轴的Y坐标</param>
    /// <returns></returns>
    public AABB FlipY2D(float baseY) {
        float maxY = baseY + (baseY - min.y); // delta
        float minY = maxY - (max.y - min.y); // height
        return new AABB(min.x, minY, min.z, max.x, maxY, max.z);
    }

    /// <summary>
    /// 校验AABB数据的正确性
    /// </summary>
    /// <exception cref="IllegalStateException"></exception>
    public void Validate() {
        if (min.x > max.x || min.y > max.y || min.z > max.z) {
            throw new IllegalStateException();
        }
    }

    /// <summary>
    /// 修正Min和Max的数据
    /// </summary>
    public void Repair() {
        MinMax(min.x, max.x, out min.x, out max.x);
        MinMax(min.y, max.y, out min.y, out max.y);
        MinMax(min.z, max.z, out min.z, out max.z);
    }

    private static void MinMax(float a, float b, out float min, out float max) {
        if ((double)a < (double)b) {
            min = a;
            max = b;
        } else {
            min = b;
            max = a;
        }
    }

    #region util

    /// <summary>
    /// 检测包围盒是否产生碰撞
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CheckCollision(in AABB a, in AABB b) {
        if (a.max.x < b.min.x || a.min.x > b.max.x) return false;
        if (a.max.y < b.min.y || a.min.y > b.max.y) return false;
        if (a.max.z < b.min.z || a.min.z > b.max.z) return false;
        return true;
    }

    /// <summary>
    /// 通过几何中心和大小创建
    /// </summary>
    /// <param name="center"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static AABB OfCenter(Vector3 center, Vector3 size) {
        Vector3 halfSize = (size / 2);
        Vector3 min = center - halfSize;
        Vector3 max = center + halfSize;
        return new AABB(min, max);
    }

    /// <summary>
    /// 通过底部中心点创建
    /// </summary>
    /// <param name="bottom"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static AABB OfBottom(Vector3 bottom, Vector3 size) {
        float minX = bottom.x - size.x / 2;
        float minZ = bottom.z - size.z / 2;
        float minY = bottom.y;
        //
        float maxX = minX + size.x;
        float maxY = minY + size.y;
        float maxZ = minZ + size.z;
        return new AABB(minX, minY, minZ, maxX, maxY, maxZ);
    }

    /// <summary>
    /// 根据顶点创建包围盒
    /// </summary>
    /// <returns></returns>
    public static AABB OfVertices(Vector3 v1, Vector3 v2) {
        float minX = Mathf.Min(v1.x, v2.x);
        float minY = Mathf.Min(v1.y, v2.y);
        float minZ = Mathf.Min(v1.z, v2.z);
        float maxX = Mathf.Max(v1.x, v2.x);
        float maxY = Mathf.Max(v1.y, v2.y);
        float maxZ = Mathf.Max(v1.z, v2.z);
        return new AABB(minX, minY, minZ, maxX, maxY, maxZ);
    }

    /// <summary>
    /// 根据顶点创建包围盒
    /// </summary>
    /// <param name="vertices"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static AABB OfVertices(List<Vector3> vertices) {
        Vector3 ver0 = vertices[0];
        float minX = ver0.x;
        float minY = ver0.y;
        float minZ = ver0.z;
        float maxX = ver0.x;
        float maxY = ver0.y;
        float maxZ = ver0.z;
        for (int i = 1; i < vertices.Count; i++) {
            Vector3 vertex = vertices[i];
            minX = Mathf.Min(minX, vertex.x);
            minY = Mathf.Min(minY, vertex.y);
            minZ = Mathf.Min(minZ, vertex.z);
            //
            maxX = Mathf.Max(maxX, vertex.x);
            maxY = Mathf.Max(maxY, vertex.y);
            maxZ = Mathf.Max(maxZ, vertex.z);
        }
        return new AABB(minX, minY, minZ, maxX, maxY, maxZ);
    }

    public static AABB OfVertices(Vector3[] vertices) {
        Vector3 ver0 = vertices[0];
        float minX = ver0.x;
        float minY = ver0.y;
        float minZ = ver0.z;
        float maxX = ver0.x;
        float maxY = ver0.y;
        float maxZ = ver0.z;
        for (int i = 1; i < vertices.Length; i++) {
            Vector3 vertex = vertices[i];
            minX = Mathf.Min(minX, vertex.x);
            minY = Mathf.Min(minY, vertex.y);
            minZ = Mathf.Min(minZ, vertex.z);
            //
            maxX = Mathf.Max(maxX, vertex.x);
            maxY = Mathf.Max(maxY, vertex.y);
            maxZ = Mathf.Max(maxZ, vertex.z);
        }
        return new AABB(minX, minY, minZ, maxX, maxY, maxZ);
    }

    #endregion
}
}