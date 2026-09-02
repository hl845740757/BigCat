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
/// 转向力的合成方式
///
/// 注：这是Steering工程实现中最容易踩坑的地方 —— 多个行为同时输出力时，
/// 朴素的加权求和会让相互冲突的行为彼此抵消（例如避障力被Cohesion力抵消，直接撞墙）。
/// </summary>
public enum ESteeringSumMode
{
    /// <summary>
    /// 加权求和：全部行为的力按权重累加，最后统一截断到maxForce
    ///
    /// 优点：简单、连续、无跳变；
    /// 缺点：1.需要为所有行为调权重，且权重之间强耦合；
    ///       2.冲突的行为会互相抵消（关键缺陷）；
    ///       3.每帧都要计算所有行为，开销最大。
    /// </summary>
    WeightedSum = 0,

    /// <summary>
    /// 优先级截断：按注册顺序（即优先级）依次累加，力预算（maxForce）用尽即停止，后续行为不再计算
    ///
    /// 这是Reynolds推荐、也是实践中最常用的方式：
    /// 保证高优先级行为（如避障）永远能拿到它需要的力，同时省下低优先级行为的计算开销。
    /// 缺点：优先级边界上可能出现力的突变（行为"被挤掉"的瞬间）。
    /// </summary>
    Prioritized = 1,

    /// <summary>
    /// 优先级抽样（Dithering）：按优先级顺序遍历，每个行为以自身的概率被抽中；
    /// 一旦某个行为被抽中且产生了非零力，立即返回该力，不再考虑其它行为
    ///
    /// 优点：单帧只计算极少数行为，是三者中开销最低的，适合大规模群体；
    /// 缺点：运动会有轻微抖动（靠帧间平均得到近似正确的行为），且概率也需要调参。
    /// 注：抽中后力会除以概率做补偿，以保证长期期望与加权求和一致。
    /// </summary>
    PrioritizedDithering = 2,
}
}
