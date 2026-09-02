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
/// Interpose：插入两个目标之间
///
/// 典型用途是保镖挡在刺客与雇主之间、或足球中的拦截跑位。
///
/// 做法是先估算"我到中点大约要多久"，用这个时间预测双方的未来位置，
/// 再取<b>未来中点</b>作为Arrive目标 —— 直接取当前中点会一直落后于两者的移动。
///
/// 注：这里的时间估算是一次迭代的近似（用当前中点估时间，再算未来中点），
/// 迭代两三次会更准，但对表现的提升微乎其微，不值得。
/// </summary>
public class InterposeBehaviour : SteeringBehaviour
{
    /// <summary>
    /// 目标A
    /// </summary>
    public SteeringAgent agentA;
    /// <summary>
    /// 目标B
    /// </summary>
    public SteeringAgent agentB;
    /// <summary>
    /// 减速档位
    /// </summary>
    public ESteeringDeceleration deceleration = ESteeringDeceleration.Fast;

    /// <summary>
    /// 上次的预测中点（调试用）
    /// </summary>
    private Vector3 _predictedMidPoint;

    public InterposeBehaviour() {
    }

    public InterposeBehaviour(SteeringAgent agentA, SteeringAgent agentB) {
        this.agentA = agentA;
        this.agentB = agentB;
    }

    /// <summary>
    /// 上次的预测中点
    /// </summary>
    public Vector3 PredictedMidPoint => _predictedMidPoint;

    public override Vector3 Calculate(float deltaTime) {
        if (agentA == null || agentB == null) return Vector3.zero;

        // 第一次估算：用当前中点估算抵达耗时
        Vector3 midPoint = (agentA.position + agentB.position) * 0.5f;
        float timeToReach = (midPoint - Position).magnitude / Mathf.Max(MaxSpeed, SteeringUtil.Epsilon);

        // 用该耗时预测双方位置，取未来中点
        Vector3 futureA = agentA.position + agentA.velocity * timeToReach;
        Vector3 futureB = agentB.position + agentB.velocity * timeToReach;
        _predictedMidPoint = (futureA + futureB) * 0.5f;

        return Agent.ArriveForce(_predictedMidPoint, deceleration);
    }

    public override void DrawGizmos() {
        if (agentA == null || agentB == null) return;

        Gizmos.color = SteeringGizmos.ColorProbe;
        Gizmos.DrawLine(agentA.position, agentB.position);
        Gizmos.color = SteeringGizmos.ColorTarget;
        SteeringGizmos.DrawCross(_predictedMidPoint, 0.4f);
        Gizmos.DrawLine(Position, _predictedMidPoint);
    }
}
}
