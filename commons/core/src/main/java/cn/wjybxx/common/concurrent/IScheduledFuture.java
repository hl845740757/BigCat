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

import java.util.concurrent.Delayed;
import java.util.concurrent.ScheduledFuture;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date 2023/4/9
 */
public interface IScheduledFuture<V> extends ICompletableFuture<V>, ScheduledFuture<V> {

    /**
     * 是否是周期性任务
     */
    boolean isPeriodic();

    /**
     * {@inheritDoc}
     * 注意：该接口不对用户保证可见性，通常是无意义的；因为这是以消费者的时间轴计算的，在多线程/分布式下，服务方的时间与本地时间不能完全一致。
     */
    @Override
    long getDelay(TimeUnit unit);

    /**
     * {@inheritDoc}
     * 注意：用户最好不要比较两个Future的大小，会涉及两个多线程对象之间的比较，不保证用户比较结果的稳定性
     */
    @Override
    int compareTo(Delayed o);

}