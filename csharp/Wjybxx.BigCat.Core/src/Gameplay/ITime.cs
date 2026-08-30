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

namespace Wjybxx.BigCat.Gameplay
{
/// <summary>
/// 最小时间接口
/// (具体业务尽可能只依赖该接口)
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// 当前帧号
    /// </summary>
    int FrameCount { get; }
    /// <summary>
    /// 当前时间
    /// </summary>
    double Time { get; }
    /// <summary>
    /// 帧间隔
    /// </summary>
    double DeltaTime { get; }
}

/// <summary>
/// 游戏的时间接口
/// </summary>
public interface ITime : ITimeProvider
{
    /// <summary>
    /// 时间缩放系数
    /// </summary>
    double TimeScale { get; }
    /// <summary>
    /// 非缩放时间
    /// </summary>
    double UnscaledTime { get; }
    /// <summary>
    /// 非缩放DeltaTime
    /// </summary>
    double UnscaledDeltaTime { get; }
}
}