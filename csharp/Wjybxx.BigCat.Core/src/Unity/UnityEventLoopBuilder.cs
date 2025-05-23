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
using System.Threading;
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Disruptor;

namespace Wjybxx.BigCat.Unity
{
public class UnityEventLoopBuilder<T> : EventLoopBuilder<T> where T : IAgentEvent
{
#nullable disable
    private readonly Thread thread;
    private EventSequencer<T> eventSequencer;
    private WaitStrategy waitStrategy = UnityWaitStrategy.Inst;
    private bool publishValueEventWithCopy;
#nullable enable

    /// <summary>
    /// 
    /// </summary>
    /// <param name="thread">unity线程引用</param>
    public UnityEventLoopBuilder(Thread thread) {
        this.thread = thread ?? throw new ArgumentNullException(nameof(thread));
    }

    private void CheckBuild() {
        if (ThreadFactory == null) {
            ThreadFactory = new DefaultThreadFactory("DisruptorEventLoop");
        }
        if (eventSequencer == null) {
            throw new IllegalStateException("eventSequencer is null");
        }
    }

#if NET6_0_OR_GREATER
    public override UnityEventLoop<T> Build() {
#else
    public override IEventLoop Build() {
#endif
        CheckBuild();
        return new UnityEventLoop<T>(this);
    }

    /// <summary>
    /// 绑定的Unity线程
    /// </summary>
    public Thread Thread => thread;

    /// <summary>
    /// 事件序列生成器
    /// 注意：应当避免使用无超时的等待策略，EventLoop需要处理定时任务，不能一直等待生产者。
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public EventSequencer<T>? EventSequencer {
        get => eventSequencer;
        set => eventSequencer = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// 等待策略
    /// 1.如果未显式指定，则使用<see cref="Sequencer.WaitStrategy"/>中的默认等待策略。
    /// 2.应当避免使用无超时的等待策略，EventLoop需要处理定时任务，不能一直等待生产者。
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public WaitStrategy WaitStrategy {
        get => waitStrategy;
        set => waitStrategy = value;
    }

    /// <summary>
    /// 当事件类型为值类型时，发布事件时是否采用copy的方式。
    /// 对于无界队列来说，采用copy的方式可以减少一次根据sequence查找data槽的开销，在生产者竞争较强的情况下可以提高性能。
    /// 对于有界队列来说，采用copy可以减少一小部分方法调用，影响可能不大。
    /// 用户需要权衡拷贝1次事件的开销和根据sequence查找data槽的开销。
    /// <see cref="EventSequencer.Publish(long, T)"/>
    /// </summary>
    public bool PublishValueEventWithCopy {
        get => publishValueEventWithCopy;
        set => publishValueEventWithCopy = value;
    }
}
}