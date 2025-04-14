/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.bigcat.eventbus;

import cn.wjybxx.base.CollectionUtils;
import cn.wjybxx.base.collection.DefaultDynamicArray;
import cn.wjybxx.base.collection.DynamicArray;
import cn.wjybxx.base.collection.SmallDynamicArray;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.NotThreadSafe;
import java.util.Map;
import java.util.Objects;

/**
 * 默认的EventBus
 * <p>
 * 1.适用于养成系统等不那么追求性能，更注重代码可读性的场景
 * 2.不再池化Key，意义不大 -- 徒增复杂度。
 *
 * @author wjybxx
 * date 2023/4/6
 */
@NotThreadSafe
public class DefaultEventBus implements EventBus {

    private static final Logger logger = LoggerFactory.getLogger(DefaultEventBus.class);
    private static final int DEFAULT_EXPECTED_SIZE = 64;
    private static final int RECURSION_LIMIT = 16;

    /**
     * eventKey -> handler
     * eventKey：{@link Class}或{@link CompositeKey}
     */
    private final Map<Object, DynamicArray<EventHandler<?>>> handlerMap;
    /** 是否使用小数组 */
    private final boolean smallArray;
    /** 递归深度 - 防止死循环 */
    private int recursionDepth;

    public DefaultEventBus() {
        this(DEFAULT_EXPECTED_SIZE, true);
    }

    /**
     * @param expectedSize 字典初始大小
     * @param smallArray   监听器列表是否使用小型数组，同一事件类型仅支持最大64个监听器
     */
    public DefaultEventBus(int expectedSize, boolean smallArray) {
        this.handlerMap = CollectionUtils.newHashMap(expectedSize);
        this.smallArray = smallArray;
    }

    @Override
    public void clear() {
        handlerMap.clear();
    }

    // region register

    @Override
    public <T> void register(Class<T> masterKey, EventHandler<? super T> handler) {
        registerImpl(masterKey, handler);
    }

    @Override
    public <T> void register(Class<T> masterKey, int childKey, EventHandler<? super T> handler) {
        registerImpl(new CompositeKey(masterKey, childKey), handler);
    }

    @Override
    public <T> void register(Class<T> masterKey, Object childKey, EventHandler<? super T> handler) {
        registerImpl(new CompositeKey(masterKey, childKey), handler);
    }

    private void registerImpl(@Nonnull Object key, EventHandler<?> handler) {
        Objects.requireNonNull(handler);
        DynamicArray<EventHandler<?>> dynamicArray = handlerMap.get(key);
        if (dynamicArray == null) {
            dynamicArray = smallArray ? new SmallDynamicArray<>(4) : new DefaultDynamicArray<>(8);
            handlerMap.put(key, dynamicArray);
        }
        dynamicArray.add(handler);
    }

    private void unregisterImpl(@Nonnull Object key, EventHandler<?> handler) {
        if (handler == null) return;
        DynamicArray<EventHandler<?>> dynamicArray = handlerMap.get(key);
        if (dynamicArray == null || dynamicArray.elementCount() == 0) {
            return;
        }
        dynamicArray.remove(handler);
    }

    // endregion

    // region unregister

    @Override
    public <T> void unregister(Class<T> masterKey, EventHandler<? super T> handler) {
        unregisterImpl(masterKey, handler);
    }

    @Override
    public <T> void unregister(Class<T> masterKey, int childKey, EventHandler<? super T> handler) {
        unregisterImpl(new CompositeKey(masterKey, childKey), handler);
    }

    @Override
    public <T> void unregister(Class<T> masterKey, Object childKey, EventHandler<? super T> handler) {
        unregisterImpl(new CompositeKey(masterKey, childKey), handler);
    }

    // endregion

    // region post

    @Override
    public final void post(Object event) {
        postImpl(event, event.getClass());
    }

    @Override
    public void post(Object event, int childKey) {
        postImpl(event, event.getClass());
        postImpl(event, new CompositeKey(event.getClass(), childKey));
    }

    @Override
    public void post(Object event, Object childKey) {
        postImpl(event, event.getClass());
        postImpl(event, new CompositeKey(event.getClass(), childKey));
    }

    @SuppressWarnings("unchecked")
    private <T> void postImpl(T event, Object key) {
        DynamicArray<?> array = handlerMap.get(key);
        if (array == null || array.elementCount() == 0) {
            return;
        }
        if (recursionDepth >= RECURSION_LIMIT) {
            throw new IllegalStateException("event had too many levels of nesting");
        }
        DynamicArray<EventHandler<? super T>> castArray = (DynamicArray<EventHandler<? super T>>) array;
        recursionDepth++;
        castArray.beginItr();
        try {
            for (int idx = 0, len = array.length(); idx < len; idx++) {
                EventHandler<? super T> handler = castArray.get(idx);
                if (handler == null) continue;
                try {
                    handler.onEvent(event);
                } catch (Exception e) {
                    logException(event, e, handler);
                }
            }
        } finally {
            castArray.endItr();
            recursionDepth--;
        }
    }

    private static void logException(Object event, Exception e, EventHandler<?> handler) {
        final String handlerClassName = handler.getClass().getName();
        final String eventClassName = event.getClass().getName();
        logger.warn("handlerClassName: " + handlerClassName + ", eventClassName: " + eventClassName, e);
    }

    // endregion

    // region key
    private static class CompositeKey {

        public final Class<?> masterKey;
        public final int intKey; // 避免装箱
        public final Object objectKey;

        public CompositeKey(Class<?> masterKey, int intKey) {
            this.masterKey = masterKey;
            this.intKey = intKey;
            this.objectKey = null;
        }

        public CompositeKey(Class<?> masterKey, Object objectKey) {
            this.masterKey = masterKey;
            this.intKey = 0;
            this.objectKey = objectKey;
        }

        @Override
        public boolean equals(Object o) {
            if (this == o) return true;
            if (o == null || getClass() != o.getClass()) return false;

            CompositeKey that = (CompositeKey) o;
            return intKey == that.intKey
                    && masterKey.equals(that.masterKey)
                    && Objects.equals(objectKey, that.objectKey);
        }

        @Override
        public int hashCode() {
            int result = masterKey.hashCode();
            result = 31 * result + intKey;
            result = 31 * result + Objects.hashCode(objectKey);
            return result;
        }

        @Override
        public String toString() {
            return "CompositeKey{" +
                    "masterKey=" + masterKey +
                    ", intKey=" + intKey +
                    ", objectKey=" + objectKey +
                    '}';
        }
    }

    // endregion
}