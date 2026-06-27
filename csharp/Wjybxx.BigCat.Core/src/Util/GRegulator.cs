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
using System.Runtime.InteropServices;
using Wjybxx.Commons.Time;

namespace Wjybxx.BigCat.Util
{
/// <summary>
/// 频率调节器 - 可理解为轮询式Timer调度器。
/// 它使用Timer的调度算法，但Timer是回调式的，Regulator是轮询式的。
///
/// 该实现是<see cref="Regulator"/>的特化实现，时间是double类型，类型是值类型。
/// </summary>
[StructLayout(LayoutKind.Auto)]
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
    private double firstDelay;
    /** 后期执行延迟 */
    private double period;

    /**
     * 上次成功更新的时间戳
     * 它的真实含义取决于外部，可能是系统时间也可能不是，甚至可能是帧数
     * 在存储已触发次数的情况下还使用上次更新的时间戳，这使得可以在运行的过程中修改间隔，该实现是有状态的。
     * 注意：允许为负数，外部赋值什么就是什么。
     */
    private double triggerTime;
    /** 两次执行之间的间隔 */
    private double deltaTime;
    /** 已触发次数 */
    private int triggerCount;

    /** 关联的上下文 -- 用于避免额外的映射 */
    private object context;

    private GRegulator(byte type, double firstDelay, double period, double triggerTime) {
        this.type = type;
        this.firstDelay = firstDelay;
        this.period = period;

        this.triggerTime = triggerTime;
        this.deltaTime = 0;
        this.triggerCount = 0;
        this.context = null;
    }

    private static double CheckFirstDelay(byte type, double firstDelay) {
        if (type == FIX_RATE && firstDelay < 0) {
            throw new ArgumentException("the firstDelay of fixRate must gte 0, delay " + firstDelay);
        }
        return firstDelay;
    }

    private static double CheckPeriod(double period) {
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
    public static GRegulator NewOnce(double firstDelay) {
        return new GRegulator(ONCE, firstDelay, 0, 0);
    }

    /// <summary>
    /// 创建一个按固定延迟更新的调节器，它保证的是两次执行的间隔大于更新间隔
    /// </summary>
    /// <param name="firstDelay">首次执行延迟</param>
    /// <param name="period">触发间隔</param>
    /// <returns></returns>
    public static GRegulator NewFixedDelay(double firstDelay, double period) {
        CheckPeriod(period);
        return new GRegulator(FIX_DELAY, firstDelay, period, 0);
    }

    /// <summary>
    /// 创建一个按固定频率更新的调节器，它尽可能的保证总运行次数
    /// </summary>
    /// <param name="firstDelay">首次执行延迟</param>
    /// <param name="period">触发间隔</param>
    /// <returns></returns>
    public static GRegulator NewFixedRate(double firstDelay, double period) {
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
    public void Restart(double curTime) {
        triggerTime = curTime;
        deltaTime = 0;
        triggerCount = 0;
    }

    /// <summary>
    /// 获取下次执行的延迟
    /// </summary>
    /// <param name="curTime">当前时间</param>
    /// <returns></returns>
    public double GetDelay(double curTime) {
        double nextTime = triggerCount == 0
            ? triggerTime + firstDelay
            : triggerTime + period;
        return Math.Max(0, nextTime - curTime);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="curTime">当前时间</param>
    /// <returns>如果应该执行一次update或者tick，则返回true，否则返回false</returns>
    public bool IsReady(double curTime) {
        if (type == ONCE && triggerCount > 0) {
            return false;
        }
        double nextTime = triggerCount == 0
            ? triggerTime + firstDelay
            : triggerTime + period;
        if (curTime >= nextTime) {
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
    public double ForceReady(double curTime) {
        InternalUpdate(curTime);
        return deltaTime;
    }

    private void InternalUpdate(double curTime) {
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
    /// 校准时间
    /// </summary>
    /// <param name="curTime"></param>
    public void CorrectTime(double curTime) {
        this.triggerTime = curTime;
    }

    /// <summary>
    /// 如果是周期性任务则返回true
    /// </summary>
    public bool IsPeriodic => period != 0;

    /// <summary>
    /// 上次成功更新的时间戳
    /// 它的具体含义取悦于更新时使用的{@code curTime}的含义。
    /// </summary>
    /// <value></value>
    public double TriggerTime => triggerTime;

    /// <summary>
    /// 如果是固定频率的调节器，应该使用<see cref="Period"/>获取更新间隔。
    /// 另外，返回值可能大于{@link #getPeriod()}（其实可以设定最大返回值的，暂时没支持）
    /// </summary>
    /// <value>两次逻辑帧之间的间隔</value>
    public double DeltaTime => deltaTime;

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
    public double FirstDelay {
        get => firstDelay;
        set => firstDelay = value;
    }

    /// <summary>
    /// 任务的执行间隔
    /// </summary>
    public double Period {
        get => period;
        set => period = CheckPeriod(value);
    }
}
}