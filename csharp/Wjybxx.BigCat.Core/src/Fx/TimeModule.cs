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
using Wjybxx.Commons;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Time;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// 简单的时间模块
/// 1.系统的启动帧我们定为第0帧。
/// 2.应当由<see cref="IEventLoopAgent{T}"/>驱动。
/// 3.每个线程一个，多线程共享是不必要也不安全的。
/// </summary>
public class TimeModule : ITimeProvider
{
    private int frameCount;
    private long startTime;
    private long time;
    private long deltaTime;

    public TimeModule() {
        startTime = time = ObjectUtil.SystemTickMillis();
    }

    public void Start(long curTime) {
        this.frameCount = 0;
        this.startTime = curTime;
        this.time = curTime;
        this.deltaTime = 0;
    }

    public void Update(long curTime) {
        frameCount++;
        this.deltaTime = Math.Max(0, curTime - this.time);
        this.time = curTime;
    }

    /// <summary>
    /// 帧号
    /// </summary>
    public int FrameCount => frameCount;

    /// <summary>
    /// 程序的启动时间
    /// </summary>
    public long StartTime => startTime;

    /// <summary>
    /// 当前时间
    /// </summary>
    public long Time => time;

    /// <summary>
    /// 帧间隔
    /// </summary>
    public long DeltaTime => deltaTime;

    /// <summary>
    /// 启动到当前的时间
    /// </summary>
    public long TimeSinceStarted => time - startTime;
    
    #region unsafe

    [VisibleForTesting]
    public void SetFrameCount(int frame) {
        this.frameCount = frame;
    }

    [VisibleForTesting]
    public void SetDeltaTime(long deltaTime) {
        this.deltaTime = deltaTime;
    }

    [VisibleForTesting]
    public void SetTime(long time) {
        this.time = time;
    }

    #endregion
}
}