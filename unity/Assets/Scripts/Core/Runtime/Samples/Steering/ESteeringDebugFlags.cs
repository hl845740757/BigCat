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

namespace Wjybxx.BigCat.Samples.Steering
{
/// <summary>
/// Steering调试绘制开关
///
/// 注：Steering调参高度依赖可视化 —— 光看运动结果几乎无法判断是权重不对、
/// 还是探测范围不对、还是被高优先级行为挤掉了力。所以这里把每类图元都做成独立开关。
/// </summary>
[Flags]
public enum ESteeringDebugFlags
{
    None = 0,

    /// <summary>
    /// Agent本体（包围球）
    /// </summary>
    Agent = 1 << 0,
    /// <summary>
    /// 速度向量（青色箭头）
    /// </summary>
    Velocity = 1 << 1,
    /// <summary>
    /// 局部坐标系（heading红/up绿/side蓝）
    /// </summary>
    Axis = 1 << 2,
    /// <summary>
    /// 本帧合成的转向力（黄色箭头）
    /// </summary>
    Force = 1 << 3,
    /// <summary>
    /// 各行为自身的调试图元（Wander球、避障探测盒、羽须……）
    /// </summary>
    Behaviour = 1 << 4,
    /// <summary>
    /// 感知半径与邻居连线
    /// </summary>
    Neighbor = 1 << 5,
    /// <summary>
    /// 障碍物
    /// </summary>
    Obstacle = 1 << 6,
    /// <summary>
    /// 墙面
    /// </summary>
    Wall = 1 << 7,
    /// <summary>
    /// 路径
    /// </summary>
    Path = 1 << 8,
    /// <summary>
    /// 空间划分网格（仅绘制非空单元）
    /// </summary>
    CellSpace = 1 << 9,
    /// <summary>
    /// 运动轨迹
    /// </summary>
    Trail = 1 << 10,
    /// <summary>
    /// 文字标签（仅Editor下有效）
    /// </summary>
    Label = 1 << 11,
    /// <summary>
    /// 活动区域边界
    /// </summary>
    Bounds = 1 << 12,

    /// <summary>
    /// 常用组合：足够看清单个Agent的决策过程
    /// </summary>
    Default = Agent | Velocity | Force | Behaviour | Obstacle | Wall | Path | Trail | Bounds,
    /// <summary>
    /// 全部
    /// </summary>
    All = Agent | Velocity | Axis | Force | Behaviour | Neighbor | Obstacle
          | Wall | Path | CellSpace | Trail | Label | Bounds,
}
}
