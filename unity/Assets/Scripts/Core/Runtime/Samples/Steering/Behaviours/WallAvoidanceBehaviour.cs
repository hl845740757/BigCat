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
/// WallAvoidance：墙面规避
///
/// 从Agent向前伸出若干根<b>羽须</b>（feeler），与墙面求交；
/// 取穿透最深的那次命中，沿墙面法线把Agent推回来。
///
/// 注：
/// 1.羽须长度必须随速度增长，否则高速时"看到墙"已经来不及转向。
/// 2.3D下需要竖直方向的羽须 —— 只有左中右三根时，天花板和地板是"看不见"的。
/// 3.取<b>穿透最深</b>而非<b>最近命中</b>：夹角墙面同时命中两根羽须时，
///   最深的那面才是当前真正的威胁。
///
/// 该行为应放在行为列表的最前面（优先级最高）。
/// </summary>
public class WallAvoidanceBehaviour : SteeringBehaviour
{
    /// <summary>
    /// 正前方羽须的基础长度
    /// </summary>
    public float feelerLength = 4f;
    /// <summary>
    /// 侧向羽须相对正前方羽须的长度比例
    /// </summary>
    public float sideFeelerRatio = 0.6f;
    /// <summary>
    /// 侧向羽须的张开角度（度）
    /// </summary>
    public float sideFeelerAngle = 45f;
    /// <summary>
    /// 是否启用水平方向的两根羽须
    /// </summary>
    public bool useHorizontalFeelers = true;
    /// <summary>
    /// 是否启用竖直方向的两根羽须（3D必需）
    /// </summary>
    public bool useVerticalFeelers = true;

    /// <summary>
    /// 本帧的羽须终点（世界空间，调试用）
    /// </summary>
    private readonly List<Vector3> _feelerEnds = new List<Vector3>(5);
    /// <summary>
    /// 局部空间的羽须方向与长度缓存（避免每帧重算三角函数）
    /// </summary>
    private readonly List<Vector3> _localFeelers = new List<Vector3>(5);
    /// <summary>
    /// 构建局部羽须时使用的角度（用于检测参数变更）
    /// </summary>
    private float _cachedAngle = float.NaN;
    /// <summary>
    /// 构建局部羽须时使用的配置（用于检测参数变更）
    /// </summary>
    private bool _cachedHorizontal;
    private bool _cachedVertical;
    private float _cachedSideRatio = float.NaN;

    /// <summary>
    /// 上次命中的墙面（调试用）
    /// </summary>
    private SteeringWall _hitWall;
    /// <summary>
    /// 上次的命中点（调试用）
    /// </summary>
    private Vector3 _hitPoint;

    /// <summary>
    /// 上次命中的墙面；null表示未命中
    /// </summary>
    public SteeringWall HitWall => _hitWall;

    public override Vector3 Calculate(float deltaTime) {
        _hitWall = null;
        IReadOnlyList<SteeringWall> walls = Manager.Walls;
        if (walls.Count == 0) return Vector3.zero;

        RebuildLocalFeelersIfNeeded();
        // 羽须长度随速度增长
        float lengthScale = feelerLength * (1f + Agent.SpeedRatio);

        _feelerEnds.Clear();
        float deepestPenetration = 0f;
        for (int i = 0; i < _localFeelers.Count; i++) {
            Vector3 end = Position + Agent.LocalToWorldDirection(_localFeelers[i] * lengthScale);
            _feelerEnds.Add(end);

            for (int j = 0; j < walls.Count; j++) {
                SteeringWall wall = walls[j];
                if (!wall.TryIntersectSegment(Position, end, out Vector3 point, out float _)) continue;
                // 穿透深度 = 羽须末端越过墙面的距离
                float penetration = (end - point).magnitude;
                if (penetration > deepestPenetration) {
                    deepestPenetration = penetration;
                    _hitWall = wall;
                    _hitPoint = point;
                }
            }
        }

        if (_hitWall == null) return Vector3.zero;
        // 沿法线（指向可通行侧）推回，力度与穿透深度成正比
        return _hitWall.normal * deepestPenetration;
    }

    /// <summary>
    /// 构建局部空间的单位羽须方向（仅在配置变化时重建）
    /// </summary>
    private void RebuildLocalFeelersIfNeeded() {
        if (_localFeelers.Count > 0
            && _cachedAngle == sideFeelerAngle
            && _cachedSideRatio == sideFeelerRatio
            && _cachedHorizontal == useHorizontalFeelers
            && _cachedVertical == useVerticalFeelers) {
            return;
        }
        _cachedAngle = sideFeelerAngle;
        _cachedSideRatio = sideFeelerRatio;
        _cachedHorizontal = useHorizontalFeelers;
        _cachedVertical = useVerticalFeelers;

        _localFeelers.Clear();
        _localFeelers.Add(Vector3.forward); // 正前方，全长
        float rad = sideFeelerAngle * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad), cos = Mathf.Cos(rad);
        if (useHorizontalFeelers) {
            _localFeelers.Add(new Vector3(sin, 0f, cos) * sideFeelerRatio);
            _localFeelers.Add(new Vector3(-sin, 0f, cos) * sideFeelerRatio);
        }
        if (useVerticalFeelers) {
            _localFeelers.Add(new Vector3(0f, sin, cos) * sideFeelerRatio);
            _localFeelers.Add(new Vector3(0f, -sin, cos) * sideFeelerRatio);
        }
    }

    public override void DrawGizmos() {
        Gizmos.color = _hitWall != null ? SteeringGizmos.ColorThreat : SteeringGizmos.ColorProbe;
        for (int i = 0; i < _feelerEnds.Count; i++) {
            Gizmos.DrawLine(Position, _feelerEnds[i]);
        }
        if (_hitWall == null) return;

        Gizmos.color = SteeringGizmos.ColorThreat;
        SteeringGizmos.DrawCross(_hitPoint, 0.4f);
        SteeringGizmos.DrawArrow(_hitPoint, _hitPoint + _hitWall.normal * 2f);
    }
}
}
