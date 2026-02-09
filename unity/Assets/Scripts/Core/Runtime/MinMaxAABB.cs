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
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// 轴对称包围盒
///
/// 注：
/// 1.严格要求max点的坐标大于min点的坐标，否则会出Bug；编辑器下尽量使用min+size模式编辑。
/// 2.直接命名为AABB会导致编辑器将其识别为<see cref="Bounds"/>，导致序列化错误。。。
/// </summary>
[Serializable]
[DsonSerializable(Names = new[] { "AABB", "MinMaxAABB" })]
public struct MinMaxAABB : IEquatable<MinMaxAABB>
{
    /// <summary>
    /// 空间最小坐标
    /// </summary>
    public Vector3 min;
    /// <summary>
    /// 空间最大坐标
    /// </summary>
    public Vector3 max;

    /// <summary>
    /// 更推荐通过<see cref="OfVertices(UnityEngine.Vector3,UnityEngine.Vector3)"/>创建
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    public MinMaxAABB(Vector3 min, Vector3 max) {
        this.min = min;
        this.max = max;
    }

    public MinMaxAABB(float minX, float minY, float minZ,
                      float maxX, float maxY, float maxZ) {
        min.x = minX;
        min.y = minY;
        min.z = minZ;
        max.x = maxX;
        max.y = maxY;
        max.z = maxZ;
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
        set => max = min + value;
    }

    public float Width {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => max.x - min.x;
        set => max.x = min.x + value;
    }
    public float Height {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => max.y - min.y;
        set => max.y = min.y + value;
    }
    public float Depth {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => max.z - min.z;
        set => max.z = min.z + value;
    }

    /// <summary>
    /// 内接球半径
    /// </summary>
    public float MinRadius => MathCommon.Min(Width, Height, Depth);

    /// <summary>
    /// 外接球半径
    /// </summary>
    public float MaxRadius => MathCommon.Max(Width, Height, Depth);

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
    /// 确保AABB包含目标点
    /// </summary>
    /// <param name="point"></param>
    public void Encapsulate(Vector3 point) {
        min.x = Mathf.Min(min.x, point.x);
        min.y = Mathf.Min(min.y, point.y);
        min.z = Mathf.Min(min.z, point.z);

        max.x = Mathf.Max(max.x, point.x);
        max.y = Mathf.Max(max.y, point.y);
        max.z = Mathf.Max(max.z, point.z);
    }

    /// <summary>
    /// 确保AABB包含目标包围盒
    /// </summary>
    /// <param name="aabb"></param>
    public void Encapsulate(MinMaxAABB aabb) {
        Encapsulate(aabb.min);
        Encapsulate(aabb.max);
    }

    /// <summary>
    /// AABB是否包含目标点
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public bool Contains(Vector3 point) {
        if (point.x < min.x || point.x > max.x) return false;
        if (point.y < min.y || point.y > max.y) return false;
        if (point.z < min.z || point.z > max.z) return false;
        return true;
    }

    #region util

    public static implicit operator Bounds(MinMaxAABB box) {
        return new Bounds(box.Center, box.Size);
    }

    public static implicit operator MinMaxAABB(Bounds bounds) {
        return new MinMaxAABB(bounds.min, bounds.max);
    }

    /// <summary>
    /// AABB是否有效
    /// </summary>
    public bool IsValid => min.x <= max.x && min.y <= max.y && min.z <= max.z;

    /// <summary>
    /// 修正Min和Max的数据
    /// </summary>
    public void Repair() {
        MathCommon.MinMax(min.x, max.x, out min.x, out max.x);
        MathCommon.MinMax(min.y, max.y, out min.y, out max.y);
        MathCommon.MinMax(min.z, max.z, out min.z, out max.z);
    }

    /// <summary>
    /// 截断浮点数
    /// </summary>
    public void Truncate() {
        min.x = (int)min.x;
        min.y = (int)min.y;
        min.z = (int)min.z;
        max.x = (int)max.x;
        max.y = (int)max.y;
        max.z = (int)max.z;
    }

    /// <summary>
    /// 检测包围盒是否产生碰撞
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Intersect(in MinMaxAABB a, in MinMaxAABB b) {
        if (a.max.x < b.min.x || a.min.x > b.max.x) return false;
        if (a.max.y < b.min.y || a.min.y > b.max.y) return false;
        if (a.max.z < b.min.z || a.min.z > b.max.z) return false;
        return true;
    }

    /// <summary>
    /// 计算AABB上距离给定点最近的点
    /// (用于计算和球体的碰撞)
    /// </summary>
    public Vector3 NearestPoint(Vector3 point) {
        float x = Mathf.Clamp(point.x, min.x, max.x);
        float y = Mathf.Clamp(point.y, min.y, max.y);
        float z = Mathf.Clamp(point.z, min.z, max.z);
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// 按照2D格式翻转左右，Z轴数据保持不变
    /// </summary>
    /// <param name="baseX">翻转轴的X坐标</param>
    /// <returns></returns>
    public MinMaxAABB FlipX2D(float baseX = 0f) {
        float maxX = baseX + (baseX - min.x); // delta
        float minX = maxX - (max.x - min.x); // width
        return new MinMaxAABB(minX, min.y, min.z, maxX, max.y, max.z);
    }

    /// <summary>
    /// 按照2D格式翻转上下，Z轴数据保持不变
    /// </summary>
    /// <param name="baseY">翻转轴的Y坐标</param>
    /// <returns></returns>
    public MinMaxAABB FlipY2D(float baseY = 0f) {
        float maxY = baseY + (baseY - min.y); // delta
        float minY = maxY - (max.y - min.y); // height
        return new MinMaxAABB(min.x, minY, min.z, max.x, maxY, max.z);
    }

    public static void FlipX2D(ref MinMaxAABB box, float baseX = 0f) {
        float maxX = baseX + (baseX - box.min.x); // delta
        float minX = maxX - (box.max.x - box.min.x); // width
        box.min.x = minX;
        box.max.x = maxX;
    }

    public static void FlipY2D(ref MinMaxAABB box, float baseY = 0f) {
        float maxY = baseY + (baseY - box.min.y); // delta
        float minY = maxY - (box.max.y - box.min.y); // height
        box.min.y = minY;
        box.max.y = maxY;
    }

    /// <summary>
    /// 以2D方式旋转
    /// (适用于简单场景，复杂需求请使用矩阵缓存中间过程)
    /// </summary>
    /// <param name="box">box</param>
    /// <param name="pivot">旋转轴心点</param>
    /// <param name="angleDeg">旋转角度</param>
    /// <returns></returns>
    public static MinMaxAABB Rotate2D(MinMaxAABB box, Vector2 pivot, float angleDeg) {
        if (angleDeg == 0) {
            return box;
        }
        float theta = -1 * Mathf.Deg2Rad * angleDeg;
        float cosT = Mathf.Cos(theta);
        float sinT = Mathf.Sin(theta);
        Vector3 p0 = Rotate(box.min, pivot, cosT, sinT);
        Vector3 p1 = Rotate(box.GetVertex(1), pivot, cosT, sinT);
        Vector3 p2 = Rotate(box.GetVertex(2), pivot, cosT, sinT);
        Vector3 p3 = Rotate(box.GetVertex(3), pivot, cosT, sinT);
        //
        MinMaxAABB aabb = OfVertices(p0, p1);
        aabb.Encapsulate(p2);
        aabb.Encapsulate(p3);
        aabb.max.z = box.max.z;
        return aabb;
    }

    private static Vector3 Rotate(Vector3 point, Vector2 pivot, float cosT, float sinT) {
        float relativeX = point.x - pivot.x;
        float relativeY = point.y - pivot.y;
        float newRelativeX = relativeX * cosT - relativeY * sinT;
        float newRelativeY = relativeX * sinT + relativeY * cosT;
        return new Vector3(newRelativeX + pivot.x, newRelativeY + pivot.y, point.z);
    }

    /// <summary>
    /// 通过几何中心和大小创建
    /// </summary>
    /// <param name="center"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static MinMaxAABB OfCenter(Vector3 center, Vector3 size) {
        Vector3 halfSize = (size / 2);
        Vector3 min = center - halfSize;
        Vector3 max = center + halfSize;
        return new MinMaxAABB(min, max);
    }

    /// <summary>
    /// 通过底部中心点创建
    /// </summary>
    /// <param name="bottom"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static MinMaxAABB OfBottom(Vector3 bottom, Vector3 size) {
        float minX = bottom.x - size.x / 2;
        float minZ = bottom.z - size.z / 2;
        float minY = bottom.y;
        //
        float maxX = minX + size.x;
        float maxY = minY + size.y;
        float maxZ = minZ + size.z;
        return new MinMaxAABB(minX, minY, minZ, maxX, maxY, maxZ);
    }

    /// <summary>
    /// 根据顶点创建包围盒
    /// </summary>
    /// <returns></returns>
    public static MinMaxAABB OfVertices(Vector3 v1, Vector3 v2) {
        MathCommon.MinMax(v1.x, v2.x, out float minX, out float maxX);
        MathCommon.MinMax(v1.y, v2.y, out float minY, out float maxY);
        MathCommon.MinMax(v1.z, v2.z, out float minZ, out float maxZ);
        return new MinMaxAABB(minX, minY, minZ, maxX, maxY, maxZ);
    }

    /// <summary>
    /// 根据顶点创建包围盒
    /// </summary>
    /// <param name="vertices"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static MinMaxAABB OfVertices(List<Vector3> vertices) {
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
        return new MinMaxAABB(minX, minY, minZ, maxX, maxY, maxZ);
    }

    public static MinMaxAABB OfVertices(Vector3[] vertices) {
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
        return new MinMaxAABB(minX, minY, minZ, maxX, maxY, maxZ);
    }

    #endregion

    #region equals

    public bool Equals(MinMaxAABB other) {
        return min.Equals(other.min) && max.Equals(other.max);
    }

    public override bool Equals(object obj) {
        return obj is MinMaxAABB other && Equals(other);
    }

    public override int GetHashCode() {
        return (min.GetHashCode() * 397) ^ max.GetHashCode();
    }

    // == 不是精确相等，允许极小偏差
    public static bool operator ==(MinMaxAABB left, MinMaxAABB right) {
        return left.min == right.min && left.max == right.max;
    }

    public static bool operator !=(MinMaxAABB left, MinMaxAABB right) {
        return !(left.min == right.min && left.max == right.max);
    }

    public override string ToString() {
        return $"{nameof(min)}: {min}, {nameof(max)}: {max}";
    }

    #endregion
}
}