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
/// 三维均匀网格空间划分（Cell Space Partition）
///
/// 用于加速邻居查询：Boids的三个行为都需要"半径R内的所有Agent"，
/// 暴力实现是O(n²)，在几百个Agent时就会成为主要开销。
///
/// 注：
/// 1.只适用于Agent分布相对均匀、且活动范围有界的场景（正是群体演示的典型情况）。
///   分布高度不均时应换成松散四叉/八叉树。
/// 2.越界的Agent会被钳制到边缘单元，因此不会漏掉，但会让边缘单元变拥挤。
/// 3.单元边长应略大于最大查询半径 —— 单元太小则要遍历大量单元，太大则退化为暴力。
/// </summary>
public class SteeringCellSpace
{
    /// <summary>
    /// 覆盖范围
    /// </summary>
    private readonly Bounds _bounds;
    /// <summary>
    /// 各轴单元数
    /// </summary>
    private readonly int _countX;
    private readonly int _countY;
    private readonly int _countZ;
    /// <summary>
    /// 各轴"每单位长度对应的单元数"（用乘法代替除法）
    /// </summary>
    private readonly Vector3 _cellsPerUnit;
    /// <summary>
    /// 单元数组，下标 = x + y * countX + z * countX * countY
    /// </summary>
    private readonly List<SteeringAgent>[] _cells;

    /// <summary>
    /// 上次查询遍历的单元数（性能观测用）
    /// </summary>
    public int LastVisitedCellCount { get; private set; }

    public SteeringCellSpace(Bounds bounds, int countX, int countY, int countZ) {
        _bounds = bounds;
        _countX = Mathf.Max(1, countX);
        _countY = Mathf.Max(1, countY);
        _countZ = Mathf.Max(1, countZ);

        Vector3 size = bounds.size;
        _cellsPerUnit = new Vector3(
            _countX / Mathf.Max(size.x, SteeringUtil.Epsilon)
            , _countY / Mathf.Max(size.y, SteeringUtil.Epsilon)
            , _countZ / Mathf.Max(size.z, SteeringUtil.Epsilon));

        _cells = new List<SteeringAgent>[_countX * _countY * _countZ];
        for (int i = 0; i < _cells.Length; i++) {
            _cells[i] = new List<SteeringAgent>(4);
        }
    }

    /// <summary>
    /// 覆盖范围
    /// </summary>
    public Bounds Bounds => _bounds;
    /// <summary>
    /// 单元总数
    /// </summary>
    public int CellCount => _cells.Length;
    /// <summary>
    /// 单元尺寸
    /// </summary>
    public Vector3 CellSize => new Vector3(_bounds.size.x / _countX
        , _bounds.size.y / _countY
        , _bounds.size.z / _countZ);

    /// <summary>
    /// 重建索引（每帧调用）
    /// </summary>
    public void Rebuild(List<SteeringAgent> agents) {
        for (int i = 0; i < _cells.Length; i++) {
            _cells[i].Clear();
        }
        for (int i = 0; i < agents.Count; i++) {
            SteeringAgent agent = agents[i];
            if (!agent.enabled) continue;
            CellCoord(agent.position, out int x, out int y, out int z);
            _cells[CellIndex(x, y, z)].Add(agent);
        }
    }

    /// <summary>
    /// 查询球形范围内的Agent
    /// </summary>
    /// <param name="center">查询中心</param>
    /// <param name="radius">查询半径</param>
    /// <param name="exclude">要排除的Agent（通常是查询者自己）</param>
    /// <param name="result">输出列表，会被先清空</param>
    public void Query(Vector3 center, float radius, SteeringAgent exclude, List<SteeringAgent> result) {
        result.Clear();
        LastVisitedCellCount = 0;
        if (radius <= 0f) return;

        Vector3 extent = new Vector3(radius, radius, radius);
        CellCoord(center - extent, out int minX, out int minY, out int minZ);
        CellCoord(center + extent, out int maxX, out int maxY, out int maxZ);

        float sqrRadius = radius * radius;
        for (int z = minZ; z <= maxZ; z++) {
            for (int y = minY; y <= maxY; y++) {
                // 同一行的单元在数组中连续，先算行首下标
                int rowBase = y * _countX + z * _countX * _countY;
                for (int x = minX; x <= maxX; x++) {
                    List<SteeringAgent> cell = _cells[rowBase + x];
                    LastVisitedCellCount++;
                    for (int i = 0; i < cell.Count; i++) {
                        SteeringAgent agent = cell[i];
                        if (agent == exclude) continue;
                        if ((agent.position - center).sqrMagnitude <= sqrRadius) {
                            result.Add(agent);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 世界坐标转单元坐标（越界会被钳制到边缘）
    /// </summary>
    private void CellCoord(Vector3 position, out int x, out int y, out int z) {
        Vector3 local = position - _bounds.min;
        x = Mathf.Clamp((int)(local.x * _cellsPerUnit.x), 0, _countX - 1);
        y = Mathf.Clamp((int)(local.y * _cellsPerUnit.y), 0, _countY - 1);
        z = Mathf.Clamp((int)(local.z * _cellsPerUnit.z), 0, _countZ - 1);
    }

    private int CellIndex(int x, int y, int z) => x + y * _countX + z * _countX * _countY;

    /// <summary>
    /// 绘制非空单元（画全部单元在3D下会糊成一片）
    /// </summary>
    public void DrawGizmos() {
        Vector3 cellSize = CellSize;
        Vector3 half = cellSize * 0.5f;
        Gizmos.color = SteeringGizmos.ColorGrid;
        for (int z = 0; z < _countZ; z++) {
            for (int y = 0; y < _countY; y++) {
                for (int x = 0; x < _countX; x++) {
                    if (_cells[CellIndex(x, y, z)].Count == 0) continue;
                    Vector3 min = _bounds.min + new Vector3(x * cellSize.x, y * cellSize.y, z * cellSize.z);
                    Gizmos.DrawWireCube(min + half, cellSize);
                }
            }
        }
    }
}
}
