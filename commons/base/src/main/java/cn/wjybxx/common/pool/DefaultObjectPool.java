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

package cn.wjybxx.common.pool;

import cn.wjybxx.common.CollectionUtils;

import javax.annotation.concurrent.NotThreadSafe;
import java.util.ArrayList;
import java.util.Collection;
import java.util.Objects;
import java.util.function.Supplier;

/**
 * 对象池的默认实现
 *
 * @author wjybxx
 * date 2023/4/1
 */
@NotThreadSafe
public final class DefaultObjectPool<T> implements ObjectPool<T> {

    /** 默认不能无限缓存 */
    private static final int DEFAULT_MAX_CAPACITY = 1024;
    private final ArrayList<T> freeObjects;
    private final int maxCapacity;

    private final Supplier<? extends T> factory;
    private final ResetPolicy<? super T> resetPolicy;

    public DefaultObjectPool(Supplier<? extends T> factory, ResetPolicy<? super T> resetPolicy) {
        this(factory, resetPolicy, 16, DEFAULT_MAX_CAPACITY);
    }

    public DefaultObjectPool(Supplier<? extends T> factory, ResetPolicy<? super T> resetPolicy, int initialCapacity) {
        this(factory, resetPolicy, initialCapacity, DEFAULT_MAX_CAPACITY);
    }

    /**
     * @param factory         对象创建工厂
     * @param resetPolicy     重置方法
     * @param initialCapacity 支持0 - 0表示默认不初始化
     * @param maxCapacity     支持0 - 0表示不缓存对象
     */
    public DefaultObjectPool(Supplier<? extends T> factory, ResetPolicy<? super T> resetPolicy, int initialCapacity, int maxCapacity) {
        if (initialCapacity < 0 || maxCapacity < 0 || initialCapacity > maxCapacity) {
            throw new IllegalArgumentException("initialCapacity: " + initialCapacity + ", maxCapacity: " + maxCapacity);
        }
        this.freeObjects = new ArrayList<>(initialCapacity);
        this.maxCapacity = maxCapacity;
        this.factory = Objects.requireNonNull(factory, "factory");
        this.resetPolicy = Objects.requireNonNull(resetPolicy, "resetPolicy");
    }

    @Override
    public final T get() {
        // 从最后一个开始删除可避免复制
        final ArrayList<T> freeObjects = this.freeObjects;
        return freeObjects.size() == 0 ? factory.get() : freeObjects.remove(freeObjects.size() - 1);
    }

    @Override
    public final void returnOne(T object) {
        if (object == null) {
            throw new IllegalArgumentException("object cannot be null.");
        }

        // 先调用reset，避免reset出现异常导致添加脏对象到缓存池中 -- 断言是否在池中还是有较大开销
        resetPolicy.reset(object);
        assert !CollectionUtils.containsRef(freeObjects, object);

        if (freeObjects.size() < maxCapacity) {
            freeObjects.add(object);
        }
    }

    @Override
    public final void returnAll(ArrayList<? extends T> objects) {
        if (objects == null) {
            throw new IllegalArgumentException("objects cannot be null.");
        }

        final ArrayList<T> freeObjects = this.freeObjects;
        final int maxCapacity = this.maxCapacity;
        for (int i = 0, n = objects.size(); i < n; i++) {
            T e = objects.get(i);
            if (null == e) {
                continue;
            }

            resetPolicy.reset(e);
            assert !CollectionUtils.containsRef(freeObjects, e);

            if (freeObjects.size() < maxCapacity) {
                freeObjects.add(e);
            }
        }
    }

    @Override
    public final void returnAll(Collection<? extends T> objects) {
        if (objects == null) {
            throw new IllegalArgumentException("objects cannot be null.");
        }

        final ArrayList<T> freeObjects = this.freeObjects;
        final int maxCapacity = this.maxCapacity;
        for (T e : objects) {
            if (null == e) {
                continue;
            }

            resetPolicy.reset(e);
            assert !CollectionUtils.containsRef(freeObjects, e);

            if (freeObjects.size() < maxCapacity) {
                freeObjects.add(e);
            }
        }
    }

    @Override
    public final int maxCount() {
        return maxCapacity;
    }

    @Override
    public final int idleCount() {
        return freeObjects.size();
    }

    @Override
    public final void clear() {
        freeObjects.clear();
    }

}