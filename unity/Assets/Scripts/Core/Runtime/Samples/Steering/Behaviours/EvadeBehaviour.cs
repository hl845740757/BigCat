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
/// Evade：预测逃离
///
/// <see cref="PursuitBehaviour"/>的镜像 —— 逃离追捕者的<b>预测位置</b>而不是当前位置。
///
/// 注：这里不做Pursuit的"正面相遇"特判 —— 对逃跑方而言，正面冲来的追捕者
/// 恰恰是最该按预测位置规避的情形。
/// </summary>
public class EvadeBehaviour : SteeringBehaviour
{
    /// <summary>
    /// 追捕者（必须是Agent —— 预测需要速度）
    /// </summary>
    public SteeringAgent pursuer;
    /// <summary>
    /// 威胁距离；超出该距离不产生力。小于等于0表示不限距离
    /// </summary>
    public float threatDistance = 15f;
    /// <summary>
    /// 预测时间上限
    /// </summary>
    public float maxPredictionTime = 3f;

    /// <summary>
    /// 上次的预测落点（调试用）
    /// </summary>
    private Vector3 _predictedPosition;

    public EvadeBehaviour() {
    }

    public EvadeBehaviour(SteeringAgent pursuer) {
        this.pursuer = pursuer;
    }

    /// <summary>
    /// 上次的预测落点
    /// </summary>
    public Vector3 PredictedPosition => _predictedPosition;

    public override Vector3 Calculate(float deltaTime) {
        if (pursuer == null) return Vector3.zero;

        Vector3 toPursuer = pursuer.position - Position;
        float sqrDistance = toPursuer.sqrMagnitude;
        if (threatDistance > 0f && sqrDistance > threatDistance * threatDistance) {
            return Vector3.zero;
        }

        float closingSpeed = MaxSpeed + pursuer.Speed;
        float lookAheadTime = Mathf.Sqrt(sqrDistance) / Mathf.Max(closingSpeed, SteeringUtil.Epsilon);
        lookAheadTime = Mathf.Min(lookAheadTime, maxPredictionTime);

        _predictedPosition = pursuer.position + pursuer.velocity * lookAheadTime;
        // 这里不传panicDistance：距离筛选已在上面按threatDistance做过
        return Agent.FleeForce(_predictedPosition);
    }

    public override void DrawGizmos() {
        if (pursuer == null) return;

        Gizmos.color = SteeringGizmos.ColorProbe;
        if (threatDistance > 0f) {
            SteeringGizmos.DrawCircle(Position, Agent.worldUp, threatDistance, 32);
        }
        if (!LastContributed) return;

        Gizmos.color = SteeringGizmos.ColorThreat;
        Gizmos.DrawLine(Position, pursuer.position);
        SteeringGizmos.DrawCross(_predictedPosition, 0.5f);
        Gizmos.DrawLine(pursuer.position, _predictedPosition);
    }
}
}
