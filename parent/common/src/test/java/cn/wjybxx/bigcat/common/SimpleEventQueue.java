/*
 * Copyright 2023 wjybxx
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

package cn.wjybxx.bigcat.common;

import cn.wjybxx.bigcat.common.concurrent.WatchableEventQueue;

import javax.annotation.concurrent.ThreadSafe;
import java.util.Objects;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date 2023/4/5
 */
@ThreadSafe
class SimpleEventQueue<E> implements WatchableEventQueue<E> {

    /** 无界队列，避免死锁 */
    private final LinkedBlockingQueue<E> blockingQueue = new LinkedBlockingQueue<>(10);
    /** 常见方案：synchronized写，volatile读 */
    private volatile Watcher<? super E> watcher;

    @Override
    public synchronized void watch(Watcher<? super E> watcher) {
        this.watcher = Objects.requireNonNull(watcher);
    }

    @Override
    public synchronized void cancelWatch(Watcher<?> watcher) {
        if (watcher == this.watcher) {
            this.watcher = null;
        }
    }

    public boolean offer(E e) {
        Objects.requireNonNull(e);
        Watcher<? super E> watcher = this.watcher;
        if (watcher != null && watcher.test(e)) {
            cancelWatch(watcher); // 不要直接修改值，避免竞争错误
            watcher.onEvent(e);
            return true;
        }
        return blockingQueue.offer(e);
    }

    public E poll(long timeout, TimeUnit unit) throws InterruptedException {
        return blockingQueue.poll(timeout, unit);
    }

    public E poll() {
        return blockingQueue.poll();
    }

}