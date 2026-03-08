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
using Wjybxx.Commons.Concurrent;
using Wjybxx.Disruptor;

namespace Wjybxx.BigCat.Fx
{
public class DefaultNodeBuilder : NodeBuilder
{
    public DefaultNodeBuilder()
        : base(new DisruptorEventLoopBuilder<WorkerEvent>()) {
    }

    public new DisruptorEventLoopBuilder<WorkerEvent> Delegated => (DisruptorEventLoopBuilder<WorkerEvent>)delegated;

#if NET6_0_OR_GREATER
    public override INode Build() {
#else
    public override IWorker Build() {
#endif
        if (EventSequencer == null) {
            EventSequencer = new MpUnboundedEventSequencer<WorkerEvent>.Builder(WorkerEvent.FACTORY)
            {
                WaitStrategy = TimeoutSleepingWaitStrategy.Inst
            }.Build();
        }
        if (WorkerFactory == null) {
            WorkerFactory = (parent, index, controlData) => {
                return new DefaultWorkerBuilder()
                {
                    Parent = parent,
                    Index = index,
                    ControlData = controlData,
                    WorkerId = "Worker-" + index,
                }.Build();
            };
        }
        if (ThreadFactory == null) {
            ThreadFactory = new DefaultThreadFactory("Worker");
        }
        return new Node(this);
    }

    /// <summary>
    /// 事件序列生成器
    /// 注意：应当避免使用无超时的等待策略，EventLoop需要处理定时任务，不能一直等待生产者。
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public EventSequencer<WorkerEvent>? EventSequencer {
        get => Delegated.EventSequencer;
        set => Delegated.EventSequencer = value;
    }

    /// <summary>
    /// 等待策略
    /// 1.如果未显式指定，则使用<see cref="Sequencer.WaitStrategy"/>中的默认等待策略。
    /// 2.应当避免使用无超时的等待策略，EventLoop需要处理定时任务，不能一直等待生产者。
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public WaitStrategy WaitStrategy {
        get => Delegated.WaitStrategy;
        set => Delegated.WaitStrategy = value;
    }
}
}