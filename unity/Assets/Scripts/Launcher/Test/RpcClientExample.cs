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
using System.Threading;
using UnityEngine;
using Wjybxx.BigCat.Fx;
using Wjybxx.BigCat.Util;
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Inject.Attributes;
using Wjybxx.Commons.Logger;
using Wjybxx.Commons.Time;
using static Wjybxx.BigCat.Fx.ExtensibleService;
using ILogger = Wjybxx.Commons.Logger.ILogger;

namespace Wjybxx.BigCat.Tests
{
[RpcService(ServiceId = 12)]
public class RpcClientExample : EventLoopModule, ExtensibleService
{
    private static readonly ILogger logger = LoggerFactory.GetLogger<RpcClientExample>();
#nullable disable
    /** worker */
    private Worker worker;
    /** 定时器 */
    private readonly Regulator regulator = Regulator.NewFixedDelay(1, 100);

    [Inject] private RpcClient rpcClient;
    [Inject] private TimeModule timeModule;

    // 测试从接口继承的方法
    private readonly Dictionary<string, object> extBlackboard = new();
    // 目标地址 -- 本地
    private WorkerAddr serverAddr;
#nullable enable

    /// <summary>
    /// 接收服务端发来的通知
    /// </summary>
    /// <param name="request"></param>
    [RpcMethod(MethodId = 1)]
    public void OnMessage(Request request) {
        logger.Info(request.String1);
    }

    public Dictionary<string, object> ExtBlackboard => extBlackboard;

    public ExecuteResult Execute(ExecuteRequest request) {
        return new ExecuteResult();
    }

    #region logic

    public override void ResolveDependence() {
        this.worker = (Worker)Entity;
        this.serverAddr = worker.Node.NodeAddr;
    }

    public override void Start() {
        regulator.Restart(timeModule.Time);
    }


    public override void Stop() {
        logger.Info("triggerCount: " + regulator.TriggerCount);
    }

    public override void Update() {
        if (!regulator.IsReady(timeModule.Time)) {
            return;
        }
        int seed = MathCommon.SharedRandom.Next(4);
        switch (seed) {
            case 0:
                TestOneway();
                break;
            case 1:
                TestAsyncCall().Forget();
                break;
            case 2:
                TestSyncCall();
                break;
            case 3:
                TestContext().Forget();
                break;
        }
    }

    private void TestOneway() {
        string msg = CreateMessage("这是一个通知，不接收结果");
        rpcClient.Send(serverAddr, RpcServiceExampleProxy.Hello(Request.OfString(msg)));
    }

    private async ValueFuture TestAsyncCall() {
        string msg = CreateMessage("这是一个异步调用，可监听结果");
        Response result = await rpcClient.Call(serverAddr, RpcServiceExampleProxy.Hello(Request.OfString(msg)));

        // 启用本地共享的情况下应当是同一个字符串
        // Assert.AreEqual(msg, result.StringVal);
        // Assert.IsTrue(worker.InEventLoop(), "worker.inEventLoop");
        logger.Info("callResult: " + result.StringVal + "\n"); // 避免冗余堆栈
    }

    private void TestSyncCall() {
        try {
            string msg = CreateMessage("这是一个同步调用，远程异步执行");
            Response result = rpcClient.SyncCall(serverAddr, RpcServiceExampleProxy.HelloAsync(Request.OfString(msg)));
            logger.Info("syncResult: " + result.StringVal + "\n");
        }
        catch (ThreadInterruptedException) {
            logger.Info("syncCall interrupted");
        }
        catch (TimeoutException ex) {
            logger.Info("syncCall timeout", ex);
        }
    }

    private async ValueFuture TestContext() {
        string msg = CreateMessage("这是一个异步调用，目标函数有Context");
        var methodSpec = RpcServiceExampleProxy.ContextHello(Request.OfString(msg));
        Response response = await rpcClient.Call(serverAddr, methodSpec);
        logger.Info(response.ToString());
    }

    private readonly int offsetSeconds = (int)TimeZoneInfo.Local.BaseUtcOffset.TotalSeconds;

    private string CreateMessage(string msg) {
        // Kind会影响ToString
        DateTime dateTime = DatetimeUtil.ToDateTime(regulator.TriggerTime);
        dateTime = new DateTime(dateTime.AddSeconds(offsetSeconds).Ticks, DateTimeKind.Unspecified);
        return "time: " + dateTime.ToString("s") + " # " + msg;
    }

    #endregion
}
}