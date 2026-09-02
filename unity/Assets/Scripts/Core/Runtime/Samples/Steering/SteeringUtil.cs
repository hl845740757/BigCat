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
using Random = System.Random;

namespace Wjybxx.BigCat.Samples.Steering
{
/// <summary>
/// Steering算法的数学工具
///
/// 注：这里刻意不依赖<see cref="UnityEngine.Random"/>，随机源由外部注入（<see cref="Random"/>），
/// 以便同一个种子能复现完全相同的运动轨迹 —— 调试Wander这类行为时非常关键。
/// </summary>
public static class SteeringUtil
{
    /// <summary>
    /// 长度阈值（用于判断向量是否可归一化）
    /// </summary>
    public const float Epsilon = 1e-5f;
    /// <summary>
    /// 长度平方阈值（<see cref="Epsilon"/>的平方，避免开方）
    /// </summary>
    public const float SqrEpsilon = 1e-10f;

    #region 向量

    /// <summary>
    /// 截断向量长度（保持方向不变）
    ///
    /// 注：这是Steering的基础操作 —— maxForce和maxSpeed都通过它施加约束。
    /// </summary>
    /// <param name="v">原向量</param>
    /// <param name="max">最大长度，小于0时视为0</param>
    public static Vector3 Truncate(Vector3 v, float max) {
        if (max <= 0f) return Vector3.zero;
        float sqrMag = v.sqrMagnitude;
        if (sqrMag <= max * max) return v;
        return v * (max / Mathf.Sqrt(sqrMag));
    }

    /// <summary>
    /// 安全归一化（零向量时返回给定的兜底方向，而不是NaN或零）
    /// </summary>
    public static Vector3 SafeNormalize(Vector3 v, Vector3 fallback) {
        float sqrMag = v.sqrMagnitude;
        if (sqrMag < SqrEpsilon) return fallback;
        return v * (1f / Mathf.Sqrt(sqrMag));
    }

    /// <summary>
    /// 安全归一化（零向量时返回零向量）
    /// </summary>
    public static Vector3 SafeNormalize(Vector3 v) => SafeNormalize(v, Vector3.zero);

    /// <summary>
    /// 是否是近似零向量
    /// </summary>
    public static bool IsZero(Vector3 v) => v.sqrMagnitude < SqrEpsilon;

    /// <summary>
    /// 求<paramref name="v"/>在垂直于<paramref name="unitAxis"/>的平面上的投影
    /// </summary>
    /// <param name="v">待投影的向量</param>
    /// <param name="unitAxis">平面法线，必须已归一化</param>
    public static Vector3 ProjectOnPlane(Vector3 v, Vector3 unitAxis) {
        return v - unitAxis * Vector3.Dot(v, unitAxis);
    }

    /// <summary>
    /// 构造一个与给定单位向量垂直的向量（用于处理退化情况，如heading与worldUp共线）
    /// </summary>
    public static Vector3 AnyPerpendicular(Vector3 unitV) {
        // 取绝对值最小的轴做叉乘，数值稳定性最好
        Vector3 axis;
        float ax = Mathf.Abs(unitV.x), ay = Mathf.Abs(unitV.y), az = Mathf.Abs(unitV.z);
        if (ax <= ay && ax <= az) axis = Vector3.right;
        else if (ay <= az) axis = Vector3.up;
        else axis = Vector3.forward;
        return SafeNormalize(Vector3.Cross(unitV, axis), Vector3.right);
    }

    #endregion

    #region 几何相交

    /// <summary>
    /// 射线与球体求交
    ///
    /// 注：返回最近的非负交点参数；若射线起点在球内，则返回0。
    /// </summary>
    /// <param name="origin">射线起点</param>
    /// <param name="unitDir">射线方向，必须已归一化</param>
    /// <param name="center">球心</param>
    /// <param name="radius">球半径</param>
    /// <param name="t">交点参数（交点 = origin + unitDir * t）</param>
    public static bool RaySphere(Vector3 origin, Vector3 unitDir, Vector3 center, float radius, out float t) {
        t = 0f;
        Vector3 m = origin - center;
        float c = Vector3.Dot(m, m) - radius * radius;
        float b = Vector3.Dot(m, unitDir);
        if (c > 0f && b > 0f) return false; // 起点在球外，且射线背向球心
        float disc = b * b - c;
        if (disc < 0f) return false; // 无实根，未命中
        t = -b - Mathf.Sqrt(disc);
        if (t < 0f) t = 0f; // 起点在球内
        return true;
    }

    /// <summary>
    /// 线段与无限平面求交
    /// </summary>
    /// <param name="a">线段起点</param>
    /// <param name="b">线段终点</param>
    /// <param name="planePoint">平面上一点</param>
    /// <param name="planeNormal">平面法线，必须已归一化</param>
    /// <param name="t">交点参数（交点 = a + (b - a) * t），仅在返回true时有效</param>
    public static bool SegmentPlane(Vector3 a, Vector3 b, Vector3 planePoint, Vector3 planeNormal, out float t) {
        t = 0f;
        Vector3 ab = b - a;
        float denom = Vector3.Dot(planeNormal, ab);
        if (Mathf.Abs(denom) < Epsilon) return false; // 线段与平面平行
        t = Vector3.Dot(planeNormal, planePoint - a) / denom;
        return t >= 0f && t <= 1f;
    }

    #endregion

    #region 随机

    /// <summary>
    /// [-1, 1]区间的均匀随机
    /// </summary>
    public static float RandomClamped(Random rand) => (float)(rand.NextDouble() * 2.0 - 1.0);

    /// <summary>
    /// [min, max)区间的均匀随机
    /// </summary>
    public static float RandomRange(Random rand, float min, float max) {
        return min + (float)rand.NextDouble() * (max - min);
    }

    /// <summary>
    /// 单位球面上的均匀随机点
    ///
    /// 注：采用Marsaglia方法（z轴均匀 + 方位角均匀），而不是"三个分量各自随机再归一化"
    /// —— 后者会在立方体对角线方向聚集，Wander时表现为转向偏好，是常见的实现错误。
    /// </summary>
    public static Vector3 RandomOnUnitSphere(Random rand) {
        double z = rand.NextDouble() * 2.0 - 1.0;
        double theta = rand.NextDouble() * Math.PI * 2.0;
        double r = Math.Sqrt(Math.Max(0.0, 1.0 - z * z));
        return new Vector3((float)(r * Math.Cos(theta)), (float)(r * Math.Sin(theta)), (float)z);
    }

    /// <summary>
    /// AABB内的均匀随机点
    /// </summary>
    public static Vector3 RandomInBounds(Random rand, Bounds bounds) {
        Vector3 min = bounds.min, max = bounds.max;
        return new Vector3(RandomRange(rand, min.x, max.x)
            , RandomRange(rand, min.y, max.y)
            , RandomRange(rand, min.z, max.z));
    }

    #endregion
}
}
