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
using System.Threading;
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Co
{
public static class TaskBuilder
{
    public const int TYPE_EMPTY = 0; // 空任务 - 协程Sleep
    public const int TYPE_ACTION = 1; // Action()
    public const int TYPE_ACTION_STATE = 2; // Action(object)
    public const int TYPE_FUNC = 3; // Func<R>()
    public const int TYPE_FUNC_STATE = 4; // Func<R>(object)
    internal const int TYPE_CANCELLER = 5; // 取消器(等待外部任务超时检测器)
    internal const int TYPE_SET_RESULT = 6; // 设置await的结果
    internal const int TYPE_SET_EXCEPTION = 7; // 设置await的结果

    /** 执行一次 */
    public const byte SCHEDULE_ONCE = 0;
    /** 固定延迟 -- 两次执行的间隔大于等于给定的延迟 */
    public const byte SCHEDULE_FIXED_DELAY = 1;
    /** 固定频率 -- 执行次数 */
    public const byte SCHEDULE_FIXED_RATE = 2;

    private static readonly Action EMPTY_ACTION = () => { };

    #region factory

    /// <summary>
    /// 创建一个空任务，用于Sleep等逻辑
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskBuilder<int> NewEmpty() {
        return new TaskBuilder<int>(TYPE_EMPTY, EMPTY_ACTION, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskBuilder<int> NewAction(Action action, CancellationToken cancelToken = default) {
        return new TaskBuilder<int>(TYPE_ACTION, action, null, cancelToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskBuilder<int> NewAction(Action<object> action, object? state, CancellationToken cancelToken = default) {
        return new TaskBuilder<int>(TYPE_ACTION_STATE, action, state, cancelToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskBuilder<T> NewFunc<T>(Func<T> func, CancellationToken cancelToken = default) {
        return new TaskBuilder<T>(TYPE_FUNC, func, cancelToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskBuilder<T> NewFunc<T>(Func<object, T> func, object state, CancellationToken cancelToken = default) {
        return new TaskBuilder<T>(TYPE_FUNC_STATE, func, state, cancelToken);
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
        if (period <= 0) throw new ArgumentException($"period: {period} (expected: > 0)");
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
    private object? _state;
    private CancellationToken _cancelToken;
    private int _options;

    // private GameLoopPhase _phase;
    private byte _scheduleType;
    private double _initialDelay;
    private double _period;
    private double _timeout;
    private int _countLimit;
    private int _extraDelayFrame;
    private int _priority;

    internal TaskBuilder(int type, object task, object? state, CancellationToken cancelToken = default) : this() {
        this._type = type;
        this._task = task ?? throw new ArgumentNullException(nameof(task));
        this._state = state;
        this._cancelToken = cancelToken;
        this._options = 0;
    }
#nullable restore

    #region factory

    public void SetAction(Action action, CancellationToken cancelToken = default) {
        _type = TaskBuilder.TYPE_ACTION;
        this._task = action ?? throw new ArgumentNullException(nameof(action));
        this._state = cancelToken;
    }

    public void SetAction(Action<object> action, object ctx) {
        _type = TaskBuilder.TYPE_ACTION_STATE;
        this._task = action ?? throw new ArgumentNullException(nameof(action));
        this._state = ctx;
    }

    public void SetFunc(Func<T> func, CancellationToken cancelToken = default) {
        _type = TaskBuilder.TYPE_FUNC;
        this._task = func ?? throw new ArgumentNullException(nameof(func));
        this._state = cancelToken;
    }

    public void SetFunc(Func<object, T> func, object ctx) {
        _type = TaskBuilder.TYPE_FUNC_STATE;
        this._task = func ?? throw new ArgumentNullException(nameof(func));
        this._state = ctx;
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
    /// 任务参数
    /// </summary>
    public object? State {
        get => _state;
        set => _state = value;
    }
    /// <summary>
    /// 任务的取消令牌
    /// </summary>
    public CancellationToken CancelToken {
        get => _cancelToken;
        set => _cancelToken = value;
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEnabled(int optionMask) {
        return (_options & optionMask) == optionMask;
    }

    /// <summary>
    /// 启用选项
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enable(int optionMask) {
        _options |= optionMask;
    }

    /// <summary>
    /// 禁用选项
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Disable(int optionMask) {
        _options &= ~optionMask;
    }

    /// <summary>
    /// 启用或禁用选项
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetEnable(int optionMask, bool enable) {
        if (enable) {
            _options |= optionMask;
        } else {
            _options &= ~optionMask;
        }
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

    /// <summary>
    /// 是否是固定延时任务
    /// </summary>
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

    /// <summary>
    /// 是否是固定频率任务
    /// </summary>
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
    public bool HasTimeout {
        get => IsEnabled(TaskOptions.HAS_TIMEOUT);
        set => SetEnable(TaskOptions.HAS_TIMEOUT, value);
    }

    /// <summary>
    /// 1. 默认只在执行任务后检查是否超时，以确保至少会执行一次
    /// 2. 达到截止时间后任务将被取消，任何的主动退出都使用取消。
    ///
    /// PS：使用取消异常是为了避免捕获堆栈，Future只对取消异常进行了优化。
    /// </summary>
    public double Timeout {
        get => _timeout;
        set {
            _timeout = value;
            SetEnable(TaskOptions.HAS_TIMEOUT, value > 0);
        }
    }

    /// <summary>
    /// 通过预估执行次数限制超时时间
    /// 该方法对于fixedRate类型的任务有帮助
    /// </summary>
    /// <param name="count"></param>
    public void SetTimeoutByCount(int count) {
        if (count < 1) {
            throw new ArgumentException("invalid count: " + count);
        }
        this.Timeout = count == 1
            ? Math.Max(0, _initialDelay)
            : Math.Max(0, _initialDelay) + (count - 1) * Period;
    }

    /// <summary>
    /// 是否包含执行次数限制
    /// </summary>
    public bool HasCountLimit {
        get => IsEnabled(TaskOptions.HAS_COUNT_LIMIT);
        set => SetEnable(TaskOptions.HAS_COUNT_LIMIT, value);
    }

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
            _countLimit = value;
            SetEnable(TaskOptions.HAS_COUNT_LIMIT, value > 0);
        }
    }

    /// <summary>
    /// 是否包含额外等待帧
    /// </summary>
    public bool HasExtraDelayFrame {
        get => IsEnabled(TaskOptions.HAS_DELAY_FRAME);
        set => SetEnable(TaskOptions.HAS_DELAY_FRAME, value);
    }

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
    /// 是否包含优先级
    /// </summary>
    public bool HasPriority {
        get => IsEnabled(TaskOptions.HAS_PRIORITY);
        set => SetEnable(TaskOptions.HAS_PRIORITY, value);
    }

    /// <summary>
    /// 设置任务的优先级
    /// </summary>
    public int Priority {
        get => _priority;
        set {
            _priority = value;
            Enable(TaskOptions.HAS_PRIORITY);
        }
    }

    #endregion
}
}