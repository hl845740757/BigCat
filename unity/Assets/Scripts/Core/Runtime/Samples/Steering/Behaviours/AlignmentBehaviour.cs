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
/// Alignment：对齐（Boids三法则之一）
///
/// 让Agent的朝向趋近邻居的平均朝向，产生"整群同向游动"的效果。
///
/// 注：返回的是<b>朝向差</b>，量级上限只有2（两个单位向量之差），
/// 与Seek类行为（量级约2*maxSpeed）差了一个数量级，所以weight要相应放大。
/// </summary>
public class AlignmentBehaviour : SteeringBehaviour, ISteeringNeighborConsumer
{
    /// <summary>
    /// 是否用速度方向而非<c>heading</c>做平均
    ///
    /// 注：两者在稳态下几乎一致；受maxTurnRate限制而转向滞后时，
    /// 用速度方向收敛更快，用heading则视觉上更连贯。
    /// </summary>
    public bool useVelocityDirection;

    /// <summary>
    /// 上次的平均朝向（调试用）
    /// </summary>
    private Vector3 _averageHeading;
    /// <summary>
    /// 上次参与计算的邻居数（调试用）
    /// </summary>
    private int _effectiveCount;

    /// <summary>
    /// 上次的平均朝向
    /// </summary>
    public Vector3 AverageHeading => _averageHeading;

    public override Vector3 Calculate(float deltaTime) {
        List<SteeringAgent> neighbors = Agent.Neighbors;
        _effectiveCount = neighbors.Count;
        if (_effectiveCount == 0) {
            _averageHeading = Heading;
            return Vector3.zero;
        }

        Vector3 sum = Vector3.zero;
        for (int i = 0; i < neighbors.Count; i++) {
            SteeringAgent neighbor = neighbors[i];
            sum += useVelocityDirection
                ? SteeringUtil.SafeNormalize(neighbor.velocity, neighbor.heading)
                : neighbor.heading;
        }
        _averageHeading = sum / _effectiveCount;

        // 朝向差：不做归一化，邻居越同向则力越小，自然收敛
        return _averageHeading - Heading;
    }

    public override void DrawGizmos() {
        if (_effectiveCount == 0) return;
        Gizmos.color = SteeringGizmos.ColorNeighbor;
        SteeringGizmos.DrawArrow(Position, Position + _averageHeading * (Agent.radius * 4f));
    }
}
}
