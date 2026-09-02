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
/// Pursuit：预测追捕
///
/// 不是冲向目标当前位置（那样只会一直吊在目标尾巴后面），而是冲向目标的<b>预测位置</b>：
/// <code>
/// lookAheadTime = 距离 / (自身maxSpeed + 目标speed)   // 相对接近速率
/// predictedPos  = 目标位置 + 目标速度 * lookAheadTime
/// </code>
///
/// 两个关键修正：
/// 1.正面相遇时（目标在前方且几乎正对着自己）预测毫无意义，直接冲当前位置；
/// 2.目标在背后时要额外计入掉头耗时，否则预测点会持续偏前。
/// </summary>
public class PursuitBehaviour : SteeringBehaviour
{
    /// <summary>
    /// 追捕目标（必须是Agent —— 预测需要速度）
    /// </summary>
    public SteeringAgent evader;
    /// <summary>
    /// 预测时间上限；防止远距离时预测出一个荒谬的落点
    /// </summary>
    public float maxPredictionTime = 3f;
    /// <summary>
    /// 掉头耗时系数（与Agent的maxTurnRate相关，越不灵活应越大）
    /// </summary>
    public float turnCoefficient = 0.5f;
    /// <summary>
    /// 正面相遇的判定阈值：双方朝向点积小于该值即视为正对
    /// </summary>
    public float headOnThreshold = -0.95f;

    /// <summary>
    /// 上次的预测落点（调试用）
    /// </summary>
    private Vector3 _predictedPosition;
    /// <summary>
    /// 上次是否走了正面相遇分支（调试用）
    /// </summary>
    private bool _headOn;

    public PursuitBehaviour() {
    }

    public PursuitBehaviour(SteeringAgent evader) {
        this.evader = evader;
    }

    /// <summary>
    /// 上次的预测落点
    /// </summary>
    public Vector3 PredictedPosition => _predictedPosition;

    public override Vector3 Calculate(float deltaTime) {
        if (evader == null) return Vector3.zero;

        Vector3 toEvader = evader.position - Position;
        // 正面相遇：目标在前方，且双方朝向几乎相反
        float relativeHeading = Vector3.Dot(Heading, evader.heading);
        _headOn = Vector3.Dot(toEvader, Heading) > 0f && relativeHeading < headOnThreshold;
        if (_headOn) {
            _predictedPosition = evader.position;
            return Agent.SeekForce(_predictedPosition);
        }

        // 相对接近速率 -> 预测时间；再加上自身掉头所需的时间
        float closingSpeed = MaxSpeed + evader.Speed;
        float lookAheadTime = toEvader.magnitude / Mathf.Max(closingSpeed, SteeringUtil.Epsilon);
        lookAheadTime += Agent.PredictTurnTime(evader.position, turnCoefficient);
        lookAheadTime = Mathf.Clamp(lookAheadTime, 0f, maxPredictionTime);

        _predictedPosition = evader.position + evader.velocity * lookAheadTime;
        return Agent.SeekForce(_predictedPosition);
    }

    public override void DrawGizmos() {
        if (evader == null) return;

        Gizmos.color = SteeringGizmos.ColorThreat;
        Gizmos.DrawLine(Position, evader.position);
        // 预测落点：正面相遇分支下与目标位置重合
        Gizmos.color = _headOn ? SteeringGizmos.ColorThreat : SteeringGizmos.ColorTarget;
        SteeringGizmos.DrawCross(_predictedPosition, 0.5f);
        SteeringGizmos.DrawCircle(_predictedPosition, Agent.worldUp, 0.5f, 12);
        Gizmos.DrawLine(evader.position, _predictedPosition);
    }
}
}
