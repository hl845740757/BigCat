/*
 * Copyright 2023 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.common.time;

/**
 * 频率调节器 - 可理解为轮询式Timer调度器。
 * 它使用Timer的调度算法，但Timer是回调式的，Regulator是轮询式的。
 *
 * @author wjybxx
 * date 2023/4/4
 */
public class Regulator {

    /** 仅执行一次 */
    private static final int ONCE = 0;
    /** 可变帧率 -- fixedDelay的间隔是前次任务的结束与下次任务的开始 */
    private static final int FIX_DELAY = 1;
    /** 固定帧率 */
    private static final int FIX_RATE = 2;

    /** 其实可以省略，根据{@link #period}的正负判断，但为了提高可读性，还是不那么做 */
    private final int type;
    /** 首次执行延迟 */
    private long firstDelay;
    /** 后期执行延迟 */
    private long period;

    /**
     * 它的真实含义取决于外部，可能是系统时间也可能不是，甚至可能是帧数
     * 在存储已触发次数的情况下还使用上次更新的时间戳，这使得可以在运行的过程中修改间隔，该实现是有状态的。
     * 注意：允许为负数，外部赋值什么就是什么。
     */
    private long lastUpdateTime;
    /** 两次执行之间的间隔 */
    private long deltaTime;
    /** 已触发次数 */
    private int count;

    /** 关联的上下文 */
    private Object context;

    private Regulator(int type, long firstDelay, long period, long lastUpdateTime) {
        this.type = type;
        this.firstDelay = firstDelay;
        this.period = period;

        this.lastUpdateTime = lastUpdateTime;
        this.deltaTime = 0;
        this.count = 0;
    }

    private static long checkFirstDelay(int type, long firstDelay) {
        if (type == FIX_RATE && firstDelay < 0) {
            throw new IllegalArgumentException("the firstDelay of fixRate must gte 0, delay " + firstDelay);
        }
        return firstDelay;
    }

    private static long checkPeriod(long period) {
        if (period <= 0) {
            throw new IllegalArgumentException("period must be positive, period " + period);
        }
        return period;
    }

    /**
     * @param firstDelay 首次执行延迟
     */
    public static Regulator newOnce(long firstDelay) {
        return new Regulator(ONCE, firstDelay, 0, 0);
    }

    /**
     * @param firstDelay 首次执行延迟
     * @param period     触发间隔
     * @return 按固定延迟更新的调节器，它保证的是两次执行的间隔大于更新间隔
     */
    public static Regulator newFixedDelay(long firstDelay, long period) {
        checkPeriod(period);
        return new Regulator(FIX_DELAY, firstDelay, period, 0);
    }

    /**
     * @param firstDelay 首次执行延迟
     * @param period     触发间隔
     * @return 按固定频率更新的调节器，它尽可能的保证总运行次数
     */
    public static Regulator newFixedRate(long firstDelay, long period) {
        checkPeriod(period);
        checkFirstDelay(FIX_RATE, firstDelay);
        return new Regulator(FIX_RATE, firstDelay, period, 0);
    }

    /**
     * 重新启动调节器
     * （没有单独的start方法，因为逻辑无区别）
     *
     * @param curTime 当前系统时间
     * @return this
     */
    public Regulator restart(long curTime) {
        lastUpdateTime = curTime;
        deltaTime = 0;
        count = 0;
        return this;
    }

    /**
     * @param curTime 当前系统时间
     * @return 如果应该执行一次update或者tick，则返回true，否则返回false
     */
    public boolean isReady(long curTime) {
        boolean ready = count == 0
                ? (curTime - lastUpdateTime >= firstDelay)
                : (period > 0 && (curTime - lastUpdateTime >= period));
        if (ready) {
            internalUpdate(curTime);
            return true;
        }
        return false;
    }

    /**
     * 强制使用当前时间更新调节器。
     *
     * @param curTime 当前系统时间
     * @return deltaTime
     */
    public long forceReady(long curTime) {
        internalUpdate(curTime);
        return deltaTime;
    }

    private void internalUpdate(long curTime) {
        if (type == FIX_DELAY || type == ONCE) {
            // 使用真实时间计算，但deltaTime也不能小于0
            deltaTime = Math.max(0, curTime - lastUpdateTime);
            lastUpdateTime = curTime;
        } else {
            // 固定频率执行时，一切都是逻辑时间
            if (count == 0) {
                deltaTime = firstDelay;
                lastUpdateTime += firstDelay;
            } else {
                deltaTime = period;
                lastUpdateTime += period;
            }
        }
        count++;
    }

    /** 获取下次执行的延迟 */
    public long getDelay(long curTime) {
        if (count == 0) {
            return Math.max(0, curTime - lastUpdateTime - firstDelay);
        }
        return Math.max(0, curTime - lastUpdateTime - period);
    }

    /** 校准时间 */
    public void correctTime(long curTime) {
        this.lastUpdateTime = curTime;
    }

    public long getFirstDelay() {
        return firstDelay;
    }

    public void setFirstDelay(long firstDelay) {
        this.firstDelay = checkFirstDelay(this.type, firstDelay);
    }

    public long getPeriod() {
        return period;
    }

    public void setPeriod(long period) {
        this.period = checkPeriod(period);
    }

    /** @return 如果是周期性任务则返回true */
    public boolean isPeriodic() {
        return period != 0;
    }

    /**
     * 它的具体含义取悦于更新时使用的{@code curTime}的含义。
     */
    public long getLastUpdateTime() {
        return lastUpdateTime;
    }

    /**
     * 如果是固定频率的调节器，应该使用{@link #getPeriod()}获取更新间隔。
     * 另外，返回值可能大于{@link #getPeriod()}（其实可以设定最大返回值的，暂时没支持）
     *
     * @return 两次逻辑帧之间的间隔。
     */
    public long getDeltaTime() {
        return deltaTime;
    }

    /**
     * 触发次数
     */
    public int getCount() {
        return count;
    }

    /** 任务关联的上下文 */
    public Object getContext() {
        return context;
    }

    public Regulator setContext(Object context) {
        this.context = context;
        return this;
    }
}