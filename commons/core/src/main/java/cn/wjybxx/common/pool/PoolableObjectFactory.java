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

import javax.annotation.Nonnull;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.function.Consumer;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2023/7/28
 */
public interface PoolableObjectFactory<T> {

    /**
     * 创建对象实例
     * 注意：不建议pool作为被池化对象的构造参数，更建议在创建对象后，再赋值给创建的对象。
     *
     * @param pool 可以注入到被池化的对象的属性中，以支持其它方式归还对象
     */
    @Nonnull
    T newInstance(ObjectPool<T> pool);

    /**
     * 激活对象
     * 该方法在对象从池中拿出时执行
     *
     * @param pool 可以注入到被池化的对象的属性中，以支持其它方式归还对象
     */
    default void active(T obj, ObjectPool<T> pool) {

    }

    /**
     * 使对象不活跃
     * 该方法在对象归还到池中时执行
     */
    void inactive(T obj);

    /**
     * 销毁对象
     * 该放在在对象池销毁时，或主动销毁对象时调用
     */
    default void destroy(T obj) {

    }

    // region 一些工厂方法

    static <E> PoolableObjectFactory<E> factory(Supplier<? extends E> factory, Consumer<? super E> resetPolicy) {
        Objects.requireNonNull(factory);
        Objects.requireNonNull(resetPolicy);
        return new PoolableObjectFactory<>() {
            @Nonnull
            @Override
            public E newInstance(ObjectPool<E> pool) {
                return factory.get();
            }

            @Override
            public void inactive(E obj) {
                resetPolicy.accept(obj);
            }
        };
    }

    static <E> PoolableObjectFactory<List<E>> listFactory(final int initCapacity) {
        if (initCapacity < 0) {
            throw new IllegalArgumentException();
        }
        return new PoolableObjectFactory<>() {
            @Nonnull
            @Override
            public List<E> newInstance(ObjectPool<List<E>> pool) {
                return new ArrayList<>(initCapacity);
            }

            @Override
            public void inactive(List<E> obj) {
                obj.clear();
            }
        };
    }

    static <E> PoolableObjectFactory<List<E>> listFactory(Supplier<? extends List<E>> factory) {
        Objects.requireNonNull(factory);
        return new PoolableObjectFactory<>() {
            @Nonnull
            @Override
            public List<E> newInstance(ObjectPool<List<E>> pool) {
                return factory.get();
            }

            @Override
            public void inactive(List<E> obj) {
                obj.clear();
            }
        };
    }

    // endregion

}