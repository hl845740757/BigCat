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
using System.Runtime.CompilerServices;
using System.Threading;
using Wjybxx.BigCat.Util;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Pool;

namespace Wjybxx.BigCat.Co
{
/// <summary>
/// 协程抽象
///
/// 1.协程对象在复用时会更新实例id，表示已被重用
/// 2.当输入输出类型都是引用类型时，全部转为object
/// TODO 可能还需要支持协程退出通知
/// </summary>
internal sealed class Coroutine
{
    // private static readonly ILogger logger = LoggerFactory.GetLogger(typeof(Coroutine));
    // public static readonly DataKey<object> CODEC_INPUT = DataKeys.NewObjectKey("co_input");
    // public static readonly DataKey<object> CODEC_OUTPUT = DataKeys.NewObjectKey("co_output");

    private const int MASK_CANCEL_REQUESTED = 0x01; // 已收到取消信号
    private const int MASK_INTERRUPTED = 0x02; // 已收到中断请求
    private const int MASK_TERMINATED = 0x04; // 已执行结束 - 任务上下文已销毁
    private const int MASK_USER_CONTEXT_DISPOSED = 0x08; // 用户上下文已销毁

#nullable disable
    /// <summary>
    /// 协程id
    /// </summary>
    public long id = -1;
    /// <summary>
    /// 辅助控制标识
    /// </summary>
    private int ctl;

    /// <summary>
    /// 用户命令缓冲区
    /// 注：只能在事件循环线程下访问。
    /// </summary>
    internal readonly Queue<UnionValue> cmdBuffer = new Queue<UnionValue>();
    /// <summary>
    /// 任务结果缓冲区
    /// 注：只能在事件循环线程下访问。
    /// </summary>
    internal readonly Queue<UnionValue> resultBuffer = new Queue<UnionValue>();

    /// <summary>
    /// 协程自身的执行结果
    /// </summary>
    internal ValueFuture coResult;
    /// <summary>
    /// 关联的异步任务
    ///
    /// 注：为了避免额外的查询，故存储在协程对象上，由协程管理器维护。
    /// </summary>
    internal PromiseTask asyncTask;
    /// <summary>
    /// 协程任务读取用户命令的Promise
    ///
    /// 注：有三处设置结果的地方：用户写入命令、超时取消、协程被中断。
    /// </summary>
    internal ValuePromise<int>? cmdReaderPromise;
    /// <summary>
    /// 用户读取任务结果的Promise
    ///
    /// 注：有三处设置结果的地方：协程返回结果、超时取消、协程结束。
    /// </summary>
    internal ValuePromise<int>? resultReaderPromise;
    internal int cmdReaderPromiseRid;
    internal int resultReaderPromiseRid;

    /// <summary>
    /// 协程关联的取消令牌
    /// (非心跳驱动，框架不能直接检查，需任务自己检查)
    /// </summary>
    internal CancellationToken cancelToken;
    internal CancellationTokenRegistration cancelRegistration;
#nullable restore

    private Coroutine() {
    }

    public void Reset() {
        id = -1;
        ctl = 0;
        cmdBuffer.Clear();
        resultBuffer.Clear();

        coResult = default;
        asyncTask = null;
        cmdReaderPromise = null;
        resultReaderPromise = null;
        cmdReaderPromiseRid = 0;
        resultReaderPromiseRid = 0;

        cancelRegistration.Dispose();
        cancelToken = default;
        cancelRegistration = default;
    }

    /// <summary>
    /// 否已收到取消信号
    /// </summary>
    public bool IsCancellationRequested {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ctl & MASK_CANCEL_REQUESTED) != 0;
        set => SetCtlBit(MASK_CANCEL_REQUESTED, value);
    }

    /// <summary>
    /// 是否已被中断
    /// </summary>
    public bool IsInterrupted {
        get => (ctl & MASK_INTERRUPTED) != 0;
        set => SetCtlBit(MASK_INTERRUPTED, value);
    }

    /// <summary>
    /// 协程是否已执行结束
    /// </summary>
    public bool IsTerminated {
        get => (ctl & MASK_TERMINATED) != 0;
        set => SetCtlBit(MASK_TERMINATED, value);
    }

    /// <summary>
    /// 用户上下文是否已销毁
    /// </summary>
    public bool IsUserContextDisposed {
        get => (ctl & MASK_USER_CONTEXT_DISPOSED) != 0;
        set => SetCtlBit(MASK_USER_CONTEXT_DISPOSED, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetCtlBit(int mask, bool enable) {
        if (enable) {
            ctl |= mask;
        } else {
            ctl &= ~mask;
        }
    }

    #region 池化

    /// <summary>
    /// 协程对象池 TODO 抽取环境变量
    /// </summary>
    private static readonly ConcurrentObjectPool<Coroutine> taskPool = new ConcurrentObjectPool<Coroutine>(
        () => new Coroutine(), task => task.Reset(), 2048);

    /// <summary>
    /// 全局ID生成器
    /// </summary>
    private static PaddedInt64 _idGenerator = new PaddedInt64(0);

    /// <summary>
    /// 申请Task对象
    /// </summary>
    public static Coroutine Acquire() {
        return taskPool.Acquire();
    }

    /// <summary>
    /// 将Task归还到对象池
    /// </summary>
    public static void Release(Coroutine task) {
        taskPool.Release(task);
    }

    /// <summary>
    /// 全局ID分配
    /// </summary>
    /// <returns></returns>
    public static long NextId() => _idGenerator.IncrementAndGet();

    #endregion
}
}