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
/// Flee：全速远离目标
///
/// 注：<see cref="panicDistance"/>是必要的 —— 没有它，Agent会被无限远的威胁
/// 一路推到世界边缘。
/// </summary>
public class FleeBehaviour : SteeringBehaviour
{
    /// <summary>
    /// 威胁源
    /// </summary>
    public SteeringTarget target;
    /// <summary>
    /// 恐慌距离；超出该距离不产生力。小于等于0表示不限距离
    /// </summary>
    public float panicDistance = 12f;

    public FleeBehaviour() {
    }

    public FleeBehaviour(SteeringTarget target) {
        this.target = target;
    }

    public override Vector3 Calculate(float deltaTime) {
        return Agent.FleeForce(target.Position, panicDistance);
    }

    public override void DrawGizmos() {
        Vector3 targetPosition = target.Position;
        Gizmos.color = SteeringGizmos.ColorThreat;
        SteeringGizmos.DrawCross(targetPosition, 0.4f);
        if (panicDistance > 0f) {
            SteeringGizmos.DrawCircle(targetPosition, Agent.worldUp, panicDistance, 32);
        }
        if (LastContributed) {
            Gizmos.DrawLine(Position, targetPosition);
        }
    }
}
}
