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
/// BoundsContainment：活动区域约束
///
/// 把Agent软性地约束在一个AABB内。相比六面<see cref="SteeringWall"/>加
/// <see cref="WallAvoidanceBehaviour"/>，它更轻量、也不会有羽须打偏漏检的问题，
/// 因此更适合做演示场景的"兜底围栏"。
///
/// 两个设计要点：
/// 1.用<b>预测位置</b>而不是当前位置判断越界 —— 高速时才来得及转向；
/// 2.推力随侵入<see cref="margin"/>的深度平滑增长，避免在边界上突然满力（会引起抖动）。
/// </summary>
public class BoundsContainmentBehaviour : SteeringBehaviour
{
    /// <summary>
    /// 约束范围
    /// </summary>
    public Bounds bounds = new Bounds(Vector3.zero, new Vector3(60f, 30f, 60f));
    /// <summary>
    /// 缓冲边距：距边界小于该值时开始推回
    /// </summary>
    public float margin = 4f;
    /// <summary>
    /// 位置预测时长
    /// </summary>
    public float predictionTime = 0.6f;

    /// <summary>
    /// 上次的预测位置（调试用）
    /// </summary>
    private Vector3 _predictedPosition;

    public BoundsContainmentBehaviour() {
    }

    public BoundsContainmentBehaviour(Bounds bounds, float margin = 4f) {
        this.bounds = bounds;
        this.margin = margin;
    }

    public override Vector3 Calculate(float deltaTime) {
        _predictedPosition = Position + Velocity * predictionTime;

        float safeMargin = Mathf.Max(margin, SteeringUtil.Epsilon);
        Vector3 innerMin = bounds.min + new Vector3(safeMargin, safeMargin, safeMargin);
        Vector3 innerMax = bounds.max - new Vector3(safeMargin, safeMargin, safeMargin);

        // 每个轴上"需要往回修正多少"
        Vector3 correction = Vector3.zero;
        correction.x = AxisCorrection(_predictedPosition.x, innerMin.x, innerMax.x);
        correction.y = AxisCorrection(_predictedPosition.y, innerMin.y, innerMax.y);
        correction.z = AxisCorrection(_predictedPosition.z, innerMin.z, innerMax.z);
        if (SteeringUtil.IsZero(correction)) return Vector3.zero;

        // 侵入深度归一化到[0,1]，让力随深度平滑增长而不是在边界上跳变
        float ratio = Mathf.Clamp01(correction.magnitude / safeMargin);
        Vector3 desiredVelocity = SteeringUtil.SafeNormalize(correction) * (MaxSpeed * ratio);
        return desiredVelocity - Velocity * ratio;
    }

    /// <summary>
    /// 单轴修正量：在[min, max]内为0，越界则返回指向区间内的偏移
    /// </summary>
    private static float AxisCorrection(float value, float min, float max) {
        if (value < min) return min - value;
        if (value > max) return max - value;
        return 0f;
    }

    public override void DrawGizmos() {
        Gizmos.color = SteeringGizmos.ColorBounds;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        // 内缩后的安全区
        Gizmos.color = SteeringGizmos.ColorProbe;
        Vector3 innerSize = bounds.size - new Vector3(margin, margin, margin) * 2f;
        if (innerSize.x > 0f && innerSize.y > 0f && innerSize.z > 0f) {
            Gizmos.DrawWireCube(bounds.center, innerSize);
        }
        if (LastContributed) {
            Gizmos.color = SteeringGizmos.ColorThreat;
            SteeringGizmos.DrawCross(_predictedPosition, 0.4f);
            Gizmos.DrawLine(Position, _predictedPosition);
        }
    }
}
}
