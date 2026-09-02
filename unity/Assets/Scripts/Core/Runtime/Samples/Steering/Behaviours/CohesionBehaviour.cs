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
/// Cohesion：聚合（Boids三法则之一）
///
/// 朝邻居的质心Seek，把群体拉在一起。
///
/// 注：默认对结果做归一化（Reynolds/Buckland的做法）——
/// 不归一化时力的量级会随"离质心的距离"线性增长，与Separation的1/d衰减叠加后
/// 极易在群体边缘产生剧烈振荡（被拉回去、又被推出来）。
/// </summary>
public class CohesionBehaviour : SteeringBehaviour, ISteeringNeighborConsumer
{
    /// <summary>
    /// 是否归一化输出，使力度与离群距离无关
    /// </summary>
    public bool normalizeForce = true;

    /// <summary>
    /// 上次的邻居质心（调试用）
    /// </summary>
    private Vector3 _centerOfMass;
    /// <summary>
    /// 上次参与计算的邻居数（调试用）
    /// </summary>
    private int _effectiveCount;

    /// <summary>
    /// 上次的邻居质心
    /// </summary>
    public Vector3 CenterOfMass => _centerOfMass;

    public override Vector3 Calculate(float deltaTime) {
        List<SteeringAgent> neighbors = Agent.Neighbors;
        _effectiveCount = neighbors.Count;
        if (_effectiveCount == 0) {
            _centerOfMass = Position;
            return Vector3.zero;
        }

        Vector3 sum = Vector3.zero;
        for (int i = 0; i < neighbors.Count; i++) {
            sum += neighbors[i].position;
        }
        _centerOfMass = sum / _effectiveCount;

        // 质心与自身重合时不该产生任何力。
        // 注：不能直接交给SeekForce —— 它在目标重合时会沿heading兜底（Seek需要过冲特性），
        // 那会变成一个凭空的前向加速力。
        Vector3 toCenter = _centerOfMass - Position;
        if (SteeringUtil.IsZero(toCenter)) return Vector3.zero;

        Vector3 force = Agent.SeekForce(_centerOfMass);
        return normalizeForce ? SteeringUtil.SafeNormalize(force) : force;
    }

    public override void DrawGizmos() {
        if (_effectiveCount == 0) return;
        Gizmos.color = SteeringGizmos.ColorNeighbor;
        SteeringGizmos.DrawCross(_centerOfMass, 0.3f);
        Gizmos.DrawLine(Position, _centerOfMass);
    }
}
}
