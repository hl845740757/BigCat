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
using System.Runtime.CompilerServices;

namespace Wjybxx.BigCat.Gameplay
{
/// <summary>
/// World模拟用的计时器
/// </summary>
public sealed class GTime : IReadonlyTime
{
    private double timeScale = 1;

    private int frameCount;
    private double time;
    private double deltaTime;
    private double unscaledTime;
    private double unscaledDeltaTime;

    private int fixedFrameCount; // 通常用于debug
    private double fixedTime;
    private double fixedDeltaTime; // 运行时可能变化
    private double fixedUnscaledTime;
    private double fixedUnscaledDeltaTime;

    public GTime() {
    }

    public void Restart() {
        this.timeScale = 1f;

        this.frameCount = 0; // 初始帧0
        this.time = 0;
        this.deltaTime = 0;
        this.unscaledTime = 0;
        this.unscaledDeltaTime = 0;

        this.fixedFrameCount = 0; // 初始帧0
        this.fixedTime = 0;
        this.fixedDeltaTime = 0;
        this.fixedUnscaledTime = 0;
        this.fixedUnscaledDeltaTime = 0;
    }

    public void Update(double unscaledDeltaTime) {
        double scaledDeltaTime = (unscaledDeltaTime * timeScale);
        this.frameCount++;
        this.time += scaledDeltaTime;
        this.deltaTime = scaledDeltaTime;
        this.unscaledTime += unscaledDeltaTime;
        this.unscaledDeltaTime = unscaledDeltaTime;
    }

    public void FixedUpdate(double unscaledDeltaTime) {
        double scaledDeltaTime = (unscaledDeltaTime * timeScale);
        this.fixedFrameCount++;
        this.fixedTime += scaledDeltaTime;
        this.fixedDeltaTime = scaledDeltaTime;
        this.fixedUnscaledTime += unscaledDeltaTime;
        this.fixedUnscaledDeltaTime = unscaledDeltaTime;
    }

    public void CopyFrom(GTime other) {
        this.timeScale = other.timeScale;

        this.frameCount = other.frameCount;
        this.time = other.time;
        this.deltaTime = other.deltaTime;
        this.unscaledTime = other.unscaledTime;
        this.unscaledDeltaTime = other.unscaledDeltaTime;

        this.fixedFrameCount = other.fixedFrameCount;
        this.fixedTime = other.fixedTime;
        this.fixedDeltaTime = other.fixedDeltaTime;
        this.fixedUnscaledTime = other.fixedUnscaledTime;
        this.fixedUnscaledDeltaTime = other.fixedUnscaledDeltaTime;
    }

    #region props

    public double TimeScale {
        get => timeScale;
        set {
            if (value < 0) throw new ArgumentException("TimeScale must be >= 0");
            timeScale = value;
        }
    }

    public int FrameCount => frameCount;
    public double Time {
        get => time;
        set => time = value;
    }
    public double DeltaTime {
        get => deltaTime;
        set {
            CheckDeltaTime(value);
            deltaTime = value;
        }
    }
    public double UnscaledTime {
        get => unscaledTime;
        set => unscaledTime = value;
    }
    public double UnscaledDeltaTime {
        get => unscaledDeltaTime;
        set {
            CheckDeltaTime(value);
            unscaledDeltaTime = value;
        }
    }

    public int FixedFrameCount => fixedFrameCount;
    public double FixedTime {
        get => fixedTime;
        set => fixedTime = value;
    }
    public double FixedDeltaTime {
        get => fixedDeltaTime;
        set {
            CheckDeltaTime(value);
            fixedDeltaTime = value;
        }
    }
    public double FixedUnscaledTime {
        get => fixedUnscaledTime;
        set => fixedUnscaledTime = value;
    }
    public double FixedUnscaledDeltaTime {
        get => fixedUnscaledDeltaTime;
        set {
            CheckDeltaTime(value);
            fixedUnscaledDeltaTime = value;
        }
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckDeltaTime(double deltaTime) {
        if (deltaTime < 0) {
            throw new ArgumentException("deltaTime must be >= 0");
        }
    }

    public override string ToString() {
        return $"{nameof(frameCount)}: {frameCount},"
               + $" {nameof(time)}: {time},"
               + $" {nameof(deltaTime)}: {deltaTime},"
               + $" {nameof(unscaledTime)}: {unscaledTime},"
               + $" {nameof(unscaledDeltaTime)}: {unscaledDeltaTime},"
               + $" {nameof(fixedFrameCount)}: {fixedFrameCount},"
               + $" {nameof(fixedTime)}: {fixedTime},"
               + $" {nameof(fixedDeltaTime)}: {fixedDeltaTime},"
               + $" {nameof(fixedUnscaledTime)}: {fixedUnscaledTime},"
               + $" {nameof(fixedUnscaledDeltaTime)}: {fixedUnscaledDeltaTime}";
    }
}
}