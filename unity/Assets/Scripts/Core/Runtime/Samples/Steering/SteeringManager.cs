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
using Random = System.Random;

namespace Wjybxx.BigCat.Samples.Steering
{
/// <summary>
/// Steering管理器
///
/// 1.该管理器是纯C#类（与<c>InputManager</c>同风格），心跳由外部驱动 ——
///   在帧循环中调用<see cref="Update"/>，在<c>OnDrawGizmos</c>中调用<see cref="DrawGizmos"/>。
///   <see cref="SteeringSample"/>提供了一个现成的MonoBehaviour驱动器。
/// 2.管理器持有全部Agent与环境对象（障碍/墙面），并负责：
///   统一时间步、邻居查询、两阶段积分、以及把结果写回<see cref="Transform"/>。
/// 3.<see cref="Random"/>是唯一的随机源，构造时指定种子即可完整复现一次运行 ——
///   调试Wander/Dithering这类含随机的逻辑时必备。
///
/// 典型用法：
/// <code>
/// SteeringManager mgr = new SteeringManager(seed: 1);
/// mgr.EnableCellSpace(new Bounds(Vector3.zero, Vector3.one * 60f), 8, 4, 8);
///
/// SteeringAgent agent = mgr.CreateAgent("hunter", someTransform);
/// agent.AddBehaviour(new ObstacleAvoidanceBehaviour { weight = 10f }); // 先加 = 优先级高
/// agent.AddBehaviour(new WanderBehaviour { weight = 1f });
///
/// // 帧循环
/// mgr.Update(Time.deltaTime);
/// </code>
/// </summary>
public class SteeringManager
{
    /// <summary>
    /// 全局实例（可选，方便Sample与调试面板取用）
    /// </summary>
    public static SteeringManager Inst { get; set; }

    #region 字段

    private readonly List<SteeringAgent> _agents = new List<SteeringAgent>(64);
    private readonly List<SteeringObstacle> _obstacles = new List<SteeringObstacle>(32);
    private readonly List<SteeringWall> _walls = new List<SteeringWall>(8);
    /// <summary>
    /// 随机源（种子固定则运行可复现）
    /// </summary>
    private readonly Random _random;
    /// <summary>
    /// Agent的id分配器
    /// </summary>
    private int _idSequence;
    /// <summary>
    /// 空间划分（null表示用暴力查询）
    /// </summary>
    private SteeringCellSpace _cellSpace;
    /// <summary>
    /// 固定步长模式的时间累积器
    /// </summary>
    private float _accumulator;

    #endregion

    #region 配置

    /// <summary>
    /// 是否暂停
    /// </summary>
    public bool paused;
    /// <summary>
    /// 时间缩放（调试时放慢观察很有用）
    /// </summary>
    public float timeScale = 1f;
    /// <summary>
    /// 单帧时间步上限
    ///
    /// 注：不做钳制的话，一次卡顿（例如编辑器编译）会让Agent瞬移穿过障碍。
    /// </summary>
    public float maxDeltaTime = 0.05f;
    /// <summary>
    /// 固定步长；大于0时启用定步长累积（结果与帧率无关，便于对比不同参数）
    /// </summary>
    public float fixedDeltaTime;
    /// <summary>
    /// 固定步长模式下单帧最多执行的子步数（防止追帧雪崩）
    /// </summary>
    public int maxSubSteps = 4;
    /// <summary>
    /// 是否两阶段积分：先让所有Agent算力，再统一提交
    ///
    /// 注：关闭后逐个Agent"算完即提交"，群体行为的结果会依赖Agent的更新顺序 ——
    /// 这是很多Boids实现里"鱼群总体向列表末尾偏移"的根因。保持开启。
    /// </summary>
    public bool deferredIntegration = true;
    /// <summary>
    /// 是否把逻辑状态写回Transform
    /// </summary>
    public bool applyToTransform = true;
    /// <summary>
    /// 轨迹采样间隔（秒）
    /// </summary>
    public float trailInterval = 0.06f;

    #endregion

    #region 调试配置

    /// <summary>
    /// 调试绘制开关
    /// </summary>
    public ESteeringDebugFlags debugFlags = ESteeringDebugFlags.Default;
    /// <summary>
    /// 速度箭头的长度缩放
    /// </summary>
    public float gizmoVelocityScale = 0.4f;
    /// <summary>
    /// 力箭头的长度缩放
    /// </summary>
    public float gizmoForceScale = 0.12f;
    /// <summary>
    /// 活动区域（仅用于绘制与随机生成，不产生约束力；约束请用<see cref="BoundsContainmentBehaviour"/>）
    /// </summary>
    public Bounds activeBounds = new Bounds(Vector3.zero, new Vector3(60f, 30f, 60f));

    #endregion

    #region 运行时统计

    /// <summary>
    /// 当前时间步长（行为层可读取）
    /// </summary>
    public float DeltaTime { get; private set; }
    /// <summary>
    /// 累计tick次数
    /// </summary>
    public int TickCount { get; private set; }
    /// <summary>
    /// 上一tick执行了邻居查询的Agent数
    /// </summary>
    public int LastNeighborQueryCount { get; private set; }
    /// <summary>
    /// 上一tick邻居查询命中的总数（观测群体密度）
    /// </summary>
    public int LastNeighborTotal { get; private set; }

    #endregion

    /// <summary>
    /// </summary>
    /// <param name="seed">随机种子；固定种子可完整复现一次运行</param>
    public SteeringManager(int seed = 20250902) {
        _random = new Random(seed);
    }

    #region 只读属性

    /// <summary>
    /// 随机源
    /// </summary>
    public Random Random => _random;
    /// <summary>
    /// 全部Agent
    /// </summary>
    public IReadOnlyList<SteeringAgent> Agents => _agents;
    /// <summary>
    /// 全部障碍
    /// </summary>
    public IReadOnlyList<SteeringObstacle> Obstacles => _obstacles;
    /// <summary>
    /// 全部墙面
    /// </summary>
    public IReadOnlyList<SteeringWall> Walls => _walls;
    /// <summary>
    /// 空间划分（未启用时为null）
    /// </summary>
    public SteeringCellSpace CellSpace => _cellSpace;

    #endregion

    #region Agent管理

    /// <summary>
    /// 创建Agent
    /// </summary>
    /// <param name="name">名字，null时自动生成</param>
    /// <param name="transform">绑定的表现对象，可为null（纯逻辑Agent）</param>
    public SteeringAgent CreateAgent(string name = null, Transform transform = null) {
        SteeringAgent agent = new SteeringAgent(this, ++_idSequence, name, transform);
        _agents.Add(agent);
        return agent;
    }

    /// <summary>
    /// 移除Agent
    /// </summary>
    public bool RemoveAgent(SteeringAgent agent) {
        return agent != null && _agents.Remove(agent);
    }

    /// <summary>
    /// 按名字查找Agent
    /// </summary>
    public SteeringAgent FindAgent(string name) {
        for (int i = 0; i < _agents.Count; i++) {
            if (_agents[i].name == name) return _agents[i];
        }
        return null;
    }

    /// <summary>
    /// 批量设置力的合成方式（对比三种模式的差异时很方便）
    /// </summary>
    public void SetSumMode(ESteeringSumMode sumMode) {
        for (int i = 0; i < _agents.Count; i++) {
            _agents[i].sumMode = sumMode;
        }
    }

    /// <summary>
    /// 批量启用轨迹记录
    /// </summary>
    public void EnableTrail(int capacity) {
        for (int i = 0; i < _agents.Count; i++) {
            _agents[i].EnableTrail(capacity);
        }
    }

    #endregion

    #region 环境对象管理

    public SteeringObstacle AddObstacle(Vector3 position, float radius, string name = null) {
        SteeringObstacle obstacle = new SteeringObstacle(position, radius, name);
        _obstacles.Add(obstacle);
        return obstacle;
    }

    public SteeringObstacle AddObstacle(SteeringObstacle obstacle) {
        if (obstacle == null) throw new ArgumentNullException(nameof(obstacle));
        _obstacles.Add(obstacle);
        return obstacle;
    }

    public bool RemoveObstacle(SteeringObstacle obstacle) {
        return obstacle != null && _obstacles.Remove(obstacle);
    }

    public SteeringWall AddWall(SteeringWall wall) {
        if (wall == null) throw new ArgumentNullException(nameof(wall));
        _walls.Add(wall);
        return wall;
    }

    /// <summary>
    /// 添加一个盒形房间的六面墙（法线朝内）
    /// </summary>
    public void AddRoomWalls(Bounds bounds) {
        SteeringWall.CreateRoom(bounds, _walls);
    }

    /// <summary>
    /// 清空所有Agent与环境对象
    /// </summary>
    public void Clear() {
        _agents.Clear();
        _obstacles.Clear();
        _walls.Clear();
        _accumulator = 0f;
        TickCount = 0;
    }

    #endregion

    #region 空间划分

    /// <summary>
    /// 启用空间划分加速邻居查询
    /// </summary>
    /// <param name="bounds">覆盖范围（应包含全部Agent的活动区域）</param>
    /// <param name="countX">x轴单元数</param>
    /// <param name="countY">y轴单元数</param>
    /// <param name="countZ">z轴单元数</param>
    public void EnableCellSpace(Bounds bounds, int countX, int countY, int countZ) {
        _cellSpace = new SteeringCellSpace(bounds, countX, countY, countZ);
    }

    /// <summary>
    /// 关闭空间划分（退化为暴力查询，Agent很少时反而更快）
    /// </summary>
    public void DisableCellSpace() {
        _cellSpace = null;
    }

    /// <summary>
    /// 查询邻居
    /// </summary>
    /// <param name="agent">查询者（会被排除在结果外）</param>
    /// <param name="radius">查询半径</param>
    /// <param name="result">输出列表，会被先清空</param>
    public void QueryNeighbors(SteeringAgent agent, float radius, List<SteeringAgent> result) {
        if (_cellSpace != null) {
            _cellSpace.Query(agent.position, radius, agent, result);
            return;
        }
        result.Clear();
        float sqrRadius = radius * radius;
        Vector3 center = agent.position;
        for (int i = 0; i < _agents.Count; i++) {
            SteeringAgent other = _agents[i];
            if (other == agent || !other.enabled) continue;
            if ((other.position - center).sqrMagnitude <= sqrRadius) {
                result.Add(other);
            }
        }
    }

    #endregion

    #region 心跳

    /// <summary>
    /// 驱动一帧
    /// </summary>
    /// <param name="deltaTime">帧间隔，通常传<c>Time.deltaTime</c></param>
    public void Update(float deltaTime) {
        if (paused || deltaTime <= 0f) return;
        deltaTime *= timeScale;
        if (deltaTime <= 0f) return;

        if (fixedDeltaTime > 0f) {
            _accumulator += deltaTime;
            // 累积上限：卡顿后不追帧，宁可慢放也不要瞬移
            float maxAccumulate = fixedDeltaTime * Mathf.Max(1, maxSubSteps);
            if (_accumulator > maxAccumulate) _accumulator = maxAccumulate;
            while (_accumulator >= fixedDeltaTime) {
                _accumulator -= fixedDeltaTime;
                Tick(fixedDeltaTime);
            }
        } else {
            Tick(Mathf.Min(deltaTime, maxDeltaTime));
        }

        if (applyToTransform) {
            for (int i = 0; i < _agents.Count; i++) {
                _agents[i].ApplyToTransform();
            }
        }
    }

    private void Tick(float deltaTime) {
        DeltaTime = deltaTime;
        TickCount++;

        // 1.同步跟随Transform的障碍
        for (int i = 0; i < _obstacles.Count; i++) {
            _obstacles[i].SyncFromTransform();
        }

        // 2.重建空间索引
        _cellSpace?.Rebuild(_agents);

        // 3.邻居查询（只为真正需要的Agent做；三个Boids行为共享同一份结果）
        LastNeighborQueryCount = 0;
        LastNeighborTotal = 0;
        for (int i = 0; i < _agents.Count; i++) {
            SteeringAgent agent = _agents[i];
            if (!agent.enabled || !agent.RequireNeighbors) continue;
            QueryNeighbors(agent, agent.neighborRadius, agent.Neighbors);
            LastNeighborQueryCount++;
            LastNeighborTotal += agent.Neighbors.Count;
        }

        // 4.两阶段积分：全部算力 -> 全部提交
        if (deferredIntegration) {
            for (int i = 0; i < _agents.Count; i++) {
                SteeringAgent agent = _agents[i];
                if (agent.enabled) agent.Step(deltaTime);
            }
            for (int i = 0; i < _agents.Count; i++) {
                SteeringAgent agent = _agents[i];
                if (agent.enabled) agent.Commit(deltaTime);
            }
        } else {
            for (int i = 0; i < _agents.Count; i++) {
                SteeringAgent agent = _agents[i];
                if (!agent.enabled) continue;
                agent.Step(deltaTime);
                agent.Commit(deltaTime);
            }
        }

        // 5.轨迹采样
        if ((debugFlags & ESteeringDebugFlags.Trail) != 0) {
            for (int i = 0; i < _agents.Count; i++) {
                _agents[i].RecordTrail(deltaTime, trailInterval);
            }
        }
    }

    #endregion

    #region 调试

    /// <summary>
    /// 绘制全部调试图元
    ///
    /// 注：只能在<c>OnDrawGizmos</c>/<c>OnDrawGizmosSelected</c>调用链内调用。
    /// </summary>
    public void DrawGizmos() {
        if (debugFlags == ESteeringDebugFlags.None) return;

        if ((debugFlags & ESteeringDebugFlags.Bounds) != 0) {
            Gizmos.color = SteeringGizmos.ColorBounds;
            Gizmos.DrawWireCube(activeBounds.center, activeBounds.size);
        }
        if ((debugFlags & ESteeringDebugFlags.CellSpace) != 0) {
            _cellSpace?.DrawGizmos();
        }
        if ((debugFlags & ESteeringDebugFlags.Obstacle) != 0) {
            for (int i = 0; i < _obstacles.Count; i++) {
                _obstacles[i].DrawGizmos();
            }
        }
        if ((debugFlags & ESteeringDebugFlags.Wall) != 0) {
            for (int i = 0; i < _walls.Count; i++) {
                _walls[i].DrawGizmos();
            }
        }
        for (int i = 0; i < _agents.Count; i++) {
            _agents[i].DrawGizmos(debugFlags);
        }
    }

    /// <summary>
    /// 构造运行时统计文本（用于屏幕上的调试信息）
    /// </summary>
    public string BuildStatsText() {
        return $"agents={_agents.Count} obstacles={_obstacles.Count} walls={_walls.Count}\n"
               + $"tick={TickCount} dt={DeltaTime * 1000f:F1}ms timeScale={timeScale:F2}{(paused ? " [PAUSED]" : "")}\n"
               + $"neighborQuery={LastNeighborQueryCount} neighborTotal={LastNeighborTotal}"
               + $" cellSpace={(_cellSpace != null ? _cellSpace.CellCount.ToString() : "off")}";
    }

    #endregion
}
}
