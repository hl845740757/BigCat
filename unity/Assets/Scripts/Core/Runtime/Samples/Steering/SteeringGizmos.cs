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

using UnityEngine;

namespace Wjybxx.BigCat.Samples.Steering
{
/// <summary>
/// Steering调试绘制的图元与配色
///
/// 注：
/// 1.只能在<c>OnDrawGizmos</c>/<c>OnDrawGizmosSelected</c>回调内调用。
/// 2.刻意不使用<c>UnityEditor.Handles</c>（Editor-only，会导致运行时编译失败），
///   圆、箭头这些Gizmos没有提供的图元都用线段自己拼。
/// </summary>
public static class SteeringGizmos
{
    #region 配色

    /// <summary>Agent本体</summary>
    public static readonly Color ColorAgent = new Color(0.60f, 0.60f, 0.65f, 0.60f);
    /// <summary>速度</summary>
    public static readonly Color ColorVelocity = new Color(0.20f, 0.90f, 0.90f, 1f);
    /// <summary>合成力</summary>
    public static readonly Color ColorForce = new Color(1.00f, 0.85f, 0.20f, 1f);
    /// <summary>目标</summary>
    public static readonly Color ColorTarget = new Color(0.30f, 1.00f, 0.40f, 1f);
    /// <summary>威胁/逃离源</summary>
    public static readonly Color ColorThreat = new Color(1.00f, 0.30f, 0.30f, 1f);
    /// <summary>障碍</summary>
    public static readonly Color ColorObstacle = new Color(0.95f, 0.55f, 0.15f, 0.85f);
    /// <summary>墙面</summary>
    public static readonly Color ColorWall = new Color(0.55f, 0.45f, 0.95f, 0.85f);
    /// <summary>路径</summary>
    public static readonly Color ColorPath = new Color(0.40f, 0.75f, 1.00f, 1f);
    /// <summary>探测范围</summary>
    public static readonly Color ColorProbe = new Color(1.00f, 1.00f, 1.00f, 0.35f);
    /// <summary>邻居连线</summary>
    public static readonly Color ColorNeighbor = new Color(0.85f, 0.85f, 0.40f, 0.45f);
    /// <summary>轨迹</summary>
    public static readonly Color ColorTrail = new Color(0.45f, 0.85f, 0.55f, 0.55f);
    /// <summary>网格</summary>
    public static readonly Color ColorGrid = new Color(0.35f, 0.35f, 0.40f, 0.25f);
    /// <summary>边界</summary>
    public static readonly Color ColorBounds = new Color(0.50f, 0.55f, 0.60f, 0.50f);

    #endregion

    /// <summary>
    /// 绘制圆环
    /// </summary>
    /// <param name="center">圆心</param>
    /// <param name="normal">圆所在平面的法线（无需归一化）</param>
    /// <param name="radius">半径</param>
    /// <param name="segments">分段数</param>
    public static void DrawCircle(Vector3 center, Vector3 normal, float radius, int segments = 32) {
        if (radius <= 0f || segments < 3) return;
        Vector3 unitNormal = SteeringUtil.SafeNormalize(normal, Vector3.up);
        Vector3 axisA = SteeringUtil.AnyPerpendicular(unitNormal);
        Vector3 axisB = Vector3.Cross(unitNormal, axisA);

        float step = Mathf.PI * 2f / segments;
        Vector3 prev = center + axisA * radius;
        for (int i = 1; i <= segments; i++) {
            float rad = step * i;
            Vector3 cur = center + (axisA * Mathf.Cos(rad) + axisB * Mathf.Sin(rad)) * radius;
            Gizmos.DrawLine(prev, cur);
            prev = cur;
        }
    }

    /// <summary>
    /// 绘制三个正交大圆构成的球（比<see cref="Gizmos.DrawWireSphere"/>更能体现3D姿态）
    /// </summary>
    public static void DrawSphere(Vector3 center, float radius, int segments = 24) {
        DrawCircle(center, Vector3.up, radius, segments);
        DrawCircle(center, Vector3.right, radius, segments);
        DrawCircle(center, Vector3.forward, radius, segments);
    }

    /// <summary>
    /// 绘制带箭头的线段
    /// </summary>
    /// <param name="from">起点</param>
    /// <param name="to">终点</param>
    /// <param name="headRatio">箭头长度占线段长度的比例</param>
    /// <param name="maxHeadSize">箭头长度上限（避免长向量的箭头过大）</param>
    public static void DrawArrow(Vector3 from, Vector3 to, float headRatio = 0.20f, float maxHeadSize = 0.5f) {
        Gizmos.DrawLine(from, to);
        Vector3 dir = to - from;
        float len = dir.magnitude;
        if (len < SteeringUtil.Epsilon) return;

        dir *= 1f / len;
        float headSize = Mathf.Min(len * headRatio, maxHeadSize);
        Vector3 axisA = SteeringUtil.AnyPerpendicular(dir);
        Vector3 axisB = Vector3.Cross(dir, axisA);
        Vector3 basePoint = to - dir * headSize;
        float halfWidth = headSize * 0.4f;
        // 画四根"羽毛"，从任意视角看都能辨认方向
        Gizmos.DrawLine(to, basePoint + axisA * halfWidth);
        Gizmos.DrawLine(to, basePoint - axisA * halfWidth);
        Gizmos.DrawLine(to, basePoint + axisB * halfWidth);
        Gizmos.DrawLine(to, basePoint - axisB * halfWidth);
    }

    /// <summary>
    /// 绘制向量（从起点出发的箭头）
    /// </summary>
    public static void DrawVector(Vector3 origin, Vector3 vector, float scale = 1f) {
        if (SteeringUtil.IsZero(vector)) return;
        DrawArrow(origin, origin + vector * scale);
    }

    /// <summary>
    /// 绘制三轴十字标记（标记一个点）
    /// </summary>
    public static void DrawCross(Vector3 center, float size = 0.25f) {
        Gizmos.DrawLine(center - Vector3.right * size, center + Vector3.right * size);
        Gizmos.DrawLine(center - Vector3.up * size, center + Vector3.up * size);
        Gizmos.DrawLine(center - Vector3.forward * size, center + Vector3.forward * size);
    }

    /// <summary>
    /// 绘制圆柱体线框（用于避障探测盒 —— 3D下探测体是胶囊/圆柱而非矩形）
    /// </summary>
    /// <param name="start">轴线起点</param>
    /// <param name="axis">轴线向量（长度即高度）</param>
    /// <param name="radius">半径</param>
    /// <param name="segments">端面分段数</param>
    public static void DrawCylinder(Vector3 start, Vector3 axis, float radius, int segments = 20) {
        float height = axis.magnitude;
        if (height < SteeringUtil.Epsilon || radius <= 0f) return;

        Vector3 unitAxis = axis * (1f / height);
        Vector3 end = start + axis;
        DrawCircle(start, unitAxis, radius, segments);
        DrawCircle(end, unitAxis, radius, segments);

        // 四根母线
        Vector3 axisA = SteeringUtil.AnyPerpendicular(unitAxis);
        Vector3 axisB = Vector3.Cross(unitAxis, axisA);
        Gizmos.DrawLine(start + axisA * radius, end + axisA * radius);
        Gizmos.DrawLine(start - axisA * radius, end - axisA * radius);
        Gizmos.DrawLine(start + axisB * radius, end + axisB * radius);
        Gizmos.DrawLine(start - axisB * radius, end - axisB * radius);
    }

    /// <summary>
    /// 绘制矩形面（四边 + 法线）
    /// </summary>
    public static void DrawQuad(Vector3 center, Vector3 axisWidth, Vector3 axisHeight, bool drawNormal) {
        Vector3 p0 = center - axisWidth - axisHeight;
        Vector3 p1 = center + axisWidth - axisHeight;
        Vector3 p2 = center + axisWidth + axisHeight;
        Vector3 p3 = center - axisWidth + axisHeight;
        Gizmos.DrawLine(p0, p1);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p0);
        if (drawNormal) {
            Vector3 normal = SteeringUtil.SafeNormalize(Vector3.Cross(axisHeight, axisWidth));
            DrawArrow(center, center + normal * Mathf.Min(axisWidth.magnitude, axisHeight.magnitude) * 0.5f);
        }
    }

    /// <summary>
    /// 绘制文字标签
    ///
    /// 注：Runtime下是空实现（<c>Handles</c>属于UnityEditor）。
    /// </summary>
    public static void DrawLabel(Vector3 position, string text) {
#if UNITY_EDITOR
        UnityEditor.Handles.Label(position, text);
#endif
    }
}
}
