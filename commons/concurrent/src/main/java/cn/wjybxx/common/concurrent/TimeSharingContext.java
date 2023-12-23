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

import cn.wjybxx.common.annotation.Internal;

/**
 * @author wjybxx
 * date 2023/4/12
 */
@Internal
public final class TimeSharingContext {

    /** 剩余时间 */
    private long timeLeft;
    /** 上次触发时间，用于固定延迟下计算deltaTime */
    private long lastTriggerTime;

    public TimeSharingContext(long timeout, long timeCreate) {
        this.timeLeft = timeout;
        this.lastTriggerTime = timeCreate;
    }

    /**
     * @param realTriggerTime  真实触发时间 -- 真正被调度的时间
     * @param logicTriggerTime 逻辑触发时间 -- 调度前计算的应该被调度的时间
     * @param period           触发周期 大于0为fixedRate
     */
    public void beforeCall(long realTriggerTime, long logicTriggerTime, long period) {
        if (period > 0) {
            timeLeft -= logicTriggerTime - lastTriggerTime;
            lastTriggerTime = logicTriggerTime;
        } else {
            timeLeft -= realTriggerTime - lastTriggerTime;
            lastTriggerTime = realTriggerTime;
        }
    }

    public long getTimeLeft() {
        return timeLeft;
    }

    public boolean isTimeout() {
        return timeLeft <= 0;
    }

}