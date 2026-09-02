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
/// Wander：随机游走
///
/// 实现方式是"球面随机游走"：在Agent正前方<see cref="wanderDistance"/>处放一个半径为
/// <see cref="wanderRadius"/>的球，球面上有一个目标点；每帧对该点施加小幅抖动后重新投影回球面，
/// 然后朝它转向。
///
/// 注：关键在于<b>抖动的是球面上的点，而不是每帧重新随机</b> ——
/// 后者会让转向意图逐帧无关，表现为原地高频抽搐而非"漫游"。
/// 球心偏置在前方也是必需的：它保证目标点始终在前半空间，Agent不会突然倒车。
///
/// 参数关系：
/// <code>
/// wanderDistance / wanderRadius  越大 -> 转向越平缓（球的张角越小）
/// wanderJitter                   越大 -> 转向意图变化越快
/// </code>
/// </summary>
public class WanderBehaviour : SteeringBehaviour
{
    /// <summary>
    /// 游走球半径
    /// </summary>
    public float wanderRadius = 1.5f;
    /// <summary>
    /// 游走球球心到Agent的前方距离
    /// </summary>
    public float wanderDistance = 3f;
    /// <summary>
    /// 每秒抖动量（球面上的位移速率）
    /// </summary>
    public float wanderJitter = 60f;
    /// <summary>
    /// 竖直抖动缩放：1为完整3D游走，0则只在水平面内游走（地面单位常用）
    /// </summary>
    public float verticalScale = 1f;

    /// <summary>
    /// 局部空间中的游走目标（始终位于半径wanderRadius的球面上）
    /// </summary>
    private Vector3 _wanderTarget;
    /// <summary>
    /// 是否已初始化游走目标
    /// </summary>
    private bool _initialized;
    /// <summary>
    /// 上次的世界空间游走目标（调试用）
    /// </summary>
    private Vector3 _worldTarget;

    /// <summary>
    /// 上次的世界空间游走目标
    /// </summary>
    public Vector3 WorldTarget => _worldTarget;

    public override Vector3 Calculate(float deltaTime) {
        if (!_initialized) {
            _initialized = true;
            _wanderTarget = SteeringUtil.RandomOnUnitSphere(Rand) * wanderRadius;
        }

        // 在球面上做随机游走：抖动 -> 重新投影回球面
        Vector3 jitter = SteeringUtil.RandomOnUnitSphere(Rand) * (wanderJitter * deltaTime);
        jitter.y *= verticalScale;
        _wanderTarget = SteeringUtil.SafeNormalize(_wanderTarget + jitter, Vector3.forward) * wanderRadius;

        // 球心置于正前方（局部+Z），保证目标点始终在前半空间
        Vector3 localTarget = _wanderTarget + Vector3.forward * wanderDistance;
        _worldTarget = Agent.LocalToWorld(localTarget);

        // 注：Wander返回"指向目标的位移"而非Seek力 —— 它的作用是持续偏转，
        // 不需要desiredVelocity那样的速率诉求，用Seek反而会让Agent一直贴着maxSpeed跑。
        return _worldTarget - Position;
    }

    public override void DrawGizmos() {
        Vector3 sphereCenter = Agent.LocalToWorld(Vector3.forward * wanderDistance);
        Gizmos.color = SteeringGizmos.ColorProbe;
        SteeringGizmos.DrawCircle(sphereCenter, Agent.heading, wanderRadius, 20);
        SteeringGizmos.DrawCircle(sphereCenter, Agent.up, wanderRadius, 20);
        Gizmos.DrawLine(Position, sphereCenter);

        Gizmos.color = SteeringGizmos.ColorTarget;
        SteeringGizmos.DrawCross(_worldTarget, 0.25f);
        Gizmos.DrawLine(sphereCenter, _worldTarget);
    }
}
}
