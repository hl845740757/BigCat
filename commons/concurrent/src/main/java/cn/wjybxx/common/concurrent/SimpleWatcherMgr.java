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

import cn.wjybxx.common.ThreadUtils;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.ThreadSafe;
import java.util.Objects;

/**
 * 一个简单的Watcher管理器
 * 由于多用在多线程环境下，因此提供了缓存行填充特性
 *
 * @author wjybxx
 * date 2023/4/6
 */
@ThreadSafe
public final class SimpleWatcherMgr<E> implements WatcherMgr<E> {

    private static final Logger logger = LoggerFactory.getLogger(SimpleWatcherMgr.class);

    @SuppressWarnings("unused")
    private long p1, p2, p3, p4, p5, p6, p7, p8;

    /** 常见方案：synchronized写，volatile读 */
    private volatile Watcher<? super E> watcher;

    @SuppressWarnings("unused")
    private long p9, p10, p11, p12, p13, p14, p15, p16;

    public Watcher<? super E> getWatcher() {
        return watcher;
    }

    @Override
    public synchronized void watch(Watcher<? super E> watcher) {
        this.watcher = Objects.requireNonNull(watcher);
    }

    @Override
    public synchronized boolean cancelWatch(Watcher<?> watcher) {
        if (watcher != null && watcher == this.watcher) {
            this.watcher = null;
            return true;
        }
        return false;
    }

    /**
     * @return 如果事件被消费了则返回true，否则返回false
     */
    @Override
    public boolean onEvent(@Nonnull E event) {
        Watcher<? super E> watcher = this.watcher;
        if (watcher == null) {
            return false;
        }
        // 取消成功才处理事件，考虑竞争的情况
        // 取消失败，证明当前监听器失效；但取消成功，不能证明当前监听器有效！目标线程可能已醒来，正准备取消监听器
        boolean r = false;
        try {
            if (watcher.test(event) && cancelWatch(watcher)) {
                r = true;
                watcher.onEvent(event);
            }
        } catch (Throwable ex) {
            ThreadUtils.recoveryInterrupted(ex);
            if (!r) {
                logger.error("Fatal Error! watcher.test caught exception", ex);
            } else if (ex instanceof VirtualMachineError) {
                logger.error("watcher.onEvent caught exception", ex);
            } else {
                logger.warn("watcher.onEvent caught exception", ex);
            }
        }
        return r;
    }

}