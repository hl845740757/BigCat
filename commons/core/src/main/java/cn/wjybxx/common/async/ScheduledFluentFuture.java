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

package cn.wjybxx.common.async;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public interface ScheduledFluentFuture<E> extends FluentFuture<E> {

    /**
     * 任务距离下次被调度还有多少时间；时间单位与Executor有关，通常是毫秒
     *
     * @return 如果返回值小于等于0，表示已超过期望的执行时间
     */
    long getDelay();

}