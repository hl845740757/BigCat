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

import javax.annotation.concurrent.NotThreadSafe;

/**
 * 增量式计时器，需要外部每帧调用{@link #update(long)}累积时间
 *
 * @author wjybxx
 * date 2023/4/4
 */
@NotThreadSafe
public interface Timepiece extends CachedTimeProvider {

    @Override
    long getTime();

    /**
     * （这其实只是个工具方法，提供在底层，是因为使用的较为普遍）
     *
     * @return 当前帧和前一帧之间的时间跨度（毫秒）
     */
    long getDeltaTime();

    /**
     * 累加时间
     *
     * @param deltaTime 时间增量（毫秒），如果该值小于0，则会被修正为0
     */
    void update(long deltaTime);

    /**
     * 设置当前时间
     *
     * @param curTime 当前时间
     */
    @Override
    void setTime(long curTime);

    /**
     * 在不修改当前时间戳的情况下修改deltaTime
     * （仅仅用在补偿的时候，慎用）
     *
     * @param deltaTime 必须大于等于0，且不超过累积时间
     */
    void setDeltaTime(long deltaTime);

    /**
     * 重新启动计时器
     *
     * @param curTime   不可小于0
     * @param deltaTime 时间间隔
     */
    void restart(long curTime, long deltaTime);

    /**
     * 重新启动计时 - 累积时间和deltaTime都清零。
     */
    default void restart() {
        restart(0, 0);
    }

    /**
     * 重新启动计时器 - 累积时间设定为给定值，deltaTime设定为0。
     */
    default void restart(long curTime) {
        restart(curTime, 0);
    }

}