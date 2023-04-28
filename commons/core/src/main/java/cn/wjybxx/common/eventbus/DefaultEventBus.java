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

package cn.wjybxx.common.eventbus;

import cn.wjybxx.common.CollectionUtils;
import cn.wjybxx.common.pool.DefaultObjectPool;
import cn.wjybxx.common.pool.ObjectPool;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.NotThreadSafe;
import java.util.Collection;
import java.util.Map;
import java.util.Objects;

/**
 * 默认的EventBus，不支持{@link DynamicEvent#sourceKey()}
 *
 * @author wjybxx
 * date 2023/4/6
 */
@NotThreadSafe
public class DefaultEventBus implements EventBus {

    /**
     * eventKey -> handler
     * eventKey：{@link Class} 或 {@link ComposeEventKey}
     */
    private final Map<Object, EventHandler<?>> handlerMap;
    /** 事件key缓存池 */
    private final ObjectPool<ComposeEventKey> keyPool = new DefaultObjectPool<>(ComposeEventKey::new, ComposeEventKey::reset, 4, 8);
    /** 递归深度 - 防止死循环 */
    private int recursionDepth;

    public DefaultEventBus() {
        this(EventBusUtils.DEFAULT_EXPECTED_SIZE);
    }

    public DefaultEventBus(int expectedSize) {
        this.handlerMap = CollectionUtils.newHashMap(expectedSize);
    }

    @Override
    public final void post(@Nonnull Object event) {
        if (recursionDepth >= EventBusUtils.RECURSION_LIMIT) {
            throw new IllegalStateException("event had too many levels of nesting");
        }
        recursionDepth++;
        try {
            if (event instanceof DynamicEvent eventX) {
                final Object masterKey = eventX.masterKey();
                EventBusUtils.postEventImp(handlerMap, eventX, masterKey);

                final Object childKey = eventX.childKey();
                if (childKey != null) {
                    final ComposeEventKey composeEventKey = keyPool.get();
                    composeEventKey.init(masterKey, childKey);
                    EventBusUtils.postEventImp(handlerMap, (Object) eventX, composeEventKey);
                    keyPool.returnOne(composeEventKey);
                }
            } else {
                // 普通事件只支持class作为masterKey
                EventBusUtils.postEventImp(handlerMap, event, event.getClass());
            }
        } finally {
            recursionDepth--;
        }
    }

    @Override
    public void clear() {
        handlerMap.clear();
    }

    @Override
    public <T> void registerX(@Nonnull Object masterKey, @Nullable Object childKey, @Nonnull EventHandler<T> handler,
                              @Nullable Object customData) {
        Objects.requireNonNull(masterKey, "masterKey");
        Objects.requireNonNull(handler, "handler");
        if (childKey == null) {
            EventBusUtils.addHandlerImp(handlerMap, masterKey, handler);
        } else {
            if (CollectionUtils.isNotEmptyCollection(childKey)) {
                for (Object c : (Collection<?>) childKey) {
                    final ComposeEventKey key = new ComposeEventKey(masterKey, c);
                    EventBusUtils.addHandlerImp(handlerMap, key, handler);
                }
            } else {
                final ComposeEventKey key = new ComposeEventKey(masterKey, childKey);
                EventBusUtils.addHandlerImp(handlerMap, key, handler);
            }
        }
    }

    @Override
    public void removeX(@Nonnull Object masterKey, @Nullable Object childKey, @Nonnull EventHandler<?> handler) {
        Objects.requireNonNull(masterKey, "masterKey");
        Objects.requireNonNull(handler, "handler");
        if (childKey == null) {
            EventBusUtils.removeHandlerImp(handlerMap, masterKey, handler);
        } else {
            final ComposeEventKey composeEventKey = keyPool.get();
            if (CollectionUtils.isNotEmptyCollection(childKey)) {
                for (Object c : (Collection<?>) childKey) {
                    composeEventKey.init(masterKey, c);
                    EventBusUtils.removeHandlerImp(handlerMap, composeEventKey, handler);
                }
            } else {
                composeEventKey.init(masterKey, childKey);
                EventBusUtils.removeHandlerImp(handlerMap, composeEventKey, handler);
            }
            keyPool.returnOne(composeEventKey);
        }
    }

    @Override
    public boolean contains(@Nonnull Object masterKey, @Nullable Object childKey, @Nonnull EventHandler<?> handler) {
        if (childKey == null) {
            return EventBusUtils.containsImpl(handlerMap, masterKey, handler);
        } else {
            final ComposeEventKey composeEventKey = keyPool.get();
            composeEventKey.init(masterKey, childKey);
            boolean contains = EventBusUtils.containsImpl(handlerMap, composeEventKey, handler);
            keyPool.returnOne(composeEventKey);
            return contains;
        }
    }

    private static final class ComposeEventKey {

        private Object masterKey; // 运行时不为null
        private Object childKey;

        ComposeEventKey() {
        }

        ComposeEventKey(@Nonnull Object masterKey, @Nonnull Object childKey) {
            this.masterKey = masterKey;
            this.childKey = childKey;
        }

        void init(@Nonnull Object masterKey, Object childKey) {
            this.masterKey = masterKey;
            this.childKey = childKey;
        }

        void reset() {
            this.masterKey = null;
            this.childKey = null;
        }

        @Override
        public boolean equals(final Object o) {
            if (this == o) {
                return true;
            }

            if (o == null || o.getClass() != ComposeEventKey.class) {
                return false;
            }

            final ComposeEventKey that = (ComposeEventKey) o;
            return Objects.equals(masterKey, that.masterKey)
                    && Objects.equals(childKey, that.childKey);
        }

        @Override
        public int hashCode() {
            return 31 * masterKey.hashCode() + childKey.hashCode();
        }

        @Override
        public String toString() {
            return "ComposeEventKey{" +
                    "parentType=" + masterKey +
                    ", childType=" + childKey +
                    '}';
        }
    }
}