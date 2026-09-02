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
/// ObstacleAvoidance：球形障碍规避
///
/// 做法是在Agent正前方假想一个<b>探测圆柱</b>（2D版本里是矩形探测盒），
/// 找出与它相交且最近的障碍，然后施加两个分量的力：
/// <code>
/// 侧向分量：把Agent推向"刚好能擦过障碍"的方向，越近越强
/// 前向分量：负向的制动力，让Agent在无法侧移时减速
/// </code>
/// 探测圆柱的长度随速度增长 —— 高速时必须更早开始规避。
///
/// 注：本实现与Buckland原书公式有一处刻意的差异。
/// 原书的侧向力写作<c>(R - localY) * multiplier</c>，由于通过筛选的障碍其localY恒在
/// (-R, R)区间内，该式几乎恒为正，导致Agent<b>总是偏向同一侧</b>躲避。
/// 这里改为沿"远离圆柱轴线"的方向推，量级取"还需横移多少才能擦过"，
/// 对称且在3D下能自然地选择任意侧移方向。
///
/// 该行为应放在行为列表的最前面（优先级最高）。
/// </summary>
public class ObstacleAvoidanceBehaviour : SteeringBehaviour
{
    /// <summary>
    /// 基础探测长度（静止时的探测距离）
    /// </summary>
    public float detectionLength = 4f;
    /// <summary>
    /// 侧向推力系数
    /// </summary>
    public float lateralWeight = 2f;
    /// <summary>
    /// 制动力系数
    /// </summary>
    public float brakingWeight = 0.3f;
    /// <summary>
    /// 正对障碍中心时的兜底侧移方向（局部空间）
    ///
    /// 注：完全正对时侧向分量为零，必须任选一侧。默认取局部+Y（拉升），
    /// 3D下比左右平移更符合直觉；地面单位应改为<see cref="Vector3.right"/>。
    /// </summary>
    public Vector3 fallbackEscapeDirection = Vector3.up;

    /// <summary>
    /// 上次探测到的最近障碍（调试用）
    /// </summary>
    private SteeringObstacle _closestObstacle;
    /// <summary>
    /// 最近障碍在局部空间的位置（调试用）
    /// </summary>
    private Vector3 _closestLocalPosition;
    /// <summary>
    /// 上次的探测圆柱长度（调试用）
    /// </summary>
    private float _detectionBoxLength;

    /// <summary>
    /// 上次探测到的最近障碍；null表示未发现威胁
    /// </summary>
    public SteeringObstacle ClosestObstacle => _closestObstacle;

    public override Vector3 Calculate(float deltaTime) {
        // 探测长度随速度线性增长
        float boxLength = detectionLength * (1f + Agent.SpeedRatio);
        _detectionBoxLength = boxLength;
        _closestObstacle = null;

        IReadOnlyList<SteeringObstacle> obstacles = Manager.Obstacles;
        float closestEntry = float.MaxValue;
        float closestExpandedRadius = 0f;

        for (int i = 0; i < obstacles.Count; i++) {
            SteeringObstacle obstacle = obstacles[i];
            // 用"障碍半径 + 自身半径"做膨胀，把Agent当质点处理
            float expandedRadius = obstacle.radius + Agent.radius;

            // 粗筛：超出探测范围直接跳过（避免每个障碍都做坐标变换）
            Vector3 toObstacle = obstacle.position - Position;
            float reach = boxLength + expandedRadius;
            if (toObstacle.sqrMagnitude > reach * reach) continue;

            // 变换到局部空间：+X=side, +Y=up, +Z=heading
            Vector3 local = Agent.WorldToLocalDirection(toObstacle);
            if (local.z + expandedRadius < 0f) continue; // 完全在身后

            // 到探测圆柱轴线（局部Z轴）的侧向距离
            float sqrLateral = local.x * local.x + local.y * local.y;
            if (sqrLateral >= expandedRadius * expandedRadius) continue; // 圆柱擦不到它

            // 轴线与膨胀球的近侧交点，即"多远处会撞上"
            float halfChord = Mathf.Sqrt(expandedRadius * expandedRadius - sqrLateral);
            float entry = local.z - halfChord;
            if (entry < 0f) {
                // 近交点在身后：Agent已经进入球的横截范围，用远交点
                entry = local.z + halfChord;
                if (entry < 0f) continue; // 整个球都在身后
            }
            if (entry > boxLength) continue; // 还没进入探测长度

            if (entry < closestEntry) {
                closestEntry = entry;
                _closestObstacle = obstacle;
                _closestLocalPosition = local;
                closestExpandedRadius = expandedRadius;
            }
        }

        if (_closestObstacle == null) return Vector3.zero;

        // 越近力越大：z接近0时multiplier趋近2，z接近boxLength时趋近1
        float multiplier = 1f + (boxLength - _closestLocalPosition.z) / boxLength;

        // 侧向：沿"远离轴线"的方向，量级 = 还需横移多少才能擦过障碍
        Vector2 lateral = new Vector2(_closestLocalPosition.x, _closestLocalPosition.y);
        float lateralLength = lateral.magnitude;
        Vector3 escapeDirection;
        if (lateralLength < SteeringUtil.Epsilon) {
            escapeDirection = SteeringUtil.SafeNormalize(fallbackEscapeDirection, Vector3.up);
        } else {
            escapeDirection = new Vector3(-lateral.x / lateralLength, -lateral.y / lateralLength, 0f);
        }

        Vector3 localForce = escapeDirection
                             * ((closestExpandedRadius - lateralLength) * multiplier * lateralWeight);
        // 前向：制动。障碍越近（z越小）制动越强，z超过膨胀半径后转为轻微加速
        localForce.z = (closestExpandedRadius - _closestLocalPosition.z) * brakingWeight;

        return Agent.LocalToWorldDirection(localForce);
    }

    public override void DrawGizmos() {
        // 探测圆柱
        Gizmos.color = _closestObstacle != null ? SteeringGizmos.ColorThreat : SteeringGizmos.ColorProbe;
        SteeringGizmos.DrawCylinder(Position, Agent.heading * _detectionBoxLength, Agent.radius, 16);

        if (_closestObstacle == null) return;
        // 高亮最近障碍与它在轴线上的投影
        Gizmos.color = SteeringGizmos.ColorThreat;
        SteeringGizmos.DrawSphere(_closestObstacle.position, _closestObstacle.radius + Agent.radius, 16);
        Vector3 axisPoint = Agent.LocalToWorld(new Vector3(0f, 0f, _closestLocalPosition.z));
        Gizmos.DrawLine(_closestObstacle.position, axisPoint);
    }
}
}
