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
/// 球形障碍
///
/// 注：Steering的避障刻意只用球体做代理，而不用真实碰撞体 ——
/// 避障是"提前规避"而非"碰撞响应"，粗略的包围球足够，且能保证探测的计算量恒定。
/// 复杂形状拆成多个球即可。
/// </summary>
public class SteeringObstacle
{
    /// <summary>
    /// 名字（调试用）
    /// </summary>
    public string name;
    /// <summary>
    /// 圆心
    /// </summary>
    public Vector3 position;
    /// <summary>
    /// 半径
    /// </summary>
    public float radius = 1f;
    /// <summary>
    /// 绑定的Transform；非null时每帧从它同步位置（可做移动障碍）
    /// </summary>
    public Transform transform;

    public SteeringObstacle() {
    }

    public SteeringObstacle(Vector3 position, float radius, string name = null) {
        this.position = position;
        this.radius = radius;
        this.name = name;
    }

    public SteeringObstacle(Transform transform, float radius, string name = null) {
        this.transform = transform;
        this.radius = radius;
        this.name = name ?? transform?.name;
        if (transform != null) position = transform.position;
    }

    /// <summary>
    /// 从Transform同步位置（由管理器每帧调用）
    /// </summary>
    public void SyncFromTransform() {
        if (transform != null) position = transform.position;
    }

    /// <summary>
    /// 点是否在障碍内部
    /// </summary>
    public bool Contains(Vector3 point) {
        return (point - position).sqrMagnitude <= radius * radius;
    }

    public void DrawGizmos() {
        Gizmos.color = SteeringGizmos.ColorObstacle;
        SteeringGizmos.DrawSphere(position, radius);
    }

    public override string ToString() => $"SteeringObstacle({name}, r={radius})";
}
}
