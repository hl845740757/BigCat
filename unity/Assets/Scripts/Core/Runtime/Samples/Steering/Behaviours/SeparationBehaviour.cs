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
/// Separation：分离（Boids三法则之一）
///
/// 对每个邻居施加一个背离它的斥力，力度随距离衰减。
///
/// 注：
/// 1.斥力量级与Seek类行为完全不同量纲（这里是1/d而不是速度差），
///   所以<c>weight</c>通常要设得很大（几十上百），这不是参数写错了。
/// 2.完全重合（距离为0）必须特殊处理 —— 否则斥力方向未定义，
///   两个Agent会永远叠在一起。这里用随机源给一个确定性的推离方向。
/// 3.<see cref="separationRadius"/>可小于Agent的感知半径：Alignment/Cohesion
///   往往需要更大的视野，而分离只关心近身。
/// </summary>
public class SeparationBehaviour : SteeringBehaviour, ISteeringNeighborConsumer
{
    /// <summary>
    /// 分离作用半径；小于等于0则使用Agent的<c>neighborRadius</c>
    /// </summary>
    public float separationRadius;
    /// <summary>
    /// 是否使用距离平方反比衰减（1/d²）；false则用线性反比（1/d，Reynolds原始做法）
    ///
    /// 注：平方反比在密集群体中防穿插效果更好，但远处邻居几乎无影响，
    /// 群体会显得更"松散且突然"。
    /// </summary>
    public bool useSquaredFalloff;

    /// <summary>
    /// 上次实际参与计算的邻居数（调试用）
    /// </summary>
    private int _effectiveCount;

    public override Vector3 Calculate(float deltaTime) {
        List<SteeringAgent> neighbors = Agent.Neighbors;
        _effectiveCount = 0;
        if (neighbors.Count == 0) return Vector3.zero;

        float radius = separationRadius > 0f ? separationRadius : Agent.neighborRadius;
        float sqrRadius = radius * radius;

        Vector3 force = Vector3.zero;
        for (int i = 0; i < neighbors.Count; i++) {
            Vector3 awayFromNeighbor = Position - neighbors[i].position;
            float sqrDistance = awayFromNeighbor.sqrMagnitude;
            if (sqrDistance > sqrRadius) continue;

            if (sqrDistance < SteeringUtil.SqrEpsilon) {
                // 完全重合：斥力方向未定义，给一个随机方向把它们掰开
                force += SteeringUtil.RandomOnUnitSphere(Rand);
                _effectiveCount++;
                continue;
            }
            float distance = Mathf.Sqrt(sqrDistance);
            // awayFromNeighbor / distance 是单位方向；再除一次(或两次)距离得到衰减
            force += useSquaredFalloff
                ? awayFromNeighbor / (sqrDistance * distance)
                : awayFromNeighbor / sqrDistance;
            _effectiveCount++;
        }
        return force;
    }

    public override void DrawGizmos() {
        float radius = separationRadius > 0f ? separationRadius : Agent.neighborRadius;
        Gizmos.color = SteeringGizmos.ColorProbe;
        SteeringGizmos.DrawCircle(Position, Agent.worldUp, radius, 24);

        if (_effectiveCount == 0) return;
        Gizmos.color = SteeringGizmos.ColorThreat;
        List<SteeringAgent> neighbors = Agent.Neighbors;
        float sqrRadius = radius * radius;
        for (int i = 0; i < neighbors.Count; i++) {
            if ((neighbors[i].position - Position).sqrMagnitude <= sqrRadius) {
                Gizmos.DrawLine(Position, neighbors[i].position);
            }
        }
    }
}
}
