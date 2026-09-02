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
/// Arrive：抵达目标并停下
///
/// 期望速率随距离线性衰减：<c>speed = distance / (deceleration * tweaker)</c>，
/// 并被<c>maxSpeed</c>截断，因此远处等价于Seek，近处自动减速。
///
/// 注：约束参数自洽性的是<b>mass</b>而不是maxForce。
/// 行为输出<c>desiredVelocity - velocity</c>，加速度为该值除以mass，
/// 所以速度跟踪的时间常数就是mass；制动时间<c>deceleration * tweaker</c>
/// 需达到约<c>4 * mass</c>才能完全消除过冲（详见<see cref="SteeringAgent.ArriveForce"/>的实测表）。
/// 若观察到"冲过目标再回头"，先检查这个比例，而不是去调maxForce
/// —— 该场景下的力通常远小于maxForce，截断根本没生效。
/// </summary>
public class ArriveBehaviour : SteeringBehaviour
{
    /// <summary>
    /// 目标
    /// </summary>
    public SteeringTarget target;
    /// <summary>
    /// 减速档位
    /// </summary>
    public ESteeringDeceleration deceleration = ESteeringDeceleration.Normal;
    /// <summary>
    /// 减速微调系数：越大减速越缓、制动距离越长、过冲越小
    ///
    /// 注：默认值取1，配合<see cref="ESteeringDeceleration.Normal"/>得到2秒制动时间；
    /// 若mass大于1需同比放大。
    /// </summary>
    public float tweaker = 1f;
    /// <summary>
    /// 到达判定半径（仅供外部查询<see cref="Arrived"/>，不影响力的计算）
    /// </summary>
    public float arriveRadius = 0.3f;
    /// <summary>
    /// 到达判定的速率阈值
    /// </summary>
    public float arriveSpeedThreshold = 0.2f;

    public ArriveBehaviour() {
    }

    public ArriveBehaviour(SteeringTarget target) {
        this.target = target;
    }

    /// <summary>
    /// 是否已抵达（位置足够近且已基本停下）
    /// </summary>
    public bool Arrived {
        get {
            if (Agent == null) return false;
            if ((target.Position - Position).sqrMagnitude > arriveRadius * arriveRadius) return false;
            return Agent.Speed <= arriveSpeedThreshold;
        }
    }

    /// <summary>
    /// 制动距离：从maxSpeed开始减速所需的距离（用于校核参数是否自洽）
    /// </summary>
    public float BrakingDistance => Agent == null
        ? 0f
        : Agent.maxSpeed * (int)deceleration * tweaker;

    public override Vector3 Calculate(float deltaTime) {
        return Agent.ArriveForce(target.Position, deceleration, tweaker);
    }

    public override void DrawGizmos() {
        Vector3 targetPosition = target.Position;
        Gizmos.color = SteeringGizmos.ColorTarget;
        SteeringGizmos.DrawCross(targetPosition, 0.4f);
        SteeringGizmos.DrawCircle(targetPosition, Agent.worldUp, arriveRadius, 16);
        Gizmos.DrawLine(Position, targetPosition);
        // 制动距离圈：Agent进入该圈时应开始明显减速，用于目视校核tweaker
        Gizmos.color = SteeringGizmos.ColorProbe;
        SteeringGizmos.DrawCircle(targetPosition, Agent.worldUp, BrakingDistance, 32);
    }
}
}
