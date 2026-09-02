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
/// Steering行为的目标（静态点 / Transform / 另一个Agent 的统一抽象）
///
/// 注：
/// 1.提供了隐式转换，因此可以直接写<code>seek.target = Vector3.zero;</code>或<code>seek.target = otherAgent;</code>。
/// 2.只有绑定到<see cref="SteeringAgent"/>时<see cref="Velocity"/>才有意义 ——
///   Pursuit/Evade/Interpose这类需要预测的行为必须用Agent作为目标。
/// </summary>
public struct SteeringTarget
{
    /// <summary>
    /// 静态目标点（仅在<see cref="agent"/>和<see cref="transform"/>都为null时生效）
    /// </summary>
    public Vector3 point;
    /// <summary>
    /// 目标Transform（优先级高于<see cref="point"/>）
    /// </summary>
    public Transform transform;
    /// <summary>
    /// 目标Agent（优先级最高，且可提供速度用于预测）
    /// </summary>
    public SteeringAgent agent;

    public SteeringTarget(Vector3 point) {
        this.point = point;
        this.transform = null;
        this.agent = null;
    }

    public SteeringTarget(Transform transform) {
        this.point = default;
        this.transform = transform;
        this.agent = null;
    }

    public SteeringTarget(SteeringAgent agent) {
        this.point = default;
        this.transform = null;
        this.agent = agent;
    }

    /// <summary>
    /// 目标是否有效
    ///
    /// 注：静态点总是有效；绑定的Transform被销毁后变为无效
    /// —— 已销毁的UnityEngine.Object满足<c>obj == null</c>但不满足<c>ReferenceEquals(obj, null)</c>，
    /// 靠这个差异区分"从未绑定Transform"和"Transform已销毁"。
    /// </summary>
    public bool IsValid {
        get {
            if (agent != null) return true;
            if (transform != null) return true;
            return ReferenceEquals(transform, null); // 从未绑定 -> 走静态点，有效
        }
    }

    /// <summary>
    /// 目标当前位置
    /// </summary>
    public Vector3 Position {
        get {
            if (agent != null) return agent.position;
            if (transform != null) return transform.position;
            return point;
        }
    }

    /// <summary>
    /// 目标当前速度（静态点和Transform都视为静止）
    /// </summary>
    public Vector3 Velocity => agent != null ? agent.velocity : Vector3.zero;

    /// <summary>
    /// 目标当前朝向
    /// </summary>
    public Vector3 Heading {
        get {
            if (agent != null) return agent.heading;
            if (transform != null) return transform.forward;
            return Vector3.forward;
        }
    }

    /// <summary>
    /// 目标的包围半径
    /// </summary>
    public float Radius => agent?.radius ?? 0f;

    public static implicit operator SteeringTarget(Vector3 point) => new SteeringTarget(point);

    public static implicit operator SteeringTarget(Transform transform) => new SteeringTarget(transform);

    public static implicit operator SteeringTarget(SteeringAgent agent) => new SteeringTarget(agent);

    public override string ToString() {
        if (agent != null) return $"Agent({agent.name})";
        if (transform != null) return $"Transform({transform.name})";
        return $"Point({point})";
    }
}
}
