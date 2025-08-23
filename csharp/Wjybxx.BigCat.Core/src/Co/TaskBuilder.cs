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
using System.Runtime.InteropServices;
using Wjybxx.BigCat.Gameplay;
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Co
{
public static class TaskBuilder
{
    public const int TYPE_EMPTY = 0; // 空任务
    public const int TYPE_ACTION = 1; // Action()
    public const int TYPE_ACTION_CTX = 2; // Action(object)
    public const int TYPE_FUNC = 3; // Func<R>()
    public const int TYPE_FUNC_CTX = 4; // Func<R>(object)

    internal const int TYPE_AWAIT = 5; // 等待外部任务(检测超时)

    /** 执行一次 */
    public const byte SCHEDULE_ONCE = 0;
    /** 固定延迟 -- 两次执行的间隔大于等于给定的延迟 */
    public const byte SCHEDULE_FIXED_DELAY = 1;
    /** 固定频率 -- 执行次数 */
    public const byte SCHEDULE_FIXED_RATE = 2;

    #region factory

    /// <summary>
    /// 创建一个空任务，用于Sleep等逻辑
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskBuilder<int> NewEmpty() {
        return new TaskBuilder<int>(TYPE_EMPTY, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskBuilder<int> NewAction(Action action, ICancelToken? cancelToken = null) {
        return new TaskBuilder<int>(TYPE_ACTION, action, cancelToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskBuilder<int> NewAction(Action<object> action, object ctx) {
        return new TaskBuilder<int>(TYPE_ACTION_CTX, action, ctx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskBuilder<T> NewFunc<T>(Func<T> func, ICancelToken? cancelToken = null) {
        return new TaskBuilder<T>(TYPE_FUNC, func, cancelToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskBuilder<T> NewFunc<T>(Func<object, T> func, object ctx) {
        return new TaskBuilder<T>(TYPE_FUNC_CTX, func, ctx);
    }

    #endregion

    #region 校验

    /** 适用于禁止初始延迟小于0的情况 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ValidateInitialDelay(double initialDelay) {
        if (initialDelay < 0) throw new ArgumentException($"initialDelay: {initialDelay} (expected: >= 0)");
    }

    /** 周期任务的调度间隔不可小于等于0 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ValidatePeriod(double period) {
        if (period <= 0) throw new ArgumentException("period: 0 (expected: != 0)");
    }

    #endregion
}

/// <summary>
/// 任务构建器
///
/// 注：
/// 1.首次延迟小于0的处理，取决于调度器 -- 通常会被修正为0，即不能插入到当前任务前方。
/// 2.由于静态工厂方法会导致冗余拷贝，因此我们开放<code>SetAction</code>等方法。
/// </summary>
/// <typeparam name="T"></typeparam>
[StructLayout(LayoutKind.Auto)]
public struct TaskBuilder<T>
{
#nullable disable
    private int _type;
    private object _task;
    private object? _ctx;
    private int _options;

    private GameLoopPhase _phase;
    private byte _scheduleType;
    private double _initialDelay;
    private double _period;
    private double _timeout;
    private int _countLimit;
    private int _extraDelayFrame;

    internal TaskBuilder(int type, object task, object? ctx = null) : this() {
        this._type = type;
        this._task = task ?? throw new ArgumentNullException(nameof(task));
        this._ctx = ctx;
        this._options = 0;
    }
#nullable restore

    #region factory

    public void SetAction(Action action, ICancelToken? cancelToken = null) {
        _type = TaskBuilder.TYPE_ACTION;
        this._task = action ?? throw new ArgumentNullException(nameof(action));
        this._ctx = cancelToken;
    }

    public void SetAction(Action<object> action, object ctx) {
        _type = TaskBuilder.TYPE_ACTION_CTX;
        this._task = action ?? throw new ArgumentNullException(nameof(action));
        this._ctx = ctx;
    }

    public void SetFunc(Func<T> func, ICancelToken? cancelToken = null) {
        _type = TaskBuilder.TYPE_FUNC;
        this._task = func ?? throw new ArgumentNullException(nameof(func));
        this._ctx = cancelToken;
    }

    public void SetFunc(Func<object, T> func, object ctx) {
        _type = TaskBuilder.TYPE_FUNC_CTX;
        this._task = func ?? throw new ArgumentNullException(nameof(func));
        this._ctx = ctx;
    }

    #endregion

    /// <summary>
    /// 任务的类型
    /// </summary>
    public int Type => _type;

    /// <summary>
    /// 委托
    /// </summary>
    public object Task => _task;

    /// <summary>
    /// 任务的上下文
    /// </summary>
    public object? Context {
        get => _ctx;
        set => _ctx = value;
    }

    /// <summary>
    /// 最终options
    /// </summary>
    public int Options {
        get => _options;
        set => _options = value;
    }

    /// <summary>
    /// 是否启用了某选项
    /// </summary>
    /// <param name="optionMask"></param>
    /// <returns></returns>
    public bool IsEnabled(int optionMask) {
        return (_options & optionMask) != 0;
    }

    /// <summary>
    /// 启用选项
    /// </summary>
    /// <param name="optionMask"></param>
    public void Enable(int optionMask) {
        _options |= optionMask;
    }

    /// <summary>
    /// 禁用选项
    /// </summary>
    /// <param name="optionMask"></param>
    public void Disable(int optionMask) {
        _options &= ~optionMask;
    }

    #region 延时任务

    /// <summary>
    /// 首次触发延迟（时间或帧数）
    /// </summary>
    public double InitialDelay => _initialDelay;
    /// <summary>
    /// 触发周期（时间或帧数）
    /// </summary>
    public double Period => _period;

    /// <summary>
    /// 调度类型
    /// </summary>
    public byte ScheduleType => _scheduleType;
    /// <summary>
    /// 是否是周期性任务
    /// </summary>
    public bool IsPeriodic => _scheduleType != 0;
    /// <summary>
    /// 是否是一次性任务
    /// </summary>
    public bool IsOnlyOnce => _scheduleType == TaskBuilder.SCHEDULE_ONCE;

    /// <summary>
    /// 设置任务为单次执行
    /// </summary>
    /// <param name="delay">触发延迟</param>
    public void SetOnlyOnce(double delay) {
        this._scheduleType = TaskBuilder.SCHEDULE_ONCE;
        this._initialDelay = delay;
        this._period = 0;
    }

    public bool IsFixedDelay => _scheduleType == TaskBuilder.SCHEDULE_FIXED_DELAY;

    /// <summary>
    /// 设置任务为固定延迟执行
    /// </summary>
    /// <param name="initialDelay">首次延迟</param>
    /// <param name="period">循环周期</param>
    public void SetFixedDelay(double initialDelay, double period) {
        TaskBuilder.ValidatePeriod(period);
        this._scheduleType = TaskBuilder.SCHEDULE_FIXED_DELAY;
        this._initialDelay = initialDelay;
        this._period = period;
    }

    public bool IsFixedRate => _scheduleType == TaskBuilder.SCHEDULE_FIXED_RATE;

    /// <summary>
    /// 设置任务为固定频率执行（会补帧）
    ///
    /// 注：一般业务不应该使用该模式。
    /// </summary>
    /// <param name="initialDelay">首次延迟</param>
    /// <param name="period">循环周期</param>
    public void SetFixedRate(double initialDelay, double period) {
        TaskBuilder.ValidateInitialDelay(initialDelay);
        TaskBuilder.ValidatePeriod(period);
        this._scheduleType = TaskBuilder.SCHEDULE_FIXED_RATE;
        this._initialDelay = initialDelay;
        this._period = period;
    }

    /// <summary>
    /// 是否设置了超时时间
    /// </summary>
    public bool HasTimeout => IsEnabled(TaskOptions.HAS_TIMEOUT);

    /// <summary>
    /// 1. 默认只在执行任务后检查是否超时，以确保至少会执行一次
    /// 2. 达到截止时间后任务将被取消<see cref="BetterCancellationException"/> -- 任何的主动退出都使用取消。
    ///
    /// PS：使用取消异常是为了避免捕获堆栈，Future只对取消异常进行了优化。
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public double Timeout {
        get => _timeout;
        set {
            if (value < 0) {
                throw new ArgumentException("invalid timeout: " + value);
            }
            _timeout = value;
            Enable(TaskOptions.HAS_TIMEOUT);
        }
    }

    /// <summary>
    /// 通过预估执行次数限制超时时间
    /// 该方法对于fixedRate类型的任务有帮助
    /// </summary>
    /// <param name="count"></param>
    public void SetTimeoutByCount(int count) {
        if (count < 1) {
            throw new ArithmeticException("invalid count: " + count);
        }
        if (count == 1) {
            this._timeout = Math.Max(0, _initialDelay);
        } else {
            this._timeout = Math.Max(0, _initialDelay + (count - 1) * Period);
        }
        Enable(TaskOptions.HAS_TIMEOUT);
    }

    /// <summary>
    /// 是否包含执行次数限制
    /// </summary>
    public bool HasCountLimit => IsEnabled(TaskOptions.HAS_COUNT_LIMIT);

    /// <summary>
    /// 设置任务的执行次数限制
    ///
    /// 注：
    /// 1.到达执行上限后任务将被取消<see cref="BetterCancellationException"/> -- 任何的主动退出都使用取消。
    /// 2.使用取消异常是为了避免捕获堆栈，Future只对取消异常进行了优化。
    /// </summary>
    public int CountLimit {
        get => _countLimit;
        set {
            if (value < 1) {
                throw new ArgumentException("invalid count: " + value);
            }
            _countLimit = value;
            Enable(TaskOptions.HAS_COUNT_LIMIT);
        }
    }

    /// <summary>
    /// 是否包含额外等待帧
    /// </summary>
    public bool HasExtraDelayFrame => IsEnabled(TaskOptions.HAS_DELAY_FRAME);

    /// <summary>
    /// 首次执行额外等待的帧数
    ///
    /// 注：为避免歧义（放大差异），不命名为initialDelayFrame。
    /// </summary>
    public int ExtraDelayFrame {
        get => _extraDelayFrame;
        set {
            if (value < 0) {
                throw new ArgumentException("invalid delay frame: " + value);
            }
            _extraDelayFrame = value;
            Enable(TaskOptions.HAS_DELAY_FRAME);
        }
    }

    /// <summary>
    /// 是否显式指定了调度阶段
    /// </summary>
    public bool HasSchedulePhase => IsEnabled(TaskOptions.HAS_SCHEDULE_PHASE);

    /// <summary>
    /// 任务的调度阶段
    /// </summary>
    public GameLoopPhase SchedulePhase {
        get => HasSchedulePhase ? _phase : GameLoopPhase.Update;
        set {
            _phase = value;
            _options |= TaskOptions.HAS_SCHEDULE_PHASE;
        }
    }

    /// <summary>
    /// 是否包含优先级
    /// </summary>
    public bool HasPriority => IsEnabled(TaskOptions.HAS_PRIORITY);

    /// <summary>
    /// 设置任务的优先级
    /// </summary>
    public int Priority {
        get => TaskOptions.GetPriority(_options);
        set {
            _options = TaskOptions.SetPriority(_options, value);
            Enable(TaskOptions.HAS_PRIORITY);
        }
    }

    #endregion
}
}