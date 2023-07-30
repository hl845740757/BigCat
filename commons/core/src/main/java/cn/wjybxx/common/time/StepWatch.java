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

import cn.wjybxx.common.annotation.Beta;
import org.apache.commons.lang3.time.StopWatch;

import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

/**
 * 步进表 -- 用于监测每一步的耗时和总耗时。
 * 示例：
 * <pre>{@code
 *  public void execute() {
 *      // 创建一个已启动的计时器
 *      final StepWatch stepWatch = StepWatch.createStarted("execute");
 *
 *      doSomethingA();
 *      stepWatch.logStep("step1");
 *
 *      doSomethingB();
 *      stepWatch.logStep("step2");
 *
 *      doSomethingC();
 *      stepWatch.logStep("step3");
 *
 *      doSomethingD();
 *      stepWatch.logStep("step4");
 *
 *      // 输出日志
 *      logger.info(stepWatch.getLog());
 *  }
 * }
 * </pre>
 *
 * @author wjybxx
 * date 2023/4/4
 */
public class StepWatch {

    private final String name;
    private final StopWatch delegate = new StopWatch();
    private final List<Item> itemList = new ArrayList<>(8);
    private final StringBuilder sb = new StringBuilder(64);

    /**
     * @param name 推荐命名格式{@code ClassName:MethodName}
     */
    public StepWatch(String name) {
        this.name = Objects.requireNonNull(name, "name");
    }

    /**
     * @return 一个已启动的计时器
     */
    public static StepWatch createStarted(String name) {
        final StepWatch sw = new StepWatch(name);
        sw.start();
        return sw;
    }

    public boolean isStarted() {
        return delegate.isStarted();
    }

    public boolean isSuspended() {
        return delegate.isSuspended();
    }

    public boolean isStopped() {
        return delegate.isStopped();
    }

    /**
     * 开始计时。
     * 重复调用start之前，必须调用{@link #reset()}
     *
     * @return this
     */
    public StepWatch start() {
        delegate.start();
        return this;
    }

    /**
     * 记录该步骤的耗时
     *
     * @param stepName 该步骤的名称
     */
    public void logStep(String stepName) {
        if (itemList.isEmpty()) {
            itemList.add(new Item(stepName, delegate.getTime()));
        } else {
            itemList.add(new Item(stepName, delegate.getTime() - delegate.getSplitTime()));
        }
        delegate.split();
    }

    /** 暂停计时 */
    public void suspend() {
        delegate.suspend();
    }

    /** 恢复计时 */
    public void resume() {
        delegate.resume();
    }

    /**
     * 如果希望停止计时，则调用该方法。
     * 停止计时后，{@link #getCostTime()}将获得一个稳定的时间值。
     */
    public void stop() {
        if (delegate.isStopped()) {
            return;
        }
        delegate.stop();
    }

    /**
     * 注意：为了安全起见，请要么在代码的开始重置，要么在finally块中重置。
     */
    public void reset() {
        delegate.reset();
        itemList.clear();
        sb.setLength(0);
    }

    /**
     * {@link #reset()}和{@link #start()}的快捷方法
     */
    public void restart() {
        reset();
        start();
    }

    /**
     * 获取启动定时器时的时间戳
     */
    public long getStartTime() {
        return delegate.getStartTime();
    }

    /**
     * 获取消耗的总时间
     * 如果尚未stop，则返回从start到当前的已消耗的时间。
     * 如果已经stop，则返回从start到stop时消耗的时间。
     * 注意：总时长不一定等于各步骤耗时，在最后一个步骤与获取日志之间存在误差。
     */
    public long getCostTime() {
        return delegate.getTime();
    }

    /**
     * 合并另一个步进表记录的步骤耗时信息
     */
    @Beta
    public StepWatch mergeSteps(StepWatch src) {
        itemList.addAll(src.itemList);
        return this;
    }

    /**
     * 获取按照时间消耗排序后的log。
     * 注意：可以在不调用{@link #stop()}的情况下调用该方法。
     * (获得了一个规律，也失去了一个规律，可能并不如未排序的log看着舒服)
     */
    public String getSortedLog() {
        // 排序开销还算比较小
        itemList.sort(null);
        return toString();
    }

    /**
     * 获取最终log。
     */
    public String getLog() {
        return toString();
    }

    /**
     * 格式: StepWatch[name={name}ms][a={a}ms,b={b}ms...]
     * 1. StepWatch为标记，方便检索。
     * 2. {@code {x}}表示x的耗时。
     * 3. 前半部分为总耗时，后半部分为各步骤耗时。
     * <p>
     * Q: 为什么重写{@code toString}？
     * A: 在输出日志的时候，我们可能常常使用占位符，那么延迟构建内容就是必须的，这要求我们实现{@code toString()}。
     */
    @Override
    public String toString() {
        final StringBuilder sb = this.sb;
        final List<Item> itemList = this.itemList;
        // 总耗时
        sb.append("StepWatch[").append(name).append('=').append(delegate.getTime()).append("ms]");
        // 每个步骤耗时
        sb.append('[');
        for (int i = 0; i < itemList.size(); i++) {
            final Item item = itemList.get(i);
            if (i > 0) {
                sb.append(',');
            }
            sb.append(item.stepName).append('=').append(item.costTimeMs).append("ms");
        }
        sb.append(']');
        return sb.toString();
    }

    private static class Item implements Comparable<Item> {

        final String stepName;
        final long costTimeMs;

        Item(String stepName, long costTimeMs) {
            this.stepName = stepName;
            this.costTimeMs = costTimeMs;
        }

        @Override
        public int compareTo(Item that) {
            final int timeCompareResult = Long.compare(costTimeMs, that.costTimeMs);
            if (timeCompareResult != 0) {
                // 时间逆序
                return -1 * timeCompareResult;
            } else {
                // 字母自然序
                return stepName.compareTo(that.stepName);
            }
        }
    }

}