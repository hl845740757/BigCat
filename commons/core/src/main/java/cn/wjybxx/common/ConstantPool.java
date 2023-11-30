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

package cn.wjybxx.common;

import cn.wjybxx.common.ex.BadImplementationException;

import javax.annotation.Nullable;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentMap;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * 常量池
 *
 * <h3>一些技巧</h3>
 * Q: 如何不影响性能的情况下获得size? <br>
 * A: <pre> {@code
 *     private static class SizeHolder {
 *         private static final int SIZE = POOL.size();
 *     }
 * }
 * </pre>
 * 内部类是必要的，这可以保证外部类加载完成在内部类之前，确保所有的常量实例创建完成之后才获取values。
 * （大多数技巧都依赖于此）
 * <p>
 * Q: 如何根据id获取常量？  <br>
 * A: 仍然需要借助内部类，在内部类中获取values，然后建立映射即可。
 *
 * @author wjybxx
 * date 2023/4/1
 */
public class ConstantPool<T extends Constant<T>> {

    private final ConcurrentMap<String, T> constants = new ConcurrentHashMap<>();
    private final ConstantFactory<? extends T> factory;
    private final AtomicInteger idGenerator;

    private ConstantPool(ConstantFactory<? extends T> factory, int firstId) {
        this.factory = factory;
        this.idGenerator = new AtomicInteger(firstId);
    }

    public static <T extends Constant<T>> ConstantPool<T> newPool(ConstantFactory<? extends T> factory) {
        return newPool(factory, 0);
    }

    /**
     * @param factory 可通过基础的Builder构建常量的工厂，通常是无额外数据的简单常量对象，factory通常是构造方法引用
     * @param firstId 第一个常量的id，如果常量的创建是无竞争的，那么id将是连续的
     */
    public static <T extends Constant<T>> ConstantPool<T> newPool(ConstantFactory<? extends T> factory, int firstId) {
        return new ConstantPool<>(factory, firstId);
    }

    /**
     * 获取给定名字对应的常量。
     * 1.如果给定的常量存在，则返回存在的常量。
     * 2.如果关联的常量不存在，但可以默认创建，则创建一个新的常量并返回，否则抛出异常。
     */
    public final T valueOf(String name) {
        EnumUtils.checkName(name);
        if (factory == null) {
            return getOrThrow(name);
        } else {
            return getOrCreate(name);
        }
    }

    /**
     * 创建一个常量，如果已存在关联的常量，则抛出异常。
     */
    public final T newInstance(String name) {
        EnumUtils.checkName(name);
        if (factory == null) {
            throw new IllegalStateException("builder required");
        }
        return createOrThrow(new SimpleBuilder<>(name, factory));
    }

    /**
     * 创建一个常量，如果已存在关联的常量，则抛出异常。
     *
     * @param builder 构建常量需要的数据--请确保创建的对象仍然是不可变的。
     */
    public final T newInstance(Constant.Builder<T> builder) {
        Objects.requireNonNull(builder, "builder");
        return createOrThrow(builder);
    }

    /**
     * @return 如果给定名字存在关联的常量，则返回true
     */
    public final boolean exists(String name) {
        EnumUtils.checkName(name);
        return constants.containsKey(name);
    }

    /**
     * 获取一个常量，若不存在关联的常量则返回null。
     *
     * @return 返回常量名关联的常量，若不存在则返回null。
     */
    @Nullable
    public final T get(String name) {
        EnumUtils.checkName(name);
        return constants.get(name);
    }

    /**
     * 获取一个常量，若不存在关联的常量则抛出异常
     *
     * @param name 常量的名字
     * @return 常量名关联的常量
     * @throws IllegalArgumentException 如果不存在对应的常量
     */
    public final T getOrThrow(String name) {
        EnumUtils.checkName(name);
        final T constant = constants.get(name);
        if (null == constant) {
            throw new IllegalArgumentException(name + " does not exist");
        }
        return constant;
    }

    /**
     * 注意：
     * 1.该操作是个高开销的操作。
     * 2.如果存在竞态条件，那么每次返回的结果可能并不一致。
     * 3.返回值是当前数据的一个快照
     * 4.默认我们是按照声明顺序排序的
     *
     * @return 返回当前拥有的所有常量
     */
    public final List<T> values() {
        final ArrayList<T> r = new ArrayList<>(constants.values());
        r.sort(Constant::compareTo);
        return r;
    }

    /**
     * @return 常量池的大小
     */
    public final int size() {
        return constants.size();
    }

    /**
     * 创建一个当前常量池的快照
     */
    public final ConstantMap<T> newConstantMap() {
        return new ConstantMap<>(this);
    }

    /**
     * x
     * 通过名字获取已存在的常量，或者当其不存在时创建新的常量，仅支持简单常量。
     */
    private T getOrCreate(String name) {
        assert factory != null;
        T constant = constants.get(name);
        if (constant == null) {
            final T tempConstant = newConstant(new SimpleBuilder<>(name, factory));
            constant = constants.putIfAbsent(name, tempConstant);
            if (constant == null) {
                return tempConstant;
            }
        }
        return constant;
    }

    /**
     * 创建一个常量，或者已存在关联的常量时则抛出异常
     */
    private T createOrThrow(Constant.Builder<? extends T> builder) {
        String name = builder.getName();
        T constant = constants.get(name);
        if (constant == null) {
            final T tempConstant = newConstant(builder);
            constant = constants.putIfAbsent(name, tempConstant);
            if (constant == null) {
                return tempConstant;
            }
        }
        throw new IllegalArgumentException(name + " is already in use");
    }

    private T newConstant(Constant.Builder<? extends T> builder) {
        final int id = idGenerator.getAndIncrement();
        final T result = builder.setId(id)
                .build();
        // 校验实现
        Objects.requireNonNull(result, "result");
        if (result.id() != id || !Objects.equals(result.name(), builder.getName())) {
            throw new BadImplementationException(String.format("expected id: %d, name: %s, but found id: %d, name: %s",
                    id, builder.getName(), result.id(), result.name()));
        }
        return result;
    }

    private static class SimpleBuilder<T> extends Constant.Builder<T> {

        private final ConstantFactory<T> factory;

        SimpleBuilder(String name, ConstantFactory<T> factory) {
            super(name);
            this.factory = Objects.requireNonNull(factory);
        }

        @Override
        public T build() {
            return factory.newConstant(this);
        }
    }
}