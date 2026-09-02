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
/// Steering算法演示驱动器
///
/// 用法：新建空场景 -> 建一个空GameObject -> 挂上本组件 -> 进入Play模式。
/// 场景内容全部由代码程序化生成，无需摆放任何预制体。
///
/// 1.运行时左上角有GUI面板，可切换演示场景、切换力的合成模式、暂停与调速；
/// 2.调试图元通过<see cref="debugFlags"/>控制，需要在Scene视图开启Gizmos才能看到
///   （Game视图右上角的Gizmos按钮也可开启）；
/// 3.所有算法逻辑都在<see cref="SteeringManager"/>及各<see cref="SteeringBehaviour"/>里，
///   本类只负责搭场景与驱动心跳，不含任何算法。
/// </summary>
[AddComponentMenu("BigCat/Samples/Steering Sample")]
public class SteeringSample : MonoBehaviour
{
    #region Inspector配置

    [Header("场景")]
    [Tooltip("演示场景；修改后自动重建")]
    public ESteeringSampleScene scene = ESteeringSampleScene.Flocking;
    [Tooltip("随机种子；相同种子的运行完全可复现")]
    public int seed = 20250902;
    [Tooltip("活动区域尺寸")]
    public Vector3 areaSize = new Vector3(60f, 26f, 60f);
    [Tooltip("Flocking场景的个体数量")]
    [Range(2, 400)]
    public int flockCount = 60;
    [Tooltip("障碍数量")]
    [Range(0, 60)]
    public int obstacleCount = 12;

    [Header("运行")]
    public bool paused;
    [Tooltip("时间缩放；调小可放慢观察转向过程")]
    [Range(0f, 3f)]
    public float timeScale = 1f;
    [Tooltip("固定步长；大于0时启用定步长积分，使结果与帧率无关")]
    public float fixedDeltaTime;
    [Tooltip("两阶段积分：先让所有Agent算力再统一提交，消除更新顺序的影响")]
    public bool deferredIntegration = true;
    [Tooltip("力的合成模式；SumModeCompare场景会忽略该设置")]
    public ESteeringSumMode sumMode = ESteeringSumMode.Prioritized;

    [Header("调试")]
    public ESteeringDebugFlags debugFlags = ESteeringDebugFlags.Default;
    [Tooltip("速度箭头长度缩放")]
    public float gizmoVelocityScale = 0.4f;
    [Tooltip("力箭头长度缩放")]
    public float gizmoForceScale = 0.12f;
    [Tooltip("轨迹采样点数；0表示关闭")]
    public int trailCapacity = 90;
    [Tooltip("是否生成可见的GameObject（关闭后只剩Gizmos）")]
    public bool createVisuals = true;
    [Tooltip("是否显示运行时GUI面板")]
    public bool showGui = true;

    #endregion

    #region 配色

    private static readonly Color ColorPrimary = new Color(0.32f, 0.66f, 1.00f);
    private static readonly Color ColorSecondary = new Color(1.00f, 0.58f, 0.28f);
    private static readonly Color ColorTertiary = new Color(0.48f, 0.90f, 0.52f);
    private static readonly Color ColorHunter = new Color(1.00f, 0.30f, 0.35f);
    private static readonly Color ColorLeader = new Color(1.00f, 0.88f, 0.32f);
    private static readonly Color ColorObstacleVisual = new Color(0.52f, 0.52f, 0.58f);
    private static readonly Color ColorMarker = new Color(0.35f, 1.00f, 0.45f);

    #endregion

    #region 运行时状态

    private SteeringManager _manager;
    /// <summary>
    /// 程序化生成的表现对象的父节点
    /// </summary>
    private Transform _visualRoot;
    /// <summary>
    /// 材质缓存（按颜色）；需要在销毁时释放
    /// </summary>
    private readonly Dictionary<Color, Material> _materialCache = new Dictionary<Color, Material>(8);
    /// <summary>
    /// 当前场景是否自行管理各Agent的sumMode（SumModeCompare场景）
    /// </summary>
    private bool _sceneOwnsSumMode;

    /// <summary>
    /// 已构建的配置快照，用于判断是否需要重建
    /// </summary>
    private ESteeringSampleScene _builtScene;
    private int _builtSeed;
    private int _builtFlockCount;
    private int _builtObstacleCount;
    private Vector3 _builtAreaSize;
    private bool _builtVisuals;
    private bool _rebuildRequested;

    /// <summary>
    /// GUI用的枚举名缓存（避免每帧反射）
    /// </summary>
    private static string[] _sceneNames;
    private static string[] _sumModeNames;

    #endregion

    /// <summary>
    /// 管理器（供外部调试脚本取用）
    /// </summary>
    public SteeringManager Manager => _manager;

    #region 生命周期

    private void OnEnable() {
        Rebuild();
    }

    private void OnDisable() {
        Teardown();
    }

    private void Update() {
        if (_rebuildRequested || NeedRebuild()) {
            Rebuild();
        }
        SyncConfig();
        _manager.Update(Time.deltaTime);
    }

    /// <summary>
    /// Inspector改动时只打标记 —— Unity不允许在OnValidate里创建/销毁对象
    /// </summary>
    private void OnValidate() {
        if (areaSize.x < 5f) areaSize.x = 5f;
        if (areaSize.y < 5f) areaSize.y = 5f;
        if (areaSize.z < 5f) areaSize.z = 5f;
        if (Application.isPlaying) _rebuildRequested = true;
    }

    private void OnDrawGizmos() {
        // 编辑器非播放状态下_manager为null，此时只画活动区域方便预先规划
        if (_manager == null) {
            Gizmos.color = SteeringGizmos.ColorBounds;
            Gizmos.DrawWireCube(transform.position, areaSize);
            return;
        }
        _manager.DrawGizmos();
    }

    #endregion

    #region 构建与销毁

    /// <summary>
    /// 重建当前场景
    /// </summary>
    [ContextMenu("Rebuild Scene")]
    public void Rebuild() {
        Teardown();

        _manager = new SteeringManager(seed) {
            activeBounds = new Bounds(transform.position, areaSize),
        };
        SteeringManager.Inst = _manager;
        _sceneOwnsSumMode = false;

        if (createVisuals) {
            GameObject rootGo = new GameObject("[SteeringVisuals]");
            rootGo.transform.SetParent(transform, false);
            _visualRoot = rootGo.transform;
        }

        switch (scene) {
            case ESteeringSampleScene.SeekVsArrive:
                BuildSeekVsArrive();
                break;
            case ESteeringSampleScene.PursuitAndEvade:
                BuildPursuitAndEvade();
                break;
            case ESteeringSampleScene.Wander:
                BuildWander();
                break;
            case ESteeringSampleScene.ObstacleAvoidance:
                BuildObstacleAvoidance();
                break;
            case ESteeringSampleScene.WallAvoidance:
                BuildWallAvoidance();
                break;
            case ESteeringSampleScene.PathFollow:
                BuildPathFollow();
                break;
            case ESteeringSampleScene.Flocking:
                BuildFlocking();
                break;
            case ESteeringSampleScene.Formation:
                BuildFormation();
                break;
            case ESteeringSampleScene.InterposeAndHide:
                BuildInterposeAndHide();
                break;
            case ESteeringSampleScene.SumModeCompare:
                BuildSumModeCompare();
                break;
            default:
                Debug.LogWarning($"unknown steering sample scene: {scene}");
                break;
        }

        SyncConfig();
        // 记录快照
        _builtScene = scene;
        _builtSeed = seed;
        _builtFlockCount = flockCount;
        _builtObstacleCount = obstacleCount;
        _builtAreaSize = areaSize;
        _builtVisuals = createVisuals;
        _rebuildRequested = false;
    }

    private void Teardown() {
        if (_visualRoot != null) {
            Destroy(_visualRoot.gameObject);
            _visualRoot = null;
        }
        foreach (Material material in _materialCache.Values) {
            if (material != null) Destroy(material);
        }
        _materialCache.Clear();

        if (ReferenceEquals(SteeringManager.Inst, _manager)) {
            SteeringManager.Inst = null;
        }
        _manager = null;
    }

    /// <summary>
    /// 只有影响场景结构的配置变化才需要重建；调速、调试开关等走<see cref="SyncConfig"/>
    /// </summary>
    private bool NeedRebuild() {
        return _manager == null
               || _builtScene != scene
               || _builtSeed != seed
               || _builtFlockCount != flockCount
               || _builtObstacleCount != obstacleCount
               || _builtAreaSize != areaSize
               || _builtVisuals != createVisuals;
    }

    /// <summary>
    /// 把Inspector上的运行期配置同步到管理器
    /// </summary>
    private void SyncConfig() {
        _manager.paused = paused;
        _manager.timeScale = timeScale;
        _manager.fixedDeltaTime = fixedDeltaTime;
        _manager.deferredIntegration = deferredIntegration;
        _manager.debugFlags = debugFlags;
        _manager.gizmoVelocityScale = gizmoVelocityScale;
        _manager.gizmoForceScale = gizmoForceScale;
        if (!_sceneOwnsSumMode) {
            _manager.SetSumMode(sumMode);
        }
    }

    #endregion

    #region 场景：基础行为

    private void BuildSeekVsArrive() {
        Bounds bounds = _manager.activeBounds;
        float z = bounds.min.z + 4f;

        // 左：Seek —— 会在目标处过冲、掉头、再过冲，形成振荡
        Vector3 seekTarget = new Vector3(bounds.center.x - 12f, bounds.center.y, bounds.center.z);
        CreateMarkerVisual("seek-target", seekTarget, 0.8f);
        SteeringAgent seeker = NewAgent("seek", new Vector3(seekTarget.x, seekTarget.y, z), ColorPrimary);
        seeker.AddBehaviour(new SeekBehaviour(seekTarget));

        // 右：Arrive —— 进入制动圈后减速，最终停住且不过冲
        Vector3 arriveTarget = new Vector3(bounds.center.x + 12f, bounds.center.y, bounds.center.z);
        CreateMarkerVisual("arrive-target", arriveTarget, 0.8f);
        SteeringAgent arriver = NewAgent("arrive", new Vector3(arriveTarget.x, arriveTarget.y, z), ColorSecondary);
        arriver.AddBehaviour(new ArriveBehaviour(arriveTarget) {
            deceleration = ESteeringDeceleration.Normal,
            // 制动时间 = Normal(2) * 2 = 4秒 ≈ 4*mass，实测过冲为0；
            // 用Buckland原书的0.3会过冲约2个单位，反而看不出与Seek的差异
            tweaker = 2f,
        });
    }

    private void BuildPursuitAndEvade() {
        Bounds bounds = _manager.activeBounds;

        SteeringAgent evader = NewAgent("evader", RandomFreePosition(), ColorSecondary);
        evader.maxSpeed = 7f;
        evader.banking = 0.5f;

        SteeringAgent pursuer = NewAgent("pursuer", RandomFreePosition(), ColorHunter, 0.6f);
        // 追捕者必须略快，否则永远只能吊在后面
        pursuer.maxSpeed = 8f;
        pursuer.banking = 0.5f;

        // 优先级：边界 > 逃逸 > 游走
        evader.AddBehaviour(new BoundsContainmentBehaviour(bounds, 5f) { weight = 4f });
        evader.AddBehaviour(new EvadeBehaviour(pursuer) { weight = 2f, threatDistance = 22f });
        evader.AddBehaviour(new WanderBehaviour { weight = 1f, wanderJitter = 50f });

        pursuer.AddBehaviour(new BoundsContainmentBehaviour(bounds, 5f) { weight = 4f });
        pursuer.AddBehaviour(new PursuitBehaviour(evader) { weight = 1f });
    }

    private void BuildWander() {
        Bounds bounds = _manager.activeBounds;

        // 三种参数组合并排对比
        SteeringAgent horizontal = NewAgent("wander-2d", RandomFreePosition(), ColorPrimary);
        horizontal.AddBehaviour(new BoundsContainmentBehaviour(bounds, 5f) { weight = 4f });
        horizontal.AddBehaviour(new WanderBehaviour { weight = 1f, wanderJitter = 40f, verticalScale = 0f });

        SteeringAgent smooth = NewAgent("wander-3d-smooth", RandomFreePosition(), ColorSecondary);
        smooth.banking = 0.8f;
        smooth.AddBehaviour(new BoundsContainmentBehaviour(bounds, 5f) { weight = 4f });
        smooth.AddBehaviour(new WanderBehaviour {
            weight = 1f,
            wanderJitter = 25f,
            wanderDistance = 4f, // 球心更远 -> 张角更小 -> 转向更平缓
        });

        SteeringAgent jittery = NewAgent("wander-3d-jittery", RandomFreePosition(), ColorTertiary);
        jittery.banking = 0.8f;
        jittery.AddBehaviour(new BoundsContainmentBehaviour(bounds, 5f) { weight = 4f });
        jittery.AddBehaviour(new WanderBehaviour {
            weight = 1f,
            wanderJitter = 140f,
            wanderDistance = 2f,
        });
    }

    #endregion

    #region 场景：避障

    private void BuildObstacleAvoidance() {
        Bounds bounds = _manager.activeBounds;
        CreateObstacles(obstacleCount);

        for (int i = 0; i < 4; i++) {
            SteeringAgent agent = NewAgent($"avoider{i}", RandomFreePosition(), ColorPrimary);
            agent.maxSpeed = 7f;
            agent.banking = 0.6f;
            // 避障优先于一切：Prioritized模式下它总能先拿到力预算
            agent.AddBehaviour(new ObstacleAvoidanceBehaviour { weight = 10f, detectionLength = 6f });
            agent.AddBehaviour(new BoundsContainmentBehaviour(bounds, 5f) { weight = 4f });
            agent.AddBehaviour(new WanderBehaviour { weight = 1f, wanderJitter = 40f });
        }
    }

    private void BuildWallAvoidance() {
        Bounds bounds = _manager.activeBounds;
        // 墙面略微内缩，外面再套一层BoundsContainment兜底：
        // 羽须探测本质上是采样，斜着高速撞角落时有漏检的可能
        Bounds room = new Bounds(bounds.center, bounds.size * 0.85f);
        _manager.AddRoomWalls(room);

        for (int i = 0; i < 4; i++) {
            SteeringAgent agent = NewAgent($"walker{i}", RandomFreePosition(0.6f), ColorTertiary);
            agent.maxSpeed = 7f;
            agent.banking = 0.6f;
            agent.AddBehaviour(new WallAvoidanceBehaviour { weight = 8f, feelerLength = 6f });
            agent.AddBehaviour(new BoundsContainmentBehaviour(bounds, 1.5f) { weight = 6f });
            agent.AddBehaviour(new WanderBehaviour { weight = 1f, wanderJitter = 60f });
        }
    }

    #endregion

    #region 场景：路径与编队

    private void BuildPathFollow() {
        Bounds bounds = _manager.activeBounds;
        CreateObstacles(Mathf.Min(obstacleCount, 6));

        float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.7f;
        SteeringPath template = SteeringPath.CreateCircle(bounds.center, radius, 12, Vector3.up
            , bounds.extents.y * 0.4f);

        for (int i = 0; i < 3; i++) {
            // 每个Agent必须持有独立的路径实例（航点下标是路径的内部状态）
            SteeringPath path = template.Clone();
            for (int j = 0; j < i * 4; j++) {
                path.SetNext(); // 错开起始航点
            }
            SteeringAgent agent = NewAgent($"follower{i}", path.CurWaypoint, ColorPrimary);
            agent.maxSpeed = 8f;
            agent.banking = 0.9f;
            agent.AddBehaviour(new ObstacleAvoidanceBehaviour { weight = 10f });
            agent.AddBehaviour(new PathFollowBehaviour(path) { weight = 1f, waypointSeekDistance = 3f });
        }
    }

    private void BuildFormation() {
        Bounds bounds = _manager.activeBounds;
        float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.65f;
        SteeringPath path = SteeringPath.CreateCircle(bounds.center, radius, 14, Vector3.up
            , bounds.extents.y * 0.3f);

        SteeringAgent leader = NewAgent("leader", path.CurWaypoint, ColorLeader, 0.7f);
        leader.maxSpeed = 6f;
        leader.banking = 0.9f;
        leader.AddBehaviour(new PathFollowBehaviour(path) { weight = 1f, waypointSeekDistance = 3f });

        // V字阵位（leader局部空间：+X=右, +Y=上, +Z=前）
        Vector3[] offsets = {
            new Vector3(-2.6f, 0f, -2.6f),
            new Vector3(2.6f, 0f, -2.6f),
            new Vector3(-5.2f, 0f, -5.2f),
            new Vector3(5.2f, 0f, -5.2f),
            new Vector3(0f, 2.2f, -5.2f),
        };
        for (int i = 0; i < offsets.Length; i++) {
            SteeringAgent wing = NewAgent($"wing{i}", leader.LocalToWorld(offsets[i]), ColorPrimary, 0.45f);
            // 僚机必须比长机快，否则永远追不上阵位
            wing.maxSpeed = 9f;
            wing.banking = 0.9f;
            wing.neighborRadius = 3.5f;
            // 分离优先于保持阵位：阵位重叠时先别撞上
            wing.AddBehaviour(new SeparationBehaviour { weight = 10f, separationRadius = 2.2f });
            wing.AddBehaviour(new OffsetPursuitBehaviour(leader, offsets[i]) { weight = 1f });
        }
    }

    #endregion

    #region 场景：群体

    private void BuildFlocking() {
        Bounds bounds = _manager.activeBounds;
        // 单元边长取略大于感知半径；这里感知半径6，区域60/8=7.5
        _manager.EnableCellSpace(bounds, 8, 4, 8);
        CreateObstacles(Mathf.Min(obstacleCount, 10));

        for (int i = 0; i < flockCount; i++) {
            SteeringAgent agent = NewAgent($"boid{i}", RandomFreePosition(), ColorPrimary, 0.4f);
            // 速度略有差异，群体才不会像刚体一样整块平移
            agent.maxSpeed = SteeringUtil.RandomRange(_manager.Random, 5.5f, 7.5f);
            agent.maxForce = 26f;
            agent.neighborRadius = 6f;
            agent.banking = 0.7f;
            agent.damping = 0.2f;
            // 个体太多时轨迹会糊成一片
            agent.EnableTrail(0);

            // 优先级：硬约束（避障/边界） > 群体三法则 > 游走
            agent.AddBehaviour(new ObstacleAvoidanceBehaviour { weight = 10f });
            agent.AddBehaviour(new BoundsContainmentBehaviour(bounds, 6f) { weight = 4f });
            // 注：三个群体行为的weight量纲完全不同 ——
            // Separation输出Σ(1/d)，Alignment输出朝向差(≤2)，Cohesion已归一化(=1)
            agent.AddBehaviour(new SeparationBehaviour { weight = 16f, separationRadius = 3f });
            agent.AddBehaviour(new AlignmentBehaviour { weight = 8f });
            agent.AddBehaviour(new CohesionBehaviour { weight = 5f });
            agent.AddBehaviour(new WanderBehaviour { weight = 1f, wanderJitter = 30f });
        }
    }

    #endregion

    #region 场景：拦截与躲藏

    private void BuildInterposeAndHide() {
        Bounds bounds = _manager.activeBounds;
        CreateObstacles(Mathf.Max(obstacleCount, 6));

        // 两个游走目标 + 一个拦截者
        SteeringAgent targetA = NewAgent("targetA", RandomFreePosition(), ColorPrimary);
        SteeringAgent targetB = NewAgent("targetB", RandomFreePosition(), ColorSecondary);
        foreach (SteeringAgent target in new[] { targetA, targetB }) {
            target.maxSpeed = 6f;
            target.AddBehaviour(new BoundsContainmentBehaviour(bounds, 5f) { weight = 4f });
            target.AddBehaviour(new WanderBehaviour { weight = 1f, wanderJitter = 45f });
        }

        SteeringAgent interposer = NewAgent("interposer", RandomFreePosition(), ColorTertiary, 0.6f);
        interposer.maxSpeed = 9f; // 要抢到中点必须更快
        interposer.AddBehaviour(new BoundsContainmentBehaviour(bounds, 5f) { weight = 4f });
        interposer.AddBehaviour(new InterposeBehaviour(targetA, targetB) { weight = 1f });

        // 猎人与猎物：先建对象再互相引用
        SteeringAgent prey = NewAgent("prey", RandomFreePosition(), ColorMarker, 0.45f);
        SteeringAgent hunter = NewAgent("hunter", RandomFreePosition(), ColorHunter, 0.6f);

        prey.maxSpeed = 7f; // 略快于猎人，否则躲藏毫无意义
        prey.AddBehaviour(new ObstacleAvoidanceBehaviour { weight = 10f });
        prey.AddBehaviour(new BoundsContainmentBehaviour(bounds, 5f) { weight = 4f });
        prey.AddBehaviour(new HideBehaviour(hunter) { weight = 2f, maxHideDistance = 30f });

        hunter.maxSpeed = 6.5f;
        hunter.AddBehaviour(new ObstacleAvoidanceBehaviour { weight = 10f });
        hunter.AddBehaviour(new BoundsContainmentBehaviour(bounds, 5f) { weight = 4f });
        hunter.AddBehaviour(new PursuitBehaviour(prey) { weight = 1f });
    }

    #endregion

    #region 场景：合成模式对比

    private void BuildSumModeCompare() {
        Bounds bounds = _manager.activeBounds;
        _sceneOwnsSumMode = true; // 各组Agent自带sumMode，不受Inspector的全局设置影响
        CreateObstacles(obstacleCount);

        ESteeringSumMode[] modes = {
            ESteeringSumMode.WeightedSum,
            ESteeringSumMode.Prioritized,
            ESteeringSumMode.PrioritizedDithering,
        };
        Color[] colors = { ColorPrimary, ColorSecondary, ColorTertiary };

        for (int m = 0; m < modes.Length; m++) {
            for (int i = 0; i < 3; i++) {
                SteeringAgent agent = NewAgent($"{modes[m]}#{i}", RandomFreePosition(), colors[m]);
                agent.sumMode = modes[m];
                agent.maxSpeed = 7f;
                agent.banking = 0.6f;
                // 三组配置完全一致，差异只来自合成模式：
                // WeightedSum下避障力会被Wander稀释，能看到明显的贴边/擦碰
                agent.AddBehaviour(new ObstacleAvoidanceBehaviour { weight = 10f, detectionLength = 6f });
                agent.AddBehaviour(new BoundsContainmentBehaviour(bounds, 5f) { weight = 4f });
                agent.AddBehaviour(new WanderBehaviour { weight = 1f, wanderJitter = 40f });
            }
        }
    }

    #endregion

    #region 构建辅助

    /// <summary>
    /// 创建Agent（含表现对象、初始朝向与轨迹）
    /// </summary>
    private SteeringAgent NewAgent(string name, Vector3 position, Color color, float radius = 0.5f) {
        Transform visual = CreateAgentVisual(name, color, radius, position);
        SteeringAgent agent = _manager.CreateAgent(name, visual);
        agent.radius = radius;
        agent.ResetMotion(position, RandomHeading());
        agent.EnableTrail(trailCapacity);
        return agent;
    }

    /// <summary>
    /// 生成一个不与任何已有障碍重叠的随机位置
    /// </summary>
    /// <param name="shrink">相对活动区域的内缩比例</param>
    private Vector3 RandomFreePosition(float shrink = 0.8f) {
        Bounds bounds = _manager.activeBounds;
        Bounds inner = new Bounds(bounds.center, bounds.size * shrink);
        IReadOnlyList<SteeringObstacle> obstacles = _manager.Obstacles;

        // 有限次重试；实在找不到就返回最后一次的结果（演示场景不必强求）
        Vector3 position = inner.center;
        for (int attempt = 0; attempt < 24; attempt++) {
            position = SteeringUtil.RandomInBounds(_manager.Random, inner);
            bool blocked = false;
            for (int i = 0; i < obstacles.Count; i++) {
                SteeringObstacle obstacle = obstacles[i];
                float clearance = obstacle.radius + 2f;
                if ((position - obstacle.position).sqrMagnitude < clearance * clearance) {
                    blocked = true;
                    break;
                }
            }
            if (!blocked) return position;
        }
        return position;
    }

    /// <summary>
    /// 随机初始朝向（压低竖直分量，避免一开始就垂直冲天）
    /// </summary>
    private Vector3 RandomHeading() {
        Vector3 direction = SteeringUtil.RandomOnUnitSphere(_manager.Random);
        direction.y *= 0.2f;
        return SteeringUtil.SafeNormalize(direction, Vector3.forward);
    }

    private void CreateObstacles(int count) {
        Bounds bounds = _manager.activeBounds;
        Bounds inner = new Bounds(bounds.center, bounds.size * 0.72f);
        for (int i = 0; i < count; i++) {
            Vector3 position = SteeringUtil.RandomInBounds(_manager.Random, inner);
            float radius = SteeringUtil.RandomRange(_manager.Random, 1.5f, 3.5f);
            _manager.AddObstacle(position, radius, $"obstacle{i}");
            CreateObstacleVisual($"obstacle{i}", position, radius);
        }
    }

    #endregion

    #region 表现对象

    private Transform CreateAgentVisual(string name, Color color, float radius, Vector3 position) {
        if (!createVisuals) return null;

        GameObject go = CreatePrimitive(PrimitiveType.Cube, name, position);
        // 扁长条，长轴朝+Z —— 不看Gizmos也能判断朝向
        go.transform.localScale = new Vector3(radius * 1.2f, radius * 0.8f, radius * 3.2f);
        ApplyColor(go, color);
        return go.transform;
    }

    private void CreateObstacleVisual(string name, Vector3 position, float radius) {
        if (!createVisuals) return;
        GameObject go = CreatePrimitive(PrimitiveType.Sphere, name, position);
        go.transform.localScale = Vector3.one * (radius * 2f);
        ApplyColor(go, ColorObstacleVisual);
    }

    private void CreateMarkerVisual(string name, Vector3 position, float size) {
        if (!createVisuals) return;
        GameObject go = CreatePrimitive(PrimitiveType.Sphere, name, position);
        go.transform.localScale = Vector3.one * size;
        ApplyColor(go, ColorMarker);
    }

    private GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 position) {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(_visualRoot, true);
        go.transform.position = position;
        // Steering不走物理，碰撞体只会带来额外开销
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        return go;
    }

    private void ApplyColor(GameObject go, Color color) {
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = GetMaterial(color);
    }

    private Material GetMaterial(Color color) {
        if (_materialCache.TryGetValue(color, out Material cached) && cached != null) {
            return cached;
        }
        // URP/内置管线的shader名不同，逐个兜底
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("Standard")
                        ?? Shader.Find("Sprites/Default");
        Material material = new Material(shader) {
            name = $"steering-sample-{ColorUtility.ToHtmlStringRGB(color)}",
            color = color,
        };
        _materialCache[color] = material;
        return material;
    }

    #endregion

    #region 运行时GUI

    private void OnGUI() {
        if (!showGui || _manager == null) return;

        _sceneNames ??= Enum.GetNames(typeof(ESteeringSampleScene));
        _sumModeNames ??= Enum.GetNames(typeof(ESteeringSumMode));

        GUILayout.BeginArea(new Rect(10f, 10f, 330f, 470f), GUI.skin.box);
        GUILayout.Label("Steering Sample");

        GUILayout.Space(4f);
        GUILayout.Label("Scene");
        int sceneIndex = Array.IndexOf(_sceneNames, scene.ToString());
        int newSceneIndex = GUILayout.SelectionGrid(sceneIndex, _sceneNames, 2);
        if (newSceneIndex != sceneIndex && newSceneIndex >= 0) {
            scene = (ESteeringSampleScene)Enum.Parse(typeof(ESteeringSampleScene), _sceneNames[newSceneIndex]);
            _rebuildRequested = true;
        }

        GUILayout.Space(4f);
        GUILayout.Label(_sceneOwnsSumMode ? "Sum Mode (overridden by scene)" : "Sum Mode");
        int modeIndex = (int)sumMode;
        int newModeIndex = GUILayout.SelectionGrid(modeIndex, _sumModeNames, 3);
        if (newModeIndex != modeIndex) {
            sumMode = (ESteeringSumMode)newModeIndex;
        }

        GUILayout.Space(4f);
        paused = GUILayout.Toggle(paused, "Paused");
        GUILayout.BeginHorizontal();
        GUILayout.Label($"TimeScale {timeScale:F2}", GUILayout.Width(110f));
        timeScale = GUILayout.HorizontalSlider(timeScale, 0f, 3f);
        GUILayout.EndHorizontal();

        GUILayout.Space(4f);
        if (GUILayout.Button("Rebuild (new seed)")) {
            seed++;
            _rebuildRequested = true;
        }

        GUILayout.Space(6f);
        GUILayout.Label(_manager.BuildStatsText());
        GUILayout.EndArea();
    }

    #endregion
}
}
