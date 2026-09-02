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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wjybxx.BigCat.Samples.Steering
{
/// <summary>
/// Steering运动体
///
/// 职责对应Reynolds三层模型的Locomotion层 —— 把行为层产出的力积分成运动：
/// <code>
/// steeringForce = 合成(所有行为)             // 受 maxForce 约束
/// acceleration  = steeringForce / mass
/// velocity      = truncate(velocity + acceleration * dt, maxSpeed)
/// position     += velocity * dt
/// heading      -> 以不超过 maxTurnRate 的角速度趋向 velocity 方向
/// </code>
///
/// 关于局部坐标系：本类维护<see cref="heading"/>(+Z) / <see cref="up"/>(+Y) / <see cref="Side"/>(+X)
/// 三个正交单位向量，与Unity的左手系一致，因此<see cref="LocalToWorld"/>可直接用转置代替求逆。
/// 避障、Wander等行为都在这个局部空间里做计算。
///
/// 注：<see cref="heading"/>与<see cref="velocity"/>刻意解耦 ——
/// 直接令<c>heading = velocity.normalized</c>会让低速时朝向剧烈抖动，
/// 且无法表达"车头方向与实际移动方向不一致"（漂移、侧滑）。
/// </summary>
public class SteeringAgent
{
    #region 标识

    /// <summary>
    /// 唯一id（由管理器分配）
    /// </summary>
    public readonly int id;
    /// <summary>
    /// 所属管理器（构造时注入，不会为null）
    /// </summary>
    public readonly SteeringManager Manager;
    /// <summary>
    /// 名字（调试用）
    /// </summary>
    public string name;
    /// <summary>
    /// 绑定的表现对象；为null时是纯逻辑Agent（可用于无渲染的算法测试）
    /// </summary>
    public Transform transform;
    /// <summary>
    /// 是否参与更新
    /// </summary>
    public bool enabled = true;

    #endregion

    #region 运动状态

    /// <summary>
    /// 位置
    /// </summary>
    public Vector3 position;
    /// <summary>
    /// 速度
    /// </summary>
    public Vector3 velocity;
    /// <summary>
    /// 朝向（单位向量，对应局部+Z）
    /// </summary>
    public Vector3 heading = Vector3.forward;
    /// <summary>
    /// 上方向（单位向量，与<see cref="heading"/>正交，对应局部+Y）
    /// </summary>
    public Vector3 up = Vector3.up;

    #endregion

    #region 运动参数

    /// <summary>
    /// 质量（力转加速度的除数）
    /// </summary>
    public float mass = 1f;
    /// <summary>
    /// 最大速度
    /// </summary>
    public float maxSpeed = 6f;
    /// <summary>
    /// 最大转向力（力预算，同时也是<see cref="ESteeringSumMode.Prioritized"/>的截断阈值）
    /// </summary>
    public float maxForce = 24f;
    /// <summary>
    /// 最大转向角速度（度/秒）；小于等于0表示不限制（朝向瞬间对齐速度方向）
    /// </summary>
    public float maxTurnRate = 360f;
    /// <summary>
    /// 包围半径（避障、分离、可视化都会用到）
    /// </summary>
    public float radius = 0.5f;
    /// <summary>
    /// 感知半径（群体行为的邻居查询半径）
    /// </summary>
    public float neighborRadius = 5f;
    /// <summary>
    /// 速度阻尼（每秒衰减比例，0表示无阻尼）
    ///
    /// 注：经典Steering靠"力反向"减速而不靠阻尼，这里提供阻尼是为了抑制
    /// Wander + Boids 组合下容易出现的速度长期贴顶。
    /// </summary>
    public float damping;
    /// <summary>
    /// 倾斜强度（3D飞行感）：转向时机体向内侧倾斜的程度，0表示<see cref="up"/>恒定趋向<see cref="worldUp"/>
    /// </summary>
    public float banking;
    /// <summary>
    /// 世界上方向（决定无倾斜时的姿态基准）
    /// </summary>
    public Vector3 worldUp = Vector3.up;
    /// <summary>
    /// 力的合成方式
    /// </summary>
    public ESteeringSumMode sumMode = ESteeringSumMode.Prioritized;

    #endregion

    #region 内部状态

    /// <summary>
    /// 行为列表；<b>列表顺序即优先级</b>（下标越小优先级越高）
    /// </summary>
    private readonly List<SteeringBehaviour> _behaviours = new List<SteeringBehaviour>(4);
    /// <summary>
    /// 本帧的邻居（由管理器填充，三个Boids行为共享）
    /// </summary>
    private readonly List<SteeringAgent> _neighbors = new List<SteeringAgent>(8);
    /// <summary>
    /// 是否存在需要邻居列表的行为
    /// </summary>
    private bool _requireNeighbors;

    /// <summary>
    /// 本帧合成出的转向力
    /// </summary>
    private Vector3 _steeringForce;
    /// <summary>
    /// 延迟积分的暂存速度
    /// </summary>
    private Vector3 _pendingVelocity;
    /// <summary>
    /// 延迟积分的暂存位置
    /// </summary>
    private Vector3 _pendingPosition;
    /// <summary>
    /// 是否已执行<see cref="Step"/>但未<see cref="Commit"/>
    /// </summary>
    private bool _stepped;

    /// <summary>
    /// 轨迹环形缓冲（null表示未启用）
    /// </summary>
    private Vector3[] _trailBuffer;
    /// <summary>
    /// 轨迹有效点数
    /// </summary>
    private int _trailCount;
    /// <summary>
    /// 轨迹下一个写入下标
    /// </summary>
    private int _trailNext;
    /// <summary>
    /// 轨迹采样计时器
    /// </summary>
    private float _trailTimer;

    #endregion

    internal SteeringAgent(SteeringManager manager, int id, string name, Transform transform) {
        this.Manager = manager ?? throw new ArgumentNullException(nameof(manager));
        this.id = id;
        this.name = name ?? $"Agent#{id}";
        this.transform = transform;
        if (transform != null) {
            SyncFromTransform();
        }
    }

    #region 只读属性

    /// <summary>
    /// 侧方向（单位向量，对应局部+X，即右方）
    /// </summary>
    public Vector3 Side => Vector3.Cross(up, heading);
    /// <summary>
    /// 当前速率
    /// </summary>
    public float Speed => velocity.magnitude;
    /// <summary>
    /// 当前速率的平方（避免开方）
    /// </summary>
    public float SqrSpeed => velocity.sqrMagnitude;
    /// <summary>
    /// 速率占最大速率的比例，[0,1]
    /// </summary>
    public float SpeedRatio => maxSpeed <= 0f ? 0f : Mathf.Clamp01(Speed / maxSpeed);
    /// <summary>
    /// 本帧合成出的转向力
    /// </summary>
    public Vector3 SteeringForce => _steeringForce;
    /// <summary>
    /// 行为列表（顺序即优先级）
    /// </summary>
    public IReadOnlyList<SteeringBehaviour> Behaviours => _behaviours;
    /// <summary>
    /// 本帧的邻居列表
    ///
    /// 注：由管理器每帧重填，行为层只读；不要长期持有其中的元素。
    /// </summary>
    public List<SteeringAgent> Neighbors => _neighbors;
    /// <summary>
    /// 是否需要邻居查询
    /// </summary>
    public bool RequireNeighbors => _requireNeighbors;

    #endregion

    #region 行为管理

    /// <summary>
    /// 追加一个行为
    ///
    /// 注：<b>添加顺序即优先级</b>，先添加的优先级高。
    /// 在<see cref="ESteeringSumMode.Prioritized"/>下这直接决定谁能拿到力预算，
    /// 因此避障类行为必须先添加。
    /// </summary>
    public T AddBehaviour<T>(T behaviour) where T : SteeringBehaviour {
        if (behaviour == null) throw new ArgumentNullException(nameof(behaviour));
        if (behaviour.Agent != null) {
            throw new ArgumentException($"behaviour {behaviour.Name} already attached to agent {behaviour.Agent.name}");
        }
        behaviour.Agent = this;
        _behaviours.Add(behaviour);
        if (behaviour is ISteeringNeighborConsumer) {
            _requireNeighbors = true;
        }
        return behaviour;
    }

    /// <summary>
    /// 在指定优先级位置插入行为
    /// </summary>
    public T InsertBehaviour<T>(int index, T behaviour) where T : SteeringBehaviour {
        if (behaviour == null) throw new ArgumentNullException(nameof(behaviour));
        if (behaviour.Agent != null) {
            throw new ArgumentException($"behaviour {behaviour.Name} already attached to agent {behaviour.Agent.name}");
        }
        behaviour.Agent = this;
        _behaviours.Insert(index, behaviour);
        if (behaviour is ISteeringNeighborConsumer) {
            _requireNeighbors = true;
        }
        return behaviour;
    }

    /// <summary>
    /// 移除行为
    /// </summary>
    public bool RemoveBehaviour(SteeringBehaviour behaviour) {
        if (behaviour == null || behaviour.Agent != this) return false;
        if (!_behaviours.Remove(behaviour)) return false;
        behaviour.Agent = null;
        if (behaviour is ISteeringNeighborConsumer) {
            RefreshRequireNeighbors();
        }
        return true;
    }

    /// <summary>
    /// 清空行为
    /// </summary>
    public void ClearBehaviours() {
        for (int i = 0; i < _behaviours.Count; i++) {
            _behaviours[i].Agent = null;
        }
        _behaviours.Clear();
        _neighbors.Clear();
        _requireNeighbors = false;
    }

    /// <summary>
    /// 查找指定类型的行为（返回第一个匹配项）
    /// </summary>
    public T GetBehaviour<T>() where T : SteeringBehaviour {
        for (int i = 0; i < _behaviours.Count; i++) {
            if (_behaviours[i] is T match) return match;
        }
        return null;
    }

    /// <summary>
    /// 设置指定类型行为的启用状态
    /// </summary>
    public bool SetBehaviourEnabled<T>(bool value) where T : SteeringBehaviour {
        T behaviour = GetBehaviour<T>();
        if (behaviour == null) return false;
        behaviour.enabled = value;
        return true;
    }

    private void RefreshRequireNeighbors() {
        _requireNeighbors = false;
        for (int i = 0; i < _behaviours.Count; i++) {
            if (_behaviours[i] is ISteeringNeighborConsumer) {
                _requireNeighbors = true;
                return;
            }
        }
    }

    #endregion

    #region 力的合成

    /// <summary>
    /// 按<see cref="sumMode"/>合成所有行为的力
    /// </summary>
    public Vector3 CalculateForce(float deltaTime) {
        for (int i = 0; i < _behaviours.Count; i++) {
            _behaviours[i].LastContributed = false;
        }
        switch (sumMode) {
            case ESteeringSumMode.WeightedSum:
                _steeringForce = SumWeighted(deltaTime);
                break;
            case ESteeringSumMode.Prioritized:
                _steeringForce = SumPrioritized(deltaTime);
                break;
            case ESteeringSumMode.PrioritizedDithering:
                _steeringForce = SumDithering(deltaTime);
                break;
            default:
                _steeringForce = Vector3.zero;
                break;
        }
        return _steeringForce;
    }

    /// <summary>
    /// 加权求和：全部累加后统一截断
    /// </summary>
    private Vector3 SumWeighted(float deltaTime) {
        Vector3 total = Vector3.zero;
        for (int i = 0; i < _behaviours.Count; i++) {
            SteeringBehaviour behaviour = _behaviours[i];
            if (!behaviour.enabled) continue;
            Vector3 force = behaviour.CalculateAndRecord(deltaTime) * behaviour.weight;
            if (SteeringUtil.IsZero(force)) continue;
            total += force;
            behaviour.LastContributed = true;
        }
        return SteeringUtil.Truncate(total, maxForce);
    }

    /// <summary>
    /// 优先级截断：按顺序累加，预算用尽即停止后续计算
    /// </summary>
    private Vector3 SumPrioritized(float deltaTime) {
        Vector3 total = Vector3.zero;
        for (int i = 0; i < _behaviours.Count; i++) {
            SteeringBehaviour behaviour = _behaviours[i];
            if (!behaviour.enabled) continue;
            Vector3 force = behaviour.CalculateAndRecord(deltaTime) * behaviour.weight;
            if (SteeringUtil.IsZero(force)) continue;
            bool budgetLeft = AccumulateForce(ref total, force, maxForce, out bool contributed);
            behaviour.LastContributed = contributed;
            if (!budgetLeft) break; // 预算耗尽，后续行为连计算都省掉
        }
        return total;
    }

    /// <summary>
    /// 优先级抽样：按顺序抽样，命中即返回
    /// </summary>
    private Vector3 SumDithering(float deltaTime) {
        for (int i = 0; i < _behaviours.Count; i++) {
            SteeringBehaviour behaviour = _behaviours[i];
            if (!behaviour.enabled) continue;
            if (Manager.Random.NextDouble() > behaviour.probability) continue;

            Vector3 force = behaviour.CalculateAndRecord(deltaTime) * behaviour.weight;
            if (SteeringUtil.IsZero(force)) continue;
            // 除以概率做无偏补偿：单帧力被放大，但帧间期望与加权求和一致
            force /= Mathf.Max(behaviour.probability, SteeringUtil.Epsilon);
            behaviour.LastContributed = true;
            return SteeringUtil.Truncate(force, maxForce);
        }
        return Vector3.zero;
    }

    /// <summary>
    /// 把<paramref name="add"/>累加到<paramref name="total"/>，不超过<paramref name="maxForce"/>预算
    ///
    /// 注：预算是<b>标量</b>的 —— 约束的是"各分力长度之和 ≤ maxForce"，
    /// 而非"合力长度 ≤ maxForce"。由三角不等式，合力长度只会更小：
    /// 例如预算10、高优先级用掉(4,0,0)、次优先级被截断为(0,6,0)时，
    /// 分力长度和恰为10，但合力长度只有7.21。
    /// 换言之各行为方向冲突时，一部分力预算会消耗在相互抵消上，
    /// 这是优先级方案换取"高优先级行为一定生效"所付的代价。
    /// </summary>
    /// <param name="total">已累加的力</param>
    /// <param name="add">待累加的力</param>
    /// <param name="maxForce">力预算</param>
    /// <param name="contributed">本次是否产生了实际贡献</param>
    /// <returns>是否还有剩余预算（false表示应停止累加）</returns>
    private static bool AccumulateForce(ref Vector3 total, Vector3 add, float maxForce, out bool contributed) {
        contributed = false;
        float used = total.magnitude;
        float remain = maxForce - used;
        if (remain <= SteeringUtil.Epsilon) return false;

        float addMag = add.magnitude;
        if (addMag < remain) {
            total += add;
            contributed = true;
            return true;
        }
        // 预算只够一部分：按比例截断后停止
        total += add * (remain / addMag);
        contributed = true;
        return false;
    }

    #endregion

    #region 运动积分

    /// <summary>
    /// 阶段1：计算力并预演运动（<b>不</b>修改<see cref="position"/>/<see cref="velocity"/>）
    ///
    /// 注：拆成Step/Commit两阶段是为了让所有Agent都基于同一时刻的状态做决策 ——
    /// 否则先更新的Agent会影响后更新的Agent的邻居计算，群体行为会出现与更新顺序相关的偏移。
    /// </summary>
    public void Step(float deltaTime) {
        CalculateForce(deltaTime);

        Vector3 acceleration = _steeringForce / Mathf.Max(mass, SteeringUtil.Epsilon);
        Vector3 newVelocity = velocity + acceleration * deltaTime;
        if (damping > 0f) {
            newVelocity *= Mathf.Clamp01(1f - damping * deltaTime);
        }
        newVelocity = SteeringUtil.Truncate(newVelocity, maxSpeed);

        _pendingVelocity = newVelocity;
        _pendingPosition = position + newVelocity * deltaTime;
        _stepped = true;
    }

    /// <summary>
    /// 阶段2：提交预演结果并更新姿态
    /// </summary>
    public void Commit(float deltaTime) {
        if (!_stepped) return;
        _stepped = false;
        velocity = _pendingVelocity;
        position = _pendingPosition;
        UpdateOrientation(deltaTime);
    }

    /// <summary>
    /// 更新朝向与上方向
    /// </summary>
    private void UpdateOrientation(float deltaTime) {
        float maxRadians = maxTurnRate > 0f
            ? maxTurnRate * Mathf.Deg2Rad * deltaTime
            : Mathf.PI * 2f; // 不限制
        Vector3 unitWorldUp = SteeringUtil.SafeNormalize(worldUp, Vector3.up);

        // 朝向趋向速度方向；速度为0时保持原朝向（而不是归零）
        if (velocity.sqrMagnitude > SteeringUtil.SqrEpsilon) {
            Vector3 desiredHeading = velocity.normalized;
            heading = SteeringUtil.SafeNormalize(
                Vector3.RotateTowards(heading, desiredHeading, maxRadians, 0f), desiredHeading);
        }

        // 上方向：banking>0时按侧向加速度向内倾斜
        Vector3 desiredUp = unitWorldUp;
        if (banking > 0f) {
            Vector3 lateralAccel = SteeringUtil.ProjectOnPlane(
                _steeringForce / Mathf.Max(mass, SteeringUtil.Epsilon), heading);
            desiredUp = unitWorldUp - lateralAccel * banking;
        }
        // 正交化：去掉heading方向的分量
        desiredUp = SteeringUtil.ProjectOnPlane(desiredUp, heading);
        if (SteeringUtil.IsZero(desiredUp)) {
            // heading与worldUp共线（垂直上升/俯冲）：保持当前up，避免姿态突变
            desiredUp = SteeringUtil.ProjectOnPlane(up, heading);
            if (SteeringUtil.IsZero(desiredUp)) {
                desiredUp = SteeringUtil.AnyPerpendicular(heading);
            }
        }
        desiredUp.Normalize();

        up = Vector3.RotateTowards(up, desiredUp, maxRadians, 0f);
        up = SteeringUtil.SafeNormalize(SteeringUtil.ProjectOnPlane(up, heading), desiredUp);
    }

    /// <summary>
    /// 重置运动状态（保留行为与参数）
    /// </summary>
    public void ResetMotion(Vector3 newPosition, Vector3 newHeading) {
        position = newPosition;
        velocity = Vector3.zero;
        heading = SteeringUtil.SafeNormalize(newHeading, Vector3.forward);
        up = SteeringUtil.SafeNormalize(
            SteeringUtil.ProjectOnPlane(SteeringUtil.SafeNormalize(worldUp, Vector3.up), heading)
            , SteeringUtil.AnyPerpendicular(heading));
        _steeringForce = Vector3.zero;
        _pendingVelocity = Vector3.zero;
        _pendingPosition = newPosition;
        _stepped = false;
        _trailCount = 0;
        _trailNext = 0;
        _trailTimer = 0f;
    }

    #endregion

    #region Transform同步

    /// <summary>
    /// 把逻辑状态写回Transform
    /// </summary>
    public void ApplyToTransform() {
        if (transform == null) return;
        transform.position = position;
        if (!SteeringUtil.IsZero(heading)) {
            transform.rotation = Quaternion.LookRotation(heading, up);
        }
    }

    /// <summary>
    /// 从Transform读取初始状态
    /// </summary>
    public void SyncFromTransform() {
        if (transform == null) return;
        position = transform.position;
        heading = transform.forward;
        up = transform.up;
        _pendingPosition = position;
    }

    #endregion

    #region 局部/世界空间变换

    /// <summary>
    /// 局部方向转世界方向（局部空间：+X=Side, +Y=up, +Z=heading）
    /// </summary>
    public Vector3 LocalToWorldDirection(Vector3 localDirection) {
        Vector3 side = Side;
        return side * localDirection.x + up * localDirection.y + heading * localDirection.z;
    }

    /// <summary>
    /// 局部坐标转世界坐标
    /// </summary>
    public Vector3 LocalToWorld(Vector3 localPosition) {
        return position + LocalToWorldDirection(localPosition);
    }

    /// <summary>
    /// 世界方向转局部方向
    ///
    /// 注：基向量正交归一，所以逆变换就是转置（三次点积），无需求矩阵逆。
    /// </summary>
    public Vector3 WorldToLocalDirection(Vector3 worldDirection) {
        Vector3 side = Side;
        return new Vector3(Vector3.Dot(worldDirection, side)
            , Vector3.Dot(worldDirection, up)
            , Vector3.Dot(worldDirection, heading));
    }

    /// <summary>
    /// 世界坐标转局部坐标
    /// </summary>
    public Vector3 WorldToLocal(Vector3 worldPosition) {
        return WorldToLocalDirection(worldPosition - position);
    }

    #endregion

    #region 转向力原语

    /// <summary>
    /// Seek：全速冲向目标
    ///
    /// 这是所有Steering行为的原型：<c>desiredVelocity - velocity</c>。
    /// 注意它不会在目标处停下，只会绕着目标反复过冲。
    ///
    /// 注：目标与自身重合时方向未定义，此时沿<see cref="heading"/>继续冲 ——
    /// 若在此退化为零向量，力就变成<c>-velocity</c>（完美刹车），Seek会静止在目标点，
    /// 行为退化成Arrive，反而掩盖了Seek必然过冲这一特征。
    /// </summary>
    public Vector3 SeekForce(Vector3 target) {
        Vector3 desiredVelocity = SteeringUtil.SafeNormalize(target - position, heading) * maxSpeed;
        return desiredVelocity - velocity;
    }

    /// <summary>
    /// Flee：全速远离目标
    /// </summary>
    /// <param name="target">威胁源</param>
    /// <param name="panicDistance">恐慌距离；大于0时，超出该距离不产生力</param>
    public Vector3 FleeForce(Vector3 target, float panicDistance = 0f) {
        Vector3 offset = position - target;
        if (panicDistance > 0f && offset.sqrMagnitude > panicDistance * panicDistance) {
            return Vector3.zero;
        }
        Vector3 desiredVelocity = SteeringUtil.SafeNormalize(offset, heading) * maxSpeed;
        return desiredVelocity - velocity;
    }

    /// <summary>
    /// Arrive：抵达目标并停下
    ///
    /// 期望速率随距离线性衰减：<c>speed = distance / (deceleration * tweaker)</c>。
    ///
    /// 注：本方法返回的是<c>desiredVelocity - velocity</c>，于是加速度为
    /// <c>(desiredVelocity - velocity) / mass</c> —— 这是一个一阶速度跟踪器，
    /// <b>时间常数就是mass</b>。因此参数自洽性取决于mass而非maxForce：
    /// 制动时间<c>deceleration * tweaker</c>必须显著大于mass，否则期望速率下降太快、
    /// 实际速度跟不上，就会过冲。mass=1、maxSpeed=6、maxForce=24下的实测过冲量：
    /// <code>
    /// deceleration*tweaker |  0.6s |  1.0s |  1.6s |  2.4s |  4.0s
    /// 过冲距离             | 2.006 | 1.757 | 1.166 | 0.428 | 0.000
    /// </code>
    /// 即需要约<c>4 * mass</c>的制动时间才能完全消除过冲。
    /// Buckland原书用的<c>tweaker = 0.3</c>属于上表最左列，会有明显过冲。
    /// </summary>
    public Vector3 ArriveForce(Vector3 target, ESteeringDeceleration deceleration = ESteeringDeceleration.Normal
        , float tweaker = 1f) {
        Vector3 toTarget = target - position;
        float distance = toTarget.magnitude;
        if (distance < SteeringUtil.Epsilon) {
            return -velocity; // 已抵达：全力刹停
        }
        float speed = distance / ((int)deceleration * Mathf.Max(tweaker, SteeringUtil.Epsilon));
        speed = Mathf.Min(speed, maxSpeed);
        Vector3 desiredVelocity = toTarget * (speed / distance);
        return desiredVelocity - velocity;
    }

    /// <summary>
    /// 估算转向到目标方向所需的归一化时间
    ///
    /// 返回值域为[0, 2 * <paramref name="coefficient"/>]：正前方为0，正后方最大。
    /// 用于Pursuit/Evade修正预测时间 —— 目标在背后时，需要额外时间掉头。
    /// </summary>
    public float PredictTurnTime(Vector3 target, float coefficient = 0.5f) {
        Vector3 toTarget = SteeringUtil.SafeNormalize(target - position, heading);
        float dot = Vector3.Dot(heading, toTarget);
        return (dot - 1f) * -coefficient;
    }

    #endregion

    #region 调试

    /// <summary>
    /// 启用轨迹记录
    /// </summary>
    /// <param name="capacity">最多保留的采样点数；小于2表示关闭</param>
    public void EnableTrail(int capacity) {
        if (capacity < 2) {
            _trailBuffer = null;
            _trailCount = 0;
            _trailNext = 0;
            return;
        }
        _trailBuffer = new Vector3[capacity];
        _trailCount = 0;
        _trailNext = 0;
        _trailTimer = 0f;
    }

    /// <summary>
    /// 采样轨迹（由管理器调用）
    /// </summary>
    internal void RecordTrail(float deltaTime, float interval) {
        if (_trailBuffer == null) return;
        _trailTimer += deltaTime;
        if (_trailTimer < interval) return;
        _trailTimer = 0f;

        _trailBuffer[_trailNext] = position;
        _trailNext = (_trailNext + 1) % _trailBuffer.Length;
        if (_trailCount < _trailBuffer.Length) _trailCount++;
    }

    /// <summary>
    /// 绘制调试图元
    /// </summary>
    public void DrawGizmos(ESteeringDebugFlags flags) {
        if ((flags & ESteeringDebugFlags.Agent) != 0) {
            Gizmos.color = enabled ? SteeringGizmos.ColorAgent : Color.gray;
            SteeringGizmos.DrawCircle(position, up, radius);
            SteeringGizmos.DrawCircle(position, heading, radius);
        }
        if ((flags & ESteeringDebugFlags.Axis) != 0) {
            float axisLen = radius * 2.5f;
            Gizmos.color = Color.red;
            SteeringGizmos.DrawArrow(position, position + heading * axisLen);
            Gizmos.color = Color.green;
            SteeringGizmos.DrawArrow(position, position + up * axisLen);
            Gizmos.color = Color.blue;
            SteeringGizmos.DrawArrow(position, position + Side * axisLen);
        }
        if ((flags & ESteeringDebugFlags.Velocity) != 0) {
            Gizmos.color = SteeringGizmos.ColorVelocity;
            SteeringGizmos.DrawVector(position, velocity, Manager.gizmoVelocityScale);
        }
        if ((flags & ESteeringDebugFlags.Force) != 0) {
            Gizmos.color = SteeringGizmos.ColorForce;
            SteeringGizmos.DrawVector(position, _steeringForce, Manager.gizmoForceScale);
        }
        if ((flags & ESteeringDebugFlags.Neighbor) != 0 && _requireNeighbors) {
            Gizmos.color = SteeringGizmos.ColorProbe;
            SteeringGizmos.DrawCircle(position, worldUp, neighborRadius);
            Gizmos.color = SteeringGizmos.ColorNeighbor;
            for (int i = 0; i < _neighbors.Count; i++) {
                Gizmos.DrawLine(position, _neighbors[i].position);
            }
        }
        if ((flags & ESteeringDebugFlags.Trail) != 0) {
            Gizmos.color = SteeringGizmos.ColorTrail;
            DrawTrail();
        }
        if ((flags & ESteeringDebugFlags.Behaviour) != 0) {
            for (int i = 0; i < _behaviours.Count; i++) {
                SteeringBehaviour behaviour = _behaviours[i];
                if (behaviour.enabled) behaviour.DrawGizmos();
            }
        }
        if ((flags & ESteeringDebugFlags.Label) != 0) {
            SteeringGizmos.DrawLabel(position + up * (radius * 2f), BuildDebugText());
        }
    }

    private void DrawTrail() {
        if (_trailBuffer == null || _trailCount < 2) return;
        // 缓冲满了之后，最老的点在_trailNext处
        int start = _trailCount == _trailBuffer.Length ? _trailNext : 0;
        Vector3 prev = _trailBuffer[start];
        for (int i = 1; i < _trailCount; i++) {
            Vector3 cur = _trailBuffer[(start + i) % _trailBuffer.Length];
            Gizmos.DrawLine(prev, cur);
            prev = cur;
        }
    }

    /// <summary>
    /// 构造调试文本（名字、速率、生效中的行为）
    /// </summary>
    public string BuildDebugText() {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(64);
        sb.Append(name).Append(" v=").Append(Speed.ToString("F1"));
        for (int i = 0; i < _behaviours.Count; i++) {
            SteeringBehaviour behaviour = _behaviours[i];
            if (!behaviour.enabled || !behaviour.LastContributed) continue;
            sb.Append('\n').Append(behaviour.Name);
        }
        return sb.ToString();
    }

    public override string ToString() => $"SteeringAgent({name})";

    #endregion
}
}
