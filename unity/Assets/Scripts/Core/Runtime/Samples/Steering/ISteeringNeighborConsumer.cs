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
/// 标记接口：该行为需要邻居列表（<see cref="SteeringAgent.Neighbors"/>）
///
/// 注：<see cref="SteeringManager"/>据此决定是否为某个Agent执行邻居查询 ——
/// 邻居查询是Steering里最重的一步（O(n²)或空间划分），不需要就不做。
/// 三个Boids行为（Separation/Alignment/Cohesion）共享同一份查询结果，避免重复查询。
/// </summary>
public interface ISteeringNeighborConsumer
{
}
}
