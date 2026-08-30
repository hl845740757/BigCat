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

namespace Wjybxx.BigCat.Gameplay
{
/// <summary>
/// GamePlay部分的扩展逻辑
/// </summary>
public static class GExtensions
{
    public static ITimeProvider GetUnscaledFacade(this GTime time) {
        if (time == null) throw new ArgumentNullException(nameof(time));
        return new UnscaledTimeAdapter1(time);
    }

    public static ITimeProvider GetUnscaledFacade(this ITime time) {
        if (time == null) throw new ArgumentNullException(nameof(time));
        if (time is GTime gTime) {
            return new UnscaledTimeAdapter1(gTime);
        }
        return new UnscaledTimeAdapter2(time);
    }

    private class UnscaledTimeAdapter1 : ITimeProvider
    {
        private readonly GTime _time;

        public UnscaledTimeAdapter1(GTime time) {
            _time = time;
        }

        public int FrameCount => _time.FrameCount;
        public double Time => _time.UnscaledTime;
        public double DeltaTime => _time.UnscaledDeltaTime;
    }

    private class UnscaledTimeAdapter2 : ITimeProvider
    {
        private readonly ITime _time;

        public UnscaledTimeAdapter2(ITime time) {
            _time = time;
        }

        public int FrameCount => _time.FrameCount;
        public double Time => _time.UnscaledTime;
        public double DeltaTime => _time.UnscaledDeltaTime;
    }
}
}