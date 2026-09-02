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
/// Hide：躲藏
///
/// 对每个障碍求出一个"藏身点"——障碍背对猎人一侧、距表面
/// <see cref="distanceFromBoundary"/>处的点——然后Arrive到最近的那个。
///
/// 注：
/// 1.这是纯几何近似，不做真正的视线遮挡判定（射线检测），所以猎人在障碍另一侧时
///   Agent会绕着障碍跑 —— 恰好是想要的表现。
/// 2.找不到藏身点时的兜底很重要：不加兜底，Agent会在空旷处呆立等死。
/// </summary>
public class HideBehaviour : SteeringBehaviour
{
    /// <summary>
    /// 猎人（要躲开的对象）
    /// </summary>
    public SteeringTarget hunter;
    /// <summary>
    /// 藏身点到障碍表面的距离
    /// </summary>
    public float distanceFromBoundary = 1.5f;
    /// <summary>
    /// 可接受的藏身点最大距离；超出则视为不可用。小于等于0表示不限
    /// </summary>
    public float maxHideDistance = 25f;
    /// <summary>
    /// 找不到藏身点时是否退化为Evade
    /// </summary>
    public bool evadeWhenNoSpot = true;
    /// <summary>
    /// 退化为Evade时的恐慌距离
    /// </summary>
    public float panicDistance = 20f;
    /// <summary>
    /// 减速档位
    /// </summary>
    public ESteeringDeceleration deceleration = ESteeringDeceleration.Fast;

    /// <summary>
    /// 上次选中的藏身点（调试用）
    /// </summary>
    private Vector3 _hidingSpot;
    /// <summary>
    /// 上次是否找到了藏身点（调试用）
    /// </summary>
    private bool _hasSpot;

    public HideBehaviour() {
    }

    public HideBehaviour(SteeringTarget hunter) {
        this.hunter = hunter;
    }

    /// <summary>
    /// 上次选中的藏身点
    /// </summary>
    public Vector3 HidingSpot => _hidingSpot;
    /// <summary>
    /// 上次是否找到藏身点
    /// </summary>
    public bool HasSpot => _hasSpot;

    public override Vector3 Calculate(float deltaTime) {
        Vector3 hunterPosition = hunter.Position;
        IReadOnlyList<SteeringObstacle> obstacles = Manager.Obstacles;

        _hasSpot = false;
        float closestSqrDistance = float.MaxValue;
        float sqrMaxHide = maxHideDistance > 0f ? maxHideDistance * maxHideDistance : float.MaxValue;

        for (int i = 0; i < obstacles.Count; i++) {
            Vector3 spot = GetHidingPosition(obstacles[i], hunterPosition, distanceFromBoundary + Agent.radius);
            float sqrDistance = (spot - Position).sqrMagnitude;
            if (sqrDistance > sqrMaxHide) continue;
            if (sqrDistance < closestSqrDistance) {
                closestSqrDistance = sqrDistance;
                _hidingSpot = spot;
                _hasSpot = true;
            }
        }

        if (_hasSpot) {
            return Agent.ArriveForce(_hidingSpot, deceleration);
        }
        // 兜底：无处可躲就直接逃
        return evadeWhenNoSpot ? Agent.FleeForce(hunterPosition, panicDistance) : Vector3.zero;
    }

    /// <summary>
    /// 求某个障碍相对猎人的藏身点
    /// </summary>
    /// <param name="obstacle">障碍</param>
    /// <param name="hunterPosition">猎人位置</param>
    /// <param name="clearance">藏身点到障碍表面的距离</param>
    private static Vector3 GetHidingPosition(SteeringObstacle obstacle, Vector3 hunterPosition, float clearance) {
        // 沿"猎人 -> 障碍"方向延伸到障碍背面
        Vector3 awayFromHunter = SteeringUtil.SafeNormalize(obstacle.position - hunterPosition, Vector3.forward);
        return obstacle.position + awayFromHunter * (obstacle.radius + clearance);
    }

    public override void DrawGizmos() {
        Vector3 hunterPosition = hunter.Position;
        Gizmos.color = SteeringGizmos.ColorThreat;
        SteeringGizmos.DrawCross(hunterPosition, 0.5f);

        // 画出所有候选藏身点，选中的高亮
        IReadOnlyList<SteeringObstacle> obstacles = Manager.Obstacles;
        Gizmos.color = SteeringGizmos.ColorProbe;
        for (int i = 0; i < obstacles.Count; i++) {
            Vector3 spot = GetHidingPosition(obstacles[i], hunterPosition, distanceFromBoundary + Agent.radius);
            SteeringGizmos.DrawCross(spot, 0.2f);
        }
        if (!_hasSpot) return;

        Gizmos.color = SteeringGizmos.ColorTarget;
        SteeringGizmos.DrawCircle(_hidingSpot, Agent.worldUp, 0.6f, 16);
        Gizmos.DrawLine(Position, _hidingSpot);
    }
}
}
