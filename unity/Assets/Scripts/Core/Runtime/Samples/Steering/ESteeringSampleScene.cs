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

namespace Wjybxx.BigCat.Samples.Steering
{
/// <summary>
/// Steering演示场景
/// </summary>
public enum ESteeringSampleScene
{
    /// <summary>
    /// Seek与Arrive对比：并排两个Agent追同一目标，
    /// 可直观看到Seek在目标附近的过冲振荡与Arrive的平滑停止
    /// </summary>
    SeekVsArrive = 0,

    /// <summary>
    /// 追捕与逃逸：pursuer预测追击，evader预测逃逸并叠加Wander
    /// </summary>
    PursuitAndEvade = 1,

    /// <summary>
    /// 随机游走：对比不同jitter与竖直自由度的参数组合
    /// </summary>
    Wander = 2,

    /// <summary>
    /// 球形障碍规避：Wander叠加高优先级避障，可观察探测圆柱与规避力
    /// </summary>
    ObstacleAvoidance = 3,

    /// <summary>
    /// 墙面规避：封闭房间内用羽须探测六面墙
    /// </summary>
    WallAvoidance = 4,

    /// <summary>
    /// 路径跟随：带竖直起伏的三维环形路径
    /// </summary>
    PathFollow = 5,

    /// <summary>
    /// 群体行为：Boids三法则 + 避障 + 空间划分加速
    /// </summary>
    Flocking = 6,

    /// <summary>
    /// 编队：长机走路径，僚机用OffsetPursuit保持V字阵位
    /// </summary>
    Formation = 7,

    /// <summary>
    /// 拦截与躲藏：interposer插入两个游走目标之间；prey借障碍躲避hunter
    /// </summary>
    InterposeAndHide = 8,

    /// <summary>
    /// 三种力合成模式对比：三组相同配置的Agent并排跑，
    /// 可看出WeightedSum会让避障力被Wander稀释、Dithering会有轻微抖动
    /// </summary>
    SumModeCompare = 9,
}
}
