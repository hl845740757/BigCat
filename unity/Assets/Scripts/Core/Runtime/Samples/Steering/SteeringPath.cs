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
using Random = System.Random;

namespace Wjybxx.BigCat.Samples.Steering
{
/// <summary>
/// 巡逻路径（供<see cref="PathFollowBehaviour"/>使用）
///
/// 注：路径只维护"当前航点"这一个状态，切换航点由行为层根据到达距离触发。
/// 多个Agent若要各自独立地跟随同一条路线，必须各持有一份<see cref="SteeringPath"/>实例
/// （或用<see cref="Clone"/>），否则会互相干扰航点下标。
/// </summary>
public class SteeringPath
{
    private readonly List<Vector3> _points = new List<Vector3>(8);
    /// <summary>
    /// 是否循环
    /// </summary>
    public bool loop;
    /// <summary>
    /// 当前航点下标
    /// </summary>
    private int _curIndex;

    public SteeringPath() {
    }

    public SteeringPath(IEnumerable<Vector3> points, bool loop = false) {
        _points.AddRange(points);
        this.loop = loop;
    }

    #region 属性

    /// <summary>
    /// 航点数量
    /// </summary>
    public int Count => _points.Count;
    /// <summary>
    /// 是否为空
    /// </summary>
    public bool IsEmpty => _points.Count == 0;
    /// <summary>
    /// 当前航点下标
    /// </summary>
    public int CurIndex => _curIndex;
    /// <summary>
    /// 当前航点；路径为空时返回<see cref="Vector3.zero"/>
    /// </summary>
    public Vector3 CurWaypoint => _points.Count == 0 ? Vector3.zero : _points[_curIndex];
    /// <summary>
    /// 是否已到达最后一个航点（循环路径永远为false）
    /// </summary>
    public bool IsFinished => !loop && _curIndex >= _points.Count - 1;
    /// <summary>
    /// 航点列表（只读）
    /// </summary>
    public IReadOnlyList<Vector3> Points => _points;

    #endregion

    #region 编辑

    public SteeringPath Add(Vector3 point) {
        _points.Add(point);
        return this;
    }

    public void SetPoints(IEnumerable<Vector3> points) {
        _points.Clear();
        _points.AddRange(points);
        _curIndex = 0;
    }

    public void Clear() {
        _points.Clear();
        _curIndex = 0;
    }

    /// <summary>
    /// 前进到下一个航点
    /// </summary>
    /// <returns>是否发生了切换</returns>
    public bool SetNext() {
        if (_points.Count == 0) return false;
        if (_curIndex + 1 < _points.Count) {
            _curIndex++;
            return true;
        }
        if (loop) {
            _curIndex = 0;
            return true;
        }
        return false; // 非循环路径的终点：停在这里
    }

    /// <summary>
    /// 回到起点
    /// </summary>
    public void Reset() => _curIndex = 0;

    /// <summary>
    /// 复制（航点共享值类型，下标独立）
    /// </summary>
    public SteeringPath Clone() => new SteeringPath(_points, loop);

    #endregion

    #region 工厂

    /// <summary>
    /// 生成随机路径
    /// </summary>
    public static SteeringPath CreateRandom(Random rand, int count, Bounds bounds, bool loop = true) {
        SteeringPath path = new SteeringPath { loop = loop };
        for (int i = 0; i < count; i++) {
            path.Add(SteeringUtil.RandomInBounds(rand, bounds));
        }
        return path;
    }

    /// <summary>
    /// 生成环形路径
    /// </summary>
    /// <param name="center">环心</param>
    /// <param name="radius">半径</param>
    /// <param name="count">航点数</param>
    /// <param name="normal">环所在平面的法线</param>
    /// <param name="verticalAmplitude">竖直波动幅度（沿法线方向做正弦起伏，0表示纯平面环）</param>
    public static SteeringPath CreateCircle(Vector3 center, float radius, int count, Vector3 normal
        , float verticalAmplitude = 0f) {
        SteeringPath path = new SteeringPath { loop = true };
        if (count < 3) count = 3;

        Vector3 unitNormal = SteeringUtil.SafeNormalize(normal, Vector3.up);
        Vector3 axisA = SteeringUtil.AnyPerpendicular(unitNormal);
        Vector3 axisB = Vector3.Cross(unitNormal, axisA);
        float step = Mathf.PI * 2f / count;
        for (int i = 0; i < count; i++) {
            float rad = step * i;
            Vector3 point = center + (axisA * Mathf.Cos(rad) + axisB * Mathf.Sin(rad)) * radius;
            if (verticalAmplitude != 0f) {
                // 用2倍频率，使起伏在闭合环上连续
                point += unitNormal * (Mathf.Sin(rad * 2f) * verticalAmplitude);
            }
            path.Add(point);
        }
        return path;
    }

    #endregion

    public void DrawGizmos() {
        if (_points.Count == 0) return;

        Gizmos.color = SteeringGizmos.ColorPath;
        for (int i = 0; i < _points.Count; i++) {
            SteeringGizmos.DrawCross(_points[i], 0.3f);
            if (i + 1 < _points.Count) {
                Gizmos.DrawLine(_points[i], _points[i + 1]);
            } else if (loop && _points.Count > 2) {
                Gizmos.DrawLine(_points[i], _points[0]);
            }
        }
        // 高亮当前航点
        Gizmos.color = SteeringGizmos.ColorTarget;
        SteeringGizmos.DrawCircle(CurWaypoint, Vector3.up, 0.5f, 16);
    }

    public override string ToString() => $"SteeringPath(count={_points.Count}, cur={_curIndex}, loop={loop})";
}
}
