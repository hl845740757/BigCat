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
using Random = System.Random;

namespace Wjybxx.BigCat.Samples.Steering
{
/// <summary>
/// Steering行为基类
///
/// 三层模型（Reynolds）中，行为只处在中间的Steering层：
/// <code>
/// Action Selection  ——  决定"要干什么"（由AI/行为树负责，不在本模块内）
/// Steering          ——  产出一个转向力（本类的职责）
/// Locomotion        ——  把力积分成运动（<see cref="SteeringAgent"/>的职责）
/// </code>
///
/// 实现约定：
/// 1.<see cref="Calculate"/>返回的是<b>力</b>，不是速度、也不是位移。
///   绝大多数行为都遵循"构造desiredVelocity，再减去当前velocity"的范式，
///   可直接复用<see cref="SteeringAgent.SeekForce"/>等原语。
/// 2.不要在<see cref="Calculate"/>里做<c>maxForce</c>截断 —— 截断是合成阶段的事。
/// 3.行为不应修改Agent的运动状态，只读。
/// </summary>
public abstract class SteeringBehaviour
{
    /// <summary>
    /// 是否启用（关闭后不参与合成，也不产生计算开销）
    /// </summary>
    public bool enabled = true;
    /// <summary>
    /// 权重
    ///
    /// 注：不同行为输出力的量级差异很大（例如Separation用距离平方反比，量级远小于Seek），
    /// 所以权重不是"重要程度"，而是"量纲换算 + 重要程度"的混合，必须实测调参。
    /// </summary>
    public float weight = 1f;
    /// <summary>
    /// 被抽中的概率，仅<see cref="ESteeringSumMode.PrioritizedDithering"/>模式下有效
    /// </summary>
    public float probability = 0.5f;

    /// <summary>
    /// 所属Agent（由<see cref="SteeringAgent.AddBehaviour{T}"/>注入）
    /// </summary>
    public SteeringAgent Agent { get; internal set; }

    /// <summary>
    /// 上一次计算出的力（未乘权重、未截断），仅用于调试显示
    /// </summary>
    public Vector3 LastForce { get; private set; }
    /// <summary>
    /// 上一帧是否参与了合成
    ///
    /// 注：<see cref="ESteeringSumMode.Prioritized"/>下被"挤掉"的行为，该值为false ——
    /// 这是排查"为什么这个行为没生效"的第一现场。
    /// </summary>
    public bool LastContributed { get; internal set; }

    /// <summary>
    /// 行为名（默认取类型名）
    /// </summary>
    public virtual string Name => GetType().Name;

    /// <summary>
    /// 计算转向力
    /// </summary>
    /// <param name="deltaTime">本次时间步长；仅Wander这类含内部状态演化的行为需要</param>
    public abstract Vector3 Calculate(float deltaTime);

    /// <summary>
    /// 绘制该行为的调试图元
    ///
    /// 注：只能在<c>OnDrawGizmos</c>调用链内被调用。
    /// </summary>
    public virtual void DrawGizmos() {
    }

    /// <summary>
    /// 由Agent调用：计算并记录调试信息
    /// </summary>
    internal Vector3 CalculateAndRecord(float deltaTime) {
        Vector3 force = Calculate(deltaTime);
        LastForce = force;
        return force;
    }

    #region 便捷访问

    /// <summary>Agent当前位置</summary>
    protected Vector3 Position => Agent.position;
    /// <summary>Agent当前速度</summary>
    protected Vector3 Velocity => Agent.velocity;
    /// <summary>Agent当前朝向</summary>
    protected Vector3 Heading => Agent.heading;
    /// <summary>Agent最大速度</summary>
    protected float MaxSpeed => Agent.maxSpeed;
    /// <summary>所属管理器</summary>
    protected SteeringManager Manager => Agent.Manager;
    /// <summary>可复现的随机源</summary>
    protected Random Rand => Agent.Manager.Random;

    #endregion
}
}
