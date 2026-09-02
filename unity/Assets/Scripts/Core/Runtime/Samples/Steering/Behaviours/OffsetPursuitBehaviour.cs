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
/// OffsetPursuit：偏移追随（编队）
///
/// 追随的不是leader本身，而是leader<b>局部坐标系</b>下的一个固定偏移点，
/// 因此leader转向时整个编队会跟着旋转 —— 这是雁阵、战机编队、护卫队列的标准做法。
///
/// 注：
/// 1.必须用Arrive而不是Seek —— Seek会让僚机在阵位附近来回过冲；
/// 2.仍要做预测（leader加速时阵位会前移），否则僚机永远落后半个身位；
/// 3.僚机的maxSpeed应略高于leader，否则永远追不上阵位。
/// </summary>
public class OffsetPursuitBehaviour : SteeringBehaviour
{
    /// <summary>
    /// 长机
    /// </summary>
    public SteeringAgent leader;
    /// <summary>
    /// 阵位偏移（leader的局部空间：+X=右, +Y=上, +Z=前）
    /// </summary>
    public Vector3 offset;
    /// <summary>
    /// 预测时间上限
    /// </summary>
    public float maxPredictionTime = 2f;
    /// <summary>
    /// 减速档位
    /// </summary>
    public ESteeringDeceleration deceleration = ESteeringDeceleration.Fast;

    /// <summary>
    /// 上次的阵位世界坐标（调试用）
    /// </summary>
    private Vector3 _offsetWorldPosition;
    /// <summary>
    /// 上次的预测阵位（调试用）
    /// </summary>
    private Vector3 _predictedPosition;

    public OffsetPursuitBehaviour() {
    }

    public OffsetPursuitBehaviour(SteeringAgent leader, Vector3 offset) {
        this.leader = leader;
        this.offset = offset;
    }

    /// <summary>
    /// 当前阵位的世界坐标
    /// </summary>
    public Vector3 OffsetWorldPosition => _offsetWorldPosition;

    public override Vector3 Calculate(float deltaTime) {
        if (leader == null) return Vector3.zero;

        _offsetWorldPosition = leader.LocalToWorld(offset);
        Vector3 toOffset = _offsetWorldPosition - Position;

        float closingSpeed = MaxSpeed + leader.Speed;
        float lookAheadTime = toOffset.magnitude / Mathf.Max(closingSpeed, SteeringUtil.Epsilon);
        lookAheadTime = Mathf.Min(lookAheadTime, maxPredictionTime);

        _predictedPosition = _offsetWorldPosition + leader.velocity * lookAheadTime;
        return Agent.ArriveForce(_predictedPosition, deceleration);
    }

    public override void DrawGizmos() {
        if (leader == null) return;

        Gizmos.color = SteeringGizmos.ColorTarget;
        SteeringGizmos.DrawCross(_offsetWorldPosition, 0.35f);
        Gizmos.DrawLine(leader.position, _offsetWorldPosition);
        Gizmos.DrawLine(Position, _predictedPosition);
    }
}
}
