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
/// Arrive行为的减速档位
///
/// 注：枚举值直接参与计算（<c>speed = distance / (value * tweaker)</c>），
/// 所以值本身有意义，不能随意改动。
/// </summary>
public enum ESteeringDeceleration
{
    /// <summary>
    /// 快速减速（急刹）
    /// </summary>
    Fast = 1,
    /// <summary>
    /// 正常减速
    /// </summary>
    Normal = 2,
    /// <summary>
    /// 缓慢减速（长距离滑行）
    /// </summary>
    Slow = 3,
}
}
