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

using System.Collections.Generic;
using UnityEngine;

namespace Wjybxx.BigCat.Samples.Steering
{
/// <summary>
/// 有限矩形墙面
///
/// 注：<see cref="normal"/>指向"可通行的一侧"。<see cref="WallAvoidanceBehaviour"/>
/// 用羽须（feeler）与墙面求交，把Agent沿法线推回，所以法线方向定义错了会把Agent推进墙里。
/// </summary>
public class SteeringWall
{
    /// <summary>
    /// 名字（调试用）
    /// </summary>
    public string name;
    /// <summary>
    /// 面中心
    /// </summary>
    public Vector3 center;
    /// <summary>
    /// 面法线（单位向量），指向可通行侧
    /// </summary>
    public Vector3 normal = Vector3.up;
    /// <summary>
    /// 面内的宽度方向（单位向量，与<see cref="normal"/>正交）
    /// </summary>
    public Vector3 tangent = Vector3.right;
    /// <summary>
    /// 沿<see cref="tangent"/>的半宽
    /// </summary>
    public float halfWidth = 5f;
    /// <summary>
    /// 沿<see cref="Binormal"/>的半高
    /// </summary>
    public float halfHeight = 5f;

    /// <summary>
    /// 面内的另一个方向（<see cref="normal"/> × <see cref="tangent"/>）
    /// </summary>
    public Vector3 Binormal => Vector3.Cross(normal, tangent);

    /// <summary>
    /// 构造墙面（自动根据法线生成正交的切线）
    ///
    /// 注：切线是任意选取的，因此<paramref name="halfWidth"/>和<paramref name="halfHeight"/>
    /// 对应的世界轴不确定；需要精确控制矩形朝向时请用带tangent参数的重载。
    /// </summary>
    /// <param name="center">面中心</param>
    /// <param name="normal">面法线，指向可通行侧（无需归一化）</param>
    /// <param name="halfWidth">半宽</param>
    /// <param name="halfHeight">半高</param>
    /// <param name="name">名字</param>
    public static SteeringWall Create(Vector3 center, Vector3 normal, float halfWidth, float halfHeight
        , string name = null) {
        Vector3 unitNormal = SteeringUtil.SafeNormalize(normal, Vector3.up);
        return Create(center, unitNormal, SteeringUtil.AnyPerpendicular(unitNormal), halfWidth, halfHeight, name);
    }

    /// <summary>
    /// 构造墙面（显式指定切线方向）
    /// </summary>
    /// <param name="center">面中心</param>
    /// <param name="normal">面法线，指向可通行侧（无需归一化）</param>
    /// <param name="tangent">宽度方向（无需归一化，会被正交化到法线平面上）</param>
    /// <param name="halfWidth">沿tangent的半宽</param>
    /// <param name="halfHeight">沿 normal × tangent 的半高</param>
    /// <param name="name">名字</param>
    public static SteeringWall Create(Vector3 center, Vector3 normal, Vector3 tangent
        , float halfWidth, float halfHeight, string name = null) {
        Vector3 unitNormal = SteeringUtil.SafeNormalize(normal, Vector3.up);
        // 正交化，容忍调用方传入的非严格正交向量
        Vector3 unitTangent = SteeringUtil.SafeNormalize(
            SteeringUtil.ProjectOnPlane(tangent, unitNormal), SteeringUtil.AnyPerpendicular(unitNormal));
        return new SteeringWall {
            name = name,
            center = center,
            normal = unitNormal,
            tangent = unitTangent,
            halfWidth = halfWidth,
            halfHeight = halfHeight,
        };
    }

    /// <summary>
    /// 构造一个盒形房间的六面墙（法线全部朝内）
    /// </summary>
    public static void CreateRoom(Bounds bounds, List<SteeringWall> output) {
        Vector3 c = bounds.center, e = bounds.extents;
        // 每面显式指定切线，保证halfWidth/halfHeight对应到预期的世界轴
        output.Add(Create(new Vector3(bounds.min.x, c.y, c.z), Vector3.right, Vector3.forward, e.z, e.y, "wall-x-min"));
        output.Add(Create(new Vector3(bounds.max.x, c.y, c.z), Vector3.left, Vector3.forward, e.z, e.y, "wall-x-max"));
        output.Add(Create(new Vector3(c.x, bounds.min.y, c.z), Vector3.up, Vector3.right, e.x, e.z, "wall-y-min"));
        output.Add(Create(new Vector3(c.x, bounds.max.y, c.z), Vector3.down, Vector3.right, e.x, e.z, "wall-y-max"));
        output.Add(Create(new Vector3(c.x, c.y, bounds.min.z), Vector3.forward, Vector3.right, e.x, e.y, "wall-z-min"));
        output.Add(Create(new Vector3(c.x, c.y, bounds.max.z), Vector3.back, Vector3.right, e.x, e.y, "wall-z-max"));
    }

    /// <summary>
    /// 线段与墙面求交（双向，不区分从哪一侧穿入）
    /// </summary>
    /// <param name="a">线段起点</param>
    /// <param name="b">线段终点</param>
    /// <param name="hitPoint">交点</param>
    /// <param name="t">交点参数（0在a处，1在b处）</param>
    public bool TryIntersectSegment(Vector3 a, Vector3 b, out Vector3 hitPoint, out float t) {
        hitPoint = default;
        if (!SteeringUtil.SegmentPlane(a, b, center, normal, out t)) return false;

        hitPoint = a + (b - a) * t;
        // 判断交点是否落在有限矩形内
        Vector3 offset = hitPoint - center;
        float u = Vector3.Dot(offset, tangent);
        if (Mathf.Abs(u) > halfWidth) return false;
        float v = Vector3.Dot(offset, Binormal);
        return Mathf.Abs(v) <= halfHeight;
    }

    /// <summary>
    /// 点到墙面所在平面的有符号距离（正值表示在可通行侧）
    /// </summary>
    public float SignedDistance(Vector3 point) {
        return Vector3.Dot(point - center, normal);
    }

    public void DrawGizmos() {
        Gizmos.color = SteeringGizmos.ColorWall;
        SteeringGizmos.DrawQuad(center, tangent * halfWidth, Binormal * halfHeight, true);
    }

    public override string ToString() => $"SteeringWall({name})";
}
}
