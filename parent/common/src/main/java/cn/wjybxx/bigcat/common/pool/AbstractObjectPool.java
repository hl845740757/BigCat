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

package cn.wjybxx.bigcat.common.pool;


import cn.wjybxx.bigcat.common.CollectionUtils;

import javax.annotation.concurrent.NotThreadSafe;
import java.util.ArrayList;
import java.util.Collection;

/**
 * 对象池的抽象实现
 *
 * @author wjybxx
 * date 2023/4/1
 */
@NotThreadSafe
public abstract class AbstractObjectPool<T> implements ObjectPool<T> {

    private final ArrayList<T> freeObjects;
    private final int maxCapacity;

    public AbstractObjectPool() {
        this(16, Integer.MAX_VALUE);
    }

    public AbstractObjectPool(int initialCapacity) {
        this(initialCapacity, Integer.MAX_VALUE);
    }

    public AbstractObjectPool(int initialCapacity, int maxCapacity) {
        if (maxCapacity <= 0 || initialCapacity > maxCapacity) {
            throw new IllegalArgumentException("initialCapacity: " + initialCapacity + ", maxCapacity: " + maxCapacity);
        }
        this.freeObjects = new ArrayList<>(initialCapacity);
        this.maxCapacity = maxCapacity;
    }

    protected abstract T newObject();

    protected abstract void reset(T object);

    @Override
    public final T get() {
        // 从最后一个开始删除可避免复制
        final ArrayList<T> freeObjects = this.freeObjects;
        return freeObjects.size() == 0 ? newObject() : freeObjects.remove(freeObjects.size() - 1);
    }

    @Override
    public final void returnOne(T object) {
        if (object == null) {
            throw new IllegalArgumentException("object cannot be null.");
        }

        // 先调用reset，避免reset出现异常导致添加脏对象到缓存池中 -- 断言是否在池中还是有较大开销
        reset(object);
        assert !CollectionUtils.containsIdentity(freeObjects, object);

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

            reset(e);
            assert !CollectionUtils.containsIdentity(freeObjects, e);

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

            reset(e);
            assert !CollectionUtils.containsIdentity(freeObjects, e);

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
