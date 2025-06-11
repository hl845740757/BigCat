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
using System.Collections.Generic;
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Inject;
using Wjybxx.Commons.Inject.Attributes;

namespace Wjybxx.BigCat.Fx
{
public class DefaultMainModule : EventLoopModule, IEventLoopAgent<WorkerEvent>
{
#nullable disable
    /** 时间模块由主模块驱动 */
    [Inject]
    protected TimeModule timeModule;
    /** 帧循环间隔 */
    private int frameInterval = 30;

    /** 主循环前时间戳 - 用于计算帧耗时等 */
    protected long timeBeforeMainLoop;
    /** 主循环后时间戳 */
    protected long timeAfterMainLoop;
    /** 上一次主循环耗时 */
    protected long mainLoopTimeSpan;
    //
    /** 事件循环 */
    protected Worker worker;
    /** 事件循环的事件处理器 */
    protected readonly Dictionary<int, IAgentEventHandler<WorkerEvent>> handlerMap = new(20);

    #region 事件

    public void Inject(IEventLoop eventLoop, long consumerId) {
        this.worker = (Worker)eventLoop;
    }

    public void Subscribe(int type, IAgentEventHandler<WorkerEvent> handler) {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (handlerMap.ContainsKey(type)) {
            throw new ArgumentException("type: " + type);
        }
        handlerMap[type] = handler;
    }

    public void OnEvent(long sequence, ref WorkerEvent rawEvent) {
        if (handlerMap.TryGetValue(rawEvent.Type, out IAgentEventHandler<WorkerEvent> handler)) {
            handler.OnEvent(sequence, ref rawEvent);
        }
    }

    #endregion

    #region 主循环

    /// <summary>
    /// 帧间隔
    /// </summary>
    public int FrameInterval {
        get => frameInterval;
        set {
            if (value < 1) throw new ArgumentException("frameInterval: " + frameInterval);
            frameInterval = value;
        }
    }

    /// <summary>
    /// 获取前一次主循环耗时 -- 或当前主循环结束后查看本次耗时
    /// </summary>
    /// <value></value>
    public long MainLoopTimeSpan => mainLoopTimeSpan;

    /// <summary>
    /// 实时的主循环耗时
    /// 注意：我们这里需要使用Unix时间戳，而不是系统Tick对应的毫秒数
    /// </summary>
    public long MainLoopElapsed => DatetimeUtil.CurrentEpochMillis() - timeBeforeMainLoop;

    public void BeforeEventLoopStart() {
        timeModule.Start(DatetimeUtil.CurrentEpochMillis());
        timeBeforeMainLoop = timeAfterMainLoop = timeModule.Time;
    }

    public bool CheckMainLoop(long threadTime) {
        return DatetimeUtil.CurrentEpochMillis() - timeModule.Time >= frameInterval;
    }

    public void BeforeMainLoop(long threadTime) {
        long timeMillis = DatetimeUtil.CurrentEpochMillis();
        timeModule.Update(timeMillis);
        timeBeforeMainLoop = timeMillis;
    }

    public void AfterMainLoop(long threadTime) {
        timeAfterMainLoop = DatetimeUtil.CurrentEpochMillis();
        mainLoopTimeSpan = timeAfterMainLoop - timeBeforeMainLoop;
    }

    #endregion
}
}