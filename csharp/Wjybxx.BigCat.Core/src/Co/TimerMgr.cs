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
using Wjybxx.BigCat.Gameplay;
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Co
{
/// <summary>
/// 游戏World专用Timer管理器
/// </summary>
public sealed class TimerMgr
{
    private static readonly GameLoopPhase[] phaseArray = EnumUtil.GetValues<GameLoopPhase>();

    // 基于非缩放时间等待的队列
    private readonly TimerQueue unscaledQueue0;
    private readonly TimerQueue unscaledQueue1;
    private readonly TimerQueue unscaledQueue2;
    private readonly TimerQueue unscaledQueue3;
    private readonly TimerQueue unscaledQueue4;
    private readonly TimerQueue unscaledQueue5;
    private readonly TimerQueue unscaledQueue6;
    private readonly TimerQueue unscaledQueue7;
    private readonly TimerQueue unscaledQueue8;
    private readonly TimerQueue unscaledQueue9;
    // 基于逻辑时间等待的队列
    private readonly TimerQueue timeQueue1;
    private readonly TimerQueue timeQueue2;
    private readonly TimerQueue timeQueue3;
    private readonly TimerQueue timeQueue4;
    private readonly TimerQueue timeQueue5;
    private readonly TimerQueue timeQueue6;
    private readonly TimerQueue timeQueue7;
    private readonly TimerQueue timeQueue8;

    private readonly IEventLoop _eventLoop;
    private readonly ITime _time;

    public TimerMgr(IEventLoop eventLoop, ITime time) {
        _eventLoop = eventLoop;
        _time = time;
        //
        ITimeProvider unscaledTime = time.GetUnscaledFacade();
        unscaledQueue0 = new TimerQueue(eventLoop, unscaledTime, 0);
        unscaledQueue1 = new TimerQueue(eventLoop, unscaledTime, 1);
        unscaledQueue2 = new TimerQueue(eventLoop, unscaledTime, 2);
        unscaledQueue3 = new TimerQueue(eventLoop, unscaledTime, 3);
        unscaledQueue4 = new TimerQueue(eventLoop, unscaledTime, 4);
        unscaledQueue5 = new TimerQueue(eventLoop, unscaledTime, 5);
        unscaledQueue6 = new TimerQueue(eventLoop, unscaledTime, 6);
        unscaledQueue7 = new TimerQueue(eventLoop, unscaledTime, 7);
        unscaledQueue8 = new TimerQueue(eventLoop, unscaledTime, 8);
        unscaledQueue9 = new TimerQueue(eventLoop, unscaledTime, 9);
        //
        timeQueue1 = new TimerQueue(eventLoop, time, 11);
        timeQueue2 = new TimerQueue(eventLoop, time, 12);
        timeQueue3 = new TimerQueue(eventLoop, time, 13);
        timeQueue4 = new TimerQueue(eventLoop, time, 14);
        timeQueue5 = new TimerQueue(eventLoop, time, 15);
        timeQueue6 = new TimerQueue(eventLoop, time, 16);
        timeQueue7 = new TimerQueue(eventLoop, time, 17);
        timeQueue8 = new TimerQueue(eventLoop, time, 18);
    }

    public IEventLoop EventLoop => _eventLoop;
    public ITime Time => _time;

    /// <summary>
    /// 启动所有定时器队列
    /// </summary>
    public void Start() {
        foreach (GameLoopPhase phase in phaseArray) {
            GetQueue(phase, TimingType.Time, false)?.Start();
        }
        foreach (GameLoopPhase phase in phaseArray) {
            GetQueue(phase, TimingType.UnscaledTime, false)?.Start();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="quietly"></param>
    public void Stop(bool quietly = false) {
        foreach (GameLoopPhase phase in phaseArray) {
            GetQueue(phase, TimingType.Time, false)?.Stop(quietly);
        }
        foreach (GameLoopPhase phase in phaseArray) {
            GetQueue(phase, TimingType.UnscaledTime, false)?.Stop(quietly);
        }
    }

    public void Update(GameLoopPhase phase) {
        switch (phase) {
            case GameLoopPhase.BeginOfFrame: {
                unscaledQueue0.Update();
                break;
            }
            //
            case GameLoopPhase.EarlyUpdate: {
                // 非缩放时间，缩放时间，帧数
                unscaledQueue1.Update();
                timeQueue1.Update();
                break;
            }
            case GameLoopPhase.PostEarlyUpdate: {
                // 帧数，缩放时间，非缩放时间
                timeQueue2.Update();
                unscaledQueue2.Update();
                break;
            }
            //
            case GameLoopPhase.FixedUpdate: {
                unscaledQueue3.Update();
                timeQueue3.Update();
                break;
            }
            case GameLoopPhase.PostFixedUpdate: {
                timeQueue4.Update();
                unscaledQueue4.Update();
                break;
            }
            //
            case GameLoopPhase.Update: {
                unscaledQueue5.Update();
                timeQueue5.Update();
                break;
            }
            case GameLoopPhase.PostUpdate: {
                timeQueue6.Update();
                unscaledQueue6.Update();
                break;
            }
            //
            case GameLoopPhase.LateUpdate: {
                unscaledQueue7.Update();
                timeQueue7.Update();
                break;
            }
            case GameLoopPhase.PostLateUpdate: {
                timeQueue8.Update();
                unscaledQueue8.Update();
                break;
            }
            //
            case GameLoopPhase.EndOfFrame: {
                unscaledQueue9.Update();
                break;
            }
        }
    }

#nullable disable
    /// <summary>
    /// 获取调度队列
    /// </summary>
    /// <param name="phase">调度阶段</param>
    /// <param name="timingType">计时类型</param>
    /// <param name="throwException">队列不存在时是否抛出异常</param>
    public TimerQueue GetQueue(GameLoopPhase phase, TimingType timingType = TimingType.Time, bool throwException = true) {
        int baseId = timingType switch
        {
            TimingType.UnscaledTime => 0,
            TimingType.Time => 10,
            TimingType.FrameCount => 20,
            _ => throw new ArgumentOutOfRangeException(nameof(timingType))
        };
        return GetQueue(baseId + (int)phase, throwException);
    }
#nullable restore

    private TimerQueue? GetQueue(int queueId, bool throwException = true) {
        return queueId switch
        {
            0 => unscaledQueue0,
            1 => unscaledQueue1,
            2 => unscaledQueue2,
            3 => unscaledQueue3,
            4 => unscaledQueue4,
            5 => unscaledQueue5,
            6 => unscaledQueue6,
            7 => unscaledQueue7,
            8 => unscaledQueue8,
            9 => unscaledQueue9,
            //
            11 => timeQueue1,
            12 => timeQueue2,
            13 => timeQueue3,
            14 => timeQueue4,
            15 => timeQueue5,
            16 => timeQueue6,
            17 => timeQueue7,
            18 => timeQueue8,
            //
            _ => throwException
                ? throw new ArgumentOutOfRangeException(nameof(queueId), null, queueId.ToString())
                : null
        };
    }
}
}