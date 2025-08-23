#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
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
using NUnit.Framework;
using Wjybxx.BigCat.Co;
using Wjybxx.BigCat.Gameplay;
using Wjybxx.BigCat.Util;
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Disruptor;

namespace Wjybxx.BigCat.Tests.Co
{
public class CoroutineTest
{
    private static readonly IEventLoop globalEventLoop = new DisruptorEventLoopBuilder<AgentEvent>()
    {
        ThreadFactory = new DefaultThreadFactory("Scheduler", true),
        EventSequencer = new RingBufferEventSequencer<AgentEvent>.Builder(AgentEvent.FACTORY)
            .Build()
    }.Build();

    private static long lastUpdateTime;
    private static readonly GTime time = new GTime();
    private static CoroutineMgr coroutineMgr;
    private static CoroutineUserContext<int, int> userContext;

    [OneTimeSetUp]
    public static void SetUp() {
        lastUpdateTime = ObjectUtil.SystemTickMillis();
        time.Restart();
        coroutineMgr = new CoroutineMgr(globalEventLoop, time, enableUnscaledQueue: true, enableFrameQueue: true);
    }

    [Test]
    public void Test() {
        globalEventLoop.ScheduleWithFixedDelay(MainLoop, TimeSpan.Zero, TimeSpan.FromMilliseconds(10))
            .AsFuture().Join();
    }

    private static void MainLoop() {
        if (time.FrameCount > 100) {
            throw new OperationCanceledException();
        }
        double deltaTime = (ObjectUtil.SystemTickMillis() - lastUpdateTime) / 1000.0;
        time.Update(deltaTime);
        coroutineMgr.Update(GameLoopPhase.Update);

        if (time.FrameCount == 1) {
            userContext = coroutineMgr.StartCoroutine(CoroutineLoop, new CoroutineStartArgs<int, int>()
            {
                inputCodec = DataKeys.NewIntKey("co_input"),
                outputCodec = DataKeys.NewIntKey("co_output"),
            });
        } else if ((time.FrameCount % 5) == 0) {
            // 每5帧发一个包
            userContext.Write(time.FrameCount);
        }
        if (userContext.TryRead(out int result)) {
            Console.WriteLine($"Echo_DateTime: {DateTime.Now}, frameCount: {result}");
        }
    }

    private static async ValueFuture CoroutineLoop(CoroutineTaskContext<int, int> context) {
        // TaskResult<int> taskResult;
        // while ((taskResult = await context.ReadAsync(0.01)).IsSucceeded) {
        //     Console.WriteLine($"DateTime: {DateTime.Now}, frameCount: {taskResult.Result}");
        // }

        TaskResult<int> taskResult;
        while (true) {
            taskResult = await context.ReadAsync(0.01);
            if (taskResult.IsSucceeded) {
                Console.WriteLine($"DateTime: {DateTime.Now}, frameCount: {taskResult.Result}");
                context.Write(taskResult.Result);
            }
            // if (taskResult.IsCancelled) {
            //     Console.WriteLine($"XXXX_DateTime: {DateTime.Now}, cancelCode: {taskResult.CancelCode}");
            // }
        }

        // while (!context.IsCancelRequest) {
        //     if (context.TryRead(out int input)) {
        //         Console.WriteLine($"DateTime: {DateTime.Now}, frameCount: {input}");
        //     }
        //     ValueFuture<int> future = globalEventLoop.ScheduleFunc(() => time.FrameCount, TimeSpan.FromMilliseconds(10));
        //     int frameCount = await context.Await(future);
        //     Console.WriteLine($"XXXX_DateTime: {DateTime.Now}, frameCount: {frameCount}");
        //
        //     // await context.SleepFrame(1);
        //     // Console.WriteLine("CurrentFrameCount: " + time.FrameCount);
        //     // await context.Sleep(0.01f);
        // }
    }
}
}