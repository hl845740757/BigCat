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

package cn.wjybxx.common.rpc;

import cn.wjybxx.common.concurrent.SimpleWatcherMgr;
import cn.wjybxx.common.concurrent.WatcherMgr;

import javax.annotation.concurrent.ThreadSafe;
import java.util.Objects;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date 2023/4/5
 */
@ThreadSafe
class SimpleEventQueue<E> {

    /** 无界队列，避免死锁 */
    private final LinkedBlockingQueue<E> blockingQueue = new LinkedBlockingQueue<>(10);
    private final SimpleWatcherMgr<E> watcherMgr = new SimpleWatcherMgr<>();

    public void watch(WatcherMgr.Watcher<? super E> watcher) {
        watcherMgr.watch(watcher);
    }

    public boolean cancelWatch(WatcherMgr.Watcher<?> watcher) {
        return watcherMgr.cancelWatch(watcher);
    }

    public boolean offer(E event) {
        Objects.requireNonNull(event);
        if (watcherMgr.onEvent(event)) {
            return true;
        }
        return blockingQueue.offer(event);
    }

    public E poll(long timeout, TimeUnit unit) throws InterruptedException {
        return blockingQueue.poll(timeout, unit);
    }

    public E poll() {
        return blockingQueue.poll();
    }

}