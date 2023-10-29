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

package cn.wjybxx.common.eventbus;

import cn.wjybxx.common.CollectionUtils;
import cn.wjybxx.common.pool.DefaultObjectPool;
import cn.wjybxx.common.pool.ObjectPool;

import javax.annotation.Nullable;
import java.util.Collection;
import java.util.Map;
import java.util.Objects;

/**
 * 支持{@link DynamicEvent#sourceKey()}
 *
 * @author wjybxx
 * date 2023/4/6
 */
public class DefaultEventBusX implements EventBus {

    /** 现在的实现，开销还是比较高的，多了一层封装，hash和equals的开销变多了 */
    private final Map<ComposeEventKey, EventHandler<?>> handlerMap;
    /** 事件key缓存池 */
    private final ObjectPool<ComposeEventKey> keyPool = new DefaultObjectPool<>(ComposeEventKey::new, ComposeEventKey::reset, 4, 8);
    /** 递归深度 - 防止死循环 */
    private int recursionDepth;

    public DefaultEventBusX() {
        this(EventBusUtils.DEFAULT_EXPECTED_SIZE);
    }

    public DefaultEventBusX(int expectedSize) {
        this.handlerMap = CollectionUtils.newHashMap(expectedSize);
    }

    @Override
    public final void post(Object event) {
        if (recursionDepth >= EventBusUtils.RECURSION_LIMIT) {
            throw new IllegalStateException("event had too many levels of nesting");
        }

        recursionDepth++;
        try {
            final ComposeEventKey key = keyPool.get();
            if (event instanceof DynamicEvent eventX) {
                key.sourceKey = eventX.sourceKey();
                key.masterKey = eventX.masterKey();
                // 先以sourceKey + masterKey派发一次
                EventBusUtils.postEvent(handlerMap, event, key);

                // 如果存在childKey，再以全部key派发一次
                final Object childKey = eventX.childKey();
                if (childKey != null) {
                    key.childKey = childKey;
                    EventBusUtils.postEvent(handlerMap, eventX, key);
                }
            } else {
                // 普通事件只支持class作为masterKey
                key.masterKey = event.getClass();
                EventBusUtils.postEvent(handlerMap, event, key);
            }
            keyPool.returnOne(key);
        } finally {
            recursionDepth--;
        }
    }

    @Override
    public <T> void registerX(Object masterKey, @Nullable Object childKey, EventHandler<T> handler, @Nullable Object customData) {
        Objects.requireNonNull(masterKey, "masterKey");
        Objects.requireNonNull(handler, "handler");

        // 处理MasterKeyX扩展，由于要支持MasterKeyX，所有的事件Key都需要使用ComposeKey
        Object sourceKey;
        if (masterKey instanceof MasterKeyX masterKeyX) {
            sourceKey = masterKeyX.getSourceKey();
            masterKey = masterKeyX.getMasterKey();
        } else {
            sourceKey = null;
        }
        // 子键集合处理
        if (CollectionUtils.isNotEmptyCollection(childKey)) {
            for (Object c : (Collection<?>) childKey) {
                final ComposeEventKey key = new ComposeEventKey(sourceKey, masterKey, c);
                EventBusUtils.addHandler(handlerMap, key, handler);
            }
        } else {
            final ComposeEventKey key = new ComposeEventKey(sourceKey, masterKey, childKey);
            EventBusUtils.addHandler(handlerMap, key, handler);
        }
    }

    @Override
    public void unregisterX(Object masterKey, @Nullable Object childKey, EventHandler<?> handler) {
        Objects.requireNonNull(masterKey, "masterKey");
        Objects.requireNonNull(handler, "handler");

        Object sourceKey;
        if (masterKey instanceof MasterKeyX masterKeyX) {
            sourceKey = masterKeyX.getSourceKey();
            masterKey = masterKeyX.getMasterKey();
        } else {
            sourceKey = null;
        }
        // 子键集合处理
        final ComposeEventKey composeEventKey = keyPool.get();
        if (CollectionUtils.isNotEmptyCollection(childKey)) {
            for (Object c : (Collection<?>) childKey) {
                composeEventKey.init(sourceKey, masterKey, c);
                EventBusUtils.removeHandler(handlerMap, composeEventKey, handler);
            }
        } else {
            composeEventKey.init(sourceKey, masterKey, childKey);
            EventBusUtils.removeHandler(handlerMap, composeEventKey, handler);
        }
        keyPool.returnOne(composeEventKey);
    }

    @Override
    public boolean hasListener(Object masterKey, @Nullable Object childKey, EventHandler<?> handler) {
        Objects.requireNonNull(masterKey, "masterKey");
        Objects.requireNonNull(handler, "handler");

        Object sourceKey;
        if (masterKey instanceof MasterKeyX masterKeyX) {
            sourceKey = masterKeyX.getSourceKey();
            masterKey = masterKeyX.getMasterKey();
        } else {
            sourceKey = null;
        }
        final ComposeEventKey composeEventKey = keyPool.get();
        composeEventKey.init(sourceKey, masterKey, childKey);
        boolean contains = EventBusUtils.hasListener(handlerMap, composeEventKey, handler);
        keyPool.returnOne(composeEventKey);
        return contains;
    }

    @Override
    public void clear() {
        handlerMap.clear();
    }

    private static final class ComposeEventKey {

        private Object sourceKey;
        private Object masterKey;
        private Object childKey;

        ComposeEventKey() {
        }

        ComposeEventKey(Object sourceKey, Object masterKey, Object childKey) {
            this.sourceKey = sourceKey;
            this.masterKey = masterKey;
            this.childKey = childKey;
        }

        void init(Object sourceKey, Object masterKey, Object childKey) {
            this.sourceKey = sourceKey;
            this.masterKey = masterKey;
            this.childKey = childKey;
        }

        void reset() {
            this.sourceKey = null;
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
            return masterKey.equals(that.masterKey)
                    && Objects.equals(sourceKey, that.sourceKey)
                    && Objects.equals(childKey, that.childKey);
        }

        @Override
        public int hashCode() {
            int r = 31 * masterKey.hashCode();
            r = 31 * r + (sourceKey != null ? sourceKey.hashCode() : 0);
            r = 31 * r + (childKey != null ? childKey.hashCode() : 0);
            return r;
        }

        @Override
        public String toString() {
            return "ComposeEventKey{" +
                    "sourceKey=" + sourceKey +
                    ", masterKey=" + masterKey +
                    ", childKey=" + childKey +
                    '}';
        }
    }

}