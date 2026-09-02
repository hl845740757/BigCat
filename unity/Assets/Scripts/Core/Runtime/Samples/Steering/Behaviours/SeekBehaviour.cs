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
/// Seek：全速冲向目标
///
/// 所有Steering行为的原型：<c>desiredVelocity - velocity</c>。
///
/// 注：Seek不会在目标处停下 —— 抵达时速度仍是maxSpeed，于是过冲、掉头、再过冲，
/// 在目标附近形成来回振荡。需要停下时用<see cref="ArriveBehaviour"/>。
/// </summary>
public class SeekBehaviour : SteeringBehaviour
{
    /// <summary>
    /// 目标
    /// </summary>
    public SteeringTarget target;

    public SeekBehaviour() {
    }

    public SeekBehaviour(SteeringTarget target) {
        this.target = target;
    }

    public override Vector3 Calculate(float deltaTime) {
        return Agent.SeekForce(target.Position);
    }

    public override void DrawGizmos() {
        Vector3 targetPosition = target.Position;
        Gizmos.color = SteeringGizmos.ColorTarget;
        SteeringGizmos.DrawCross(targetPosition, 0.4f);
        Gizmos.DrawLine(Position, targetPosition);
    }
}
}
