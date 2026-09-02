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
/// PathFollow：路径跟随
///
/// 对中途航点用Seek（不减速，保持流畅过弯），对终点航点用Arrive（停下）。
///
/// 注：<see cref="waypointSeekDistance"/>不能太小 —— 它必须大于Agent一帧的位移
/// 且大于转弯半径，否则Agent会绕着航点打转却始终"没到达"，永远无法切换到下一个航点。
/// 经验值：不小于 <c>maxSpeed * maxDeltaTime</c> 的若干倍。
/// </summary>
public class PathFollowBehaviour : SteeringBehaviour
{
    /// <summary>
    /// 路径
    ///
    /// 注：多个Agent不能共享同一个实例（航点下标是路径的内部状态），用<see cref="SteeringPath.Clone"/>。
    /// </summary>
    public SteeringPath path;
    /// <summary>
    /// 航点到达判定距离
    /// </summary>
    public float waypointSeekDistance = 2.5f;
    /// <summary>
    /// 终点航点的减速档位
    /// </summary>
    public ESteeringDeceleration deceleration = ESteeringDeceleration.Normal;

    public PathFollowBehaviour() {
    }

    public PathFollowBehaviour(SteeringPath path) {
        this.path = path;
    }

    public override Vector3 Calculate(float deltaTime) {
        if (path == null || path.IsEmpty) return Vector3.zero;

        // 进入判定距离即切换到下一个航点（终点航点不切换，交给Arrive收尾）
        float sqrDistance = (path.CurWaypoint - Position).sqrMagnitude;
        if (sqrDistance < waypointSeekDistance * waypointSeekDistance && !path.IsFinished) {
            path.SetNext();
        }
        return path.IsFinished
            ? Agent.ArriveForce(path.CurWaypoint, deceleration)
            : Agent.SeekForce(path.CurWaypoint);
    }

    public override void DrawGizmos() {
        if (path == null || path.IsEmpty) return;

        path.DrawGizmos();
        Gizmos.color = SteeringGizmos.ColorPath;
        Gizmos.DrawLine(Position, path.CurWaypoint);
        // 到达判定球：能直观看出它是否小于转弯半径
        Gizmos.color = SteeringGizmos.ColorProbe;
        SteeringGizmos.DrawCircle(path.CurWaypoint, Agent.worldUp, waypointSeekDistance, 20);
    }
}
}
