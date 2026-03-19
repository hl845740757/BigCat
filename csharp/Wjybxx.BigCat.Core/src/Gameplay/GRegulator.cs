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
using Wjybxx.Commons.Time;

namespace Wjybxx.BigCat.Gameplay
{
/// <summary>
/// 频率调节器 - 可理解为轮询式Timer调度器。
/// 它使用Timer的调度算法，但Timer是回调式的，Regulator是轮询式的。
///
/// 该实现是<see cref="Regulator"/>的特化实现，时间是double类型，类型是值类型。
/// </summary>
public struct GRegulator
{
#nullable disable
    /** 仅执行一次 */
    private const int ONCE = 0;
    /** 可变帧率 -- fixedDelay的间隔是前次任务的结束与下次任务的开始 */
    private const int FIX_DELAY = 1;
    /** 固定帧率 */
    private const int FIX_RATE = 2;

    /** 其实可以省略，根据{@link #period}的正负判断，但为了提高可读性，还是不那么做 */
    private readonly int type;
    /** 首次执行延迟 */
    private float firstDelay;
    /** 后期执行延迟 */
    private float period;

    /**
     * 它的真实含义取决于外部，可能是系统时间也可能不是，甚至可能是帧数
     * 在存储已触发次数的情况下还使用上次更新的时间戳，这使得可以在运行的过程中修改间隔，该实现是有状态的。
     * 注意：允许为负数，外部赋值什么就是什么。
     */
    private float triggerTime;
    /** 两次执行之间的间隔 */
    private float deltaTime;
    /** 已触发次数 */
    private int triggerCount;

    /** 关联的上下文 -- 用于避免额外的映射 */
    private object context;

    private GRegulator(byte type, float firstDelay, float period, float triggerTime) {
        this.type = type;
        this.firstDelay = firstDelay;
        this.period = period;

        this.triggerTime = triggerTime;
        this.deltaTime = 0;
        this.triggerCount = 0;
        this.context = null;
    }

    private static float CheckFirstDelay(byte type, float firstDelay) {
        if (type == FIX_RATE && firstDelay < 0) {
            throw new ArgumentException("the firstDelay of fixRate must gte 0, delay " + firstDelay);
        }
        return firstDelay;
    }

    private static float CheckPeriod(float period) {
        if (period <= 0) {
            throw new ArgumentException("period must be positive, period " + period);
        }
        return period;
    }

    /// <summary>
    /// 创建一个指向一次的Regulator
    /// </summary>
    /// <param name="firstDelay">首次执行延迟</param>
    /// <returns></returns>
    public static GRegulator NewOnce(float firstDelay) {
        return new GRegulator(ONCE, firstDelay, 0, 0);
    }

    /// <summary>
    /// 创建一个按固定延迟更新的调节器，它保证的是两次执行的间隔大于更新间隔
    /// </summary>
    /// <param name="firstDelay">首次执行延迟</param>
    /// <param name="period">触发间隔</param>
    /// <returns></returns>
    public static GRegulator NewFixedDelay(float firstDelay, float period) {
        CheckPeriod(period);
        return new GRegulator(FIX_DELAY, firstDelay, period, 0);
    }

    /// <summary>
    /// 创建一个按固定频率更新的调节器，它尽可能的保证总运行次数
    /// </summary>
    /// <param name="firstDelay">首次执行延迟</param>
    /// <param name="period">触发间隔</param>
    /// <returns></returns>
    public static GRegulator NewFixedRate(float firstDelay, float period) {
        CheckPeriod(period);
        CheckFirstDelay(FIX_RATE, firstDelay);
        return new GRegulator(FIX_RATE, firstDelay, period, 0);
    }

    /// <summary>
    /// 重新启动调节器
    /// （没有单独的start方法，因为逻辑无区别）
    /// </summary>
    /// <param name="curTime">当前时间</param>
    /// <returns>this</returns>
    public void Restart(float curTime) {
        triggerTime = curTime;
        deltaTime = 0;
        triggerCount = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curTime">当前时间</param>
    /// <returns>如果应该执行一次update或者tick，则返回true，否则返回false</returns>
    public bool IsReady(float curTime) {
        bool ready = triggerCount == 0
            ? (curTime - triggerTime >= firstDelay)
            : (period > 0 && (curTime - triggerTime >= period));
        if (ready) {
            InternalUpdate(curTime);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 强制使用当前时间更新调节器
    /// </summary>
    /// <param name="curTime">当前时间</param>
    /// <returns>deltaTime</returns>
    public float ForceReady(float curTime) {
        InternalUpdate(curTime);
        return deltaTime;
    }

    private void InternalUpdate(float curTime) {
        if (type == FIX_DELAY || type == ONCE) {
            // 使用真实时间计算，但deltaTime也不能小于0
            deltaTime = Math.Max(0, curTime - triggerTime);
            triggerTime = curTime;
        } else {
            // 固定频率执行时，一切都是逻辑时间
            if (triggerCount == 0) {
                deltaTime = firstDelay;
                triggerTime += firstDelay;
            } else {
                deltaTime = period;
                triggerTime += period;
            }
        }
        triggerCount++;
    }

    /// <summary>
    /// 获取下次执行的延迟
    /// </summary>
    /// <param name="curTime">当前时间</param>
    /// <returns></returns>
    public float GetDelay(float curTime) {
        if (triggerCount == 0) {
            return Math.Max(0, curTime - triggerTime - firstDelay);
        }
        return Math.Max(0, curTime - triggerTime - period);
    }

    /// <summary>
    /// 校准时间
    /// </summary>
    /// <param name="curTime"></param>
    public void CorrectTime(float curTime) {
        this.triggerTime = curTime;
    }

    /// <summary>
    /// 如果是周期性任务则返回true
    /// </summary>
    public bool IsPeriodic => period != 0;

    /// <summary>
    /// 获取上次成功更新的时间戳
    /// 它的具体含义取悦于更新时使用的{@code curTime}的含义。
    /// </summary>
    /// <value></value>
    public float TriggerTime => triggerTime;

    /// <summary>
    /// 如果是固定频率的调节器，应该使用<see cref="Period"/>获取更新间隔。
    /// 另外，返回值可能大于{@link #getPeriod()}（其实可以设定最大返回值的，暂时没支持）
    /// </summary>
    /// <value>两次逻辑帧之间的间隔</value>
    public float DeltaTime => deltaTime;

    /// <summary>
    /// 已触发次数
    /// </summary>
    public int TriggerCount => triggerCount;

    /// <summary>
    /// 任务关联的上下文
    /// </summary>
    public object Context {
        get => context;
        set => context = value;
    }

    /// <summary>
    /// 任务的首次的执行延迟
    /// </summary>
    public float FirstDelay {
        get => firstDelay;
        set => firstDelay = value;
    }

    /// <summary>
    /// 任务的执行间隔
    /// </summary>
    public float Period {
        get => period;
        set => period = CheckPeriod(value);
    }
}
}