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

package cn.wjybxx.common.concurrent;

/**
 * 任务的特征值
 *
 * @author wjybxx
 * date 2023/4/14
 */
public enum TaskFeature {

    /**
     * 是否是低优先级的任务
     * 如果是低优先级的任务，可以被延迟调度，不保证执行的时机，更不能保证时序
     *
     * @apiNote EventLoop可以不支持该特性，低优先任务延迟是可选优化项
     */
    LOW_PRIORITY(false),

    /**
     * 周期性任务的优先级是否可降低
     * 默认情况下一个周期性任务是不降低优先级的，如果周期性任务是可以降低优先级的，可开启选项
     *
     * @apiNote EventLoop可以不支持该特性，降级是可选优化项
     */
    PRIORITY_DECREASABLE(false),

    /**
     * 本地序（可以与其它线程无序）
     * 对于EventLoop内部的任务，如果EventLoop的定时任务是无界的，该属性还可以避免阻塞或死锁。
     */
    LOCAL_ORDER(false),

    /**
     * 捕获{@link Exception}类异常
     * 在出现异常后继续执行；只适用无需结果的周期性任务；
     */
    CAUGHT_EXCEPTION(false),
    /**
     * 捕获{@link Throwable}类异常，即所有的异常
     */
    CAUGHT_THROWABLE(false),

    /**
     * 在执行任务执行必须先处理一次定时任务队列
     * EventLoop收到具有该特征的任务时，需要更新时间戳，尝试执行该任务之前的所有定时任务。
     */
    SCHEDULE_BARRIER(false),
    ;

    private final boolean _defaultState;
    private final int _mask;

    TaskFeature(boolean defaultState) {
        _defaultState = defaultState;
        _mask = (1 << ordinal());
    }

    public boolean enabledByDefault() {
        return _defaultState;
    }

    public int getMask() {
        return _mask;
    }

    public boolean enabledIn(int flags) {
        return (flags & _mask) == _mask;
    }

    public int setEnable(int flags, boolean enable) {
        if (enable) {
            return (flags | _mask);
        } else {
            return (flags & ~_mask);
        }
    }

    /** 默认开启的flags */
    public static final int defaultFlags;

    static {
        int flag = 0;
        for (TaskFeature feature : values()) {
            flag = feature.setEnable(flag, feature._defaultState);
        }
        defaultFlags = flag;
    }

}