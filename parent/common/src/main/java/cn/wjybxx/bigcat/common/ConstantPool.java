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

import cn.wjybxx.bigcat.common.ex.BadImplementationException;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentMap;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * 常量池
 * <p>
 * 注意：如果使用{@code extInfo}创建常量对象，请确保你使用的是{@link #ofExtConstantFactory(ExtConstantFactory)}创建的常量池。
 *
 * <h3>一些技巧</h3>
 * Q: 如何不影响性能的情况下获得size? <br>
 * A: <pre> {@code
 *     private static class SizeHolder {
 *         private static final int SIZE = POOL.values().size();
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
    private final FactoryWrapper<T> factory;

    private ConstantPool(@Nonnull FactoryWrapper<T> factory) {
        this.factory = factory;
    }

    // JAVA什么时候出方法参数默认值啊.,.
    public static <T extends Constant<T>> ConstantPool<T> ofConstantFactory(ConstantFactory<? extends T> factory) {
        return ofConstantFactory(factory, 0);
    }

    public static <T extends Constant<T>> ConstantPool<T> ofExtConstantFactory(ExtConstantFactory<? extends T> factory) {
        return ofExtConstantFactory(factory, 0);
    }

    /**
     * @param factory 创建常量的工厂方法，通常是绑定构造方法
     * @param firstId 第一个常量的id，如果常量的创建是无竞争的，那么id将是连续的
     */
    public static <T extends Constant<T>> ConstantPool<T> ofConstantFactory(ConstantFactory<? extends T> factory, int firstId) {
        Objects.requireNonNull(factory, "factory");
        final FactoryWrapper<T> wrapper = new FactoryWrapper<>(new ExtConstantFactoryAdapter<>(factory), firstId);
        return new ConstantPool<>(wrapper);
    }

    /**
     * 使用静态方法，可以使用不同的方法名，避免使用lambda或方法引用时造成的混乱。
     *
     * @param factory 可使用外部信息创建常量的工厂
     * @param firstId 第一个常量的id，如果常量的创建是无竞争的，那么id将是连续的
     */
    public static <T extends Constant<T>> ConstantPool<T> ofExtConstantFactory(ExtConstantFactory<? extends T> factory, int firstId) {
        Objects.requireNonNull(factory, "factory");
        return new ConstantPool<>(new FactoryWrapper<>(factory, firstId));
    }

    /**
     * Shortcut of {@link #valueOf(String) valueOf(firstNameComponent.getName() + "#" + secondNameComponent)}.
     *
     * @param firstNameComponent  充当命名空间
     * @param secondNameComponent 命名空间内的名字
     */
    public final T valueOf(Class<?> firstNameComponent, String secondNameComponent) {
        final String nameSpace = Objects.requireNonNull(firstNameComponent, "firstNameComponent").getName();
        final String name = checkNotNullAndNotEmpty(secondNameComponent, "secondNameComponent");
        return valueOf(nameSpace + "#" + name);
    }

    /**
     * 获取给定名字对应的常量。
     * 如果关联的常量上不存在，则创建一个新的常量并返回。
     * 一旦创建成功，则接下来的调用，则总是返回先前创建的常量。
     */
    public final T valueOf(String name) {
        checkNotNullAndNotEmpty(name, "name");
        return getOrCreate(name, null);
    }

    /**
     * 获取给定名字对应的常量。
     * 如果关联的常量上不存在，则创建一个新的常量并返回。
     * 一旦创建成功，则接下来的调用，则总是返回先前创建的常量。
     *
     * @param name    常量的名字
     * @param extInfo 外部扩展信息。
     *                请注意：如果使用外部信息创建对象，请确保创建的对象仍然是不可变的。
     */
    public final T valueOf(String name, Object extInfo) {
        checkNotNullAndNotEmpty(name, "name");
        return getOrCreate(name, extInfo);
    }

    /**
     * 创建一个常量，如果已存在关联的常量，则抛出异常。
     */
    public final T newInstance(String name) {
        checkNotNullAndNotEmpty(name, "name");
        return createOrThrow(name, null);
    }

    /**
     * 创建一个常量，如果已存在关联的常量，则抛出异常。
     *
     * @param name    常量的名字
     * @param extInfo 外部扩展信息。
     *                请注意：如果使用外部信息创建对象，请确保创建的对象仍然是不可变的。
     */
    public final T newInstance(String name, Object extInfo) {
        checkNotNullAndNotEmpty(name, "name");
        return createOrThrow(name, extInfo);
    }

    /**
     * @return 如果给定名字存在关联的常量，则返回true
     */
    public final boolean exists(String name) {
        checkNotNullAndNotEmpty(name, "name");
        return constants.containsKey(name);
    }

    /**
     * 获取一个常量，若不存在关联的常量则返回null。
     *
     * @return 返回常量名关联的常量，若不存在则返回null。
     */
    @Nullable
    public final T get(String name) {
        checkNotNullAndNotEmpty(name, "name");
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
        checkNotNullAndNotEmpty(name, "name");
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
     * 创建一个当前常量池的快照
     */
    public final ConstantPoolSnapshot<T> newSnapshot() {
        return new ConstantPoolSnapshot<>(this);
    }

    private static String checkNotNullAndNotEmpty(String value, String name) {
        Objects.requireNonNull(value, name);
        if (value.isEmpty()) {
            throw new IllegalArgumentException(name + " is empty ");
        }
        return value;
    }

    /**
     * 通过名字获取已存在的常量，或者当其不存在时创建新的常量。
     *
     * @param name    常量的名字
     * @param extInfo 外部扩展信息
     */
    private T getOrCreate(String name, Object extInfo) {
        T constant = constants.get(name);
        if (constant == null) {
            final T tempConstant = factory.apply(name, extInfo);
            constant = constants.putIfAbsent(name, tempConstant);
            if (constant == null) {
                return tempConstant;
            }
        }

        return constant;
    }

    /**
     * 创建一个常量，或者已存在关联的常量时则抛出异常
     *
     * @param name 常量的名字
     */
    private T createOrThrow(String name, Object extInfo) {
        T constant = constants.get(name);
        if (constant == null) {
            final T tempConstant = factory.apply(name, extInfo);
            constant = constants.putIfAbsent(name, tempConstant);
            if (constant == null) {
                return tempConstant;
            }
        }

        throw new IllegalArgumentException(name + " is already in use");
    }

    private static class ExtConstantFactoryAdapter<T> implements ExtConstantFactory<T> {

        private final ConstantFactory<? extends T> delegated;

        private ExtConstantFactoryAdapter(ConstantFactory<? extends T> delegated) {
            this.delegated = delegated;
        }

        @Nonnull
        @Override
        public T newConstant(int id, String name, Object extInfo) {
            if (null != extInfo) {
                throw new IllegalArgumentException("ExtInfo is not supported in your pool");
            }
            return delegated.newConstant(id, name);
        }
    }

    private static class FactoryWrapper<T extends Constant<T>> {

        private final ExtConstantFactory<? extends T> delegate;
        private final AtomicInteger idGenerator;

        FactoryWrapper(ExtConstantFactory<? extends T> delegate, int firstId) {
            this.delegate = delegate;
            this.idGenerator = new AtomicInteger(firstId);
        }

        private int nextId() {
            return idGenerator.getAndIncrement();
        }

        T apply(String name, Object extInfo) {
            final int id = nextId();
            final T result = delegate.newConstant(id, name, extInfo);
            Objects.requireNonNull(result, "result");
            if (result.id() == id && Objects.equals(result.name(), name)) {
                // 校验实现
                return result;
            }
            final String msg = String.format("expected id: %d, name: %s, found id: %d, name: %s", id, name, result.id(), result.name());
            throw new BadImplementationException(msg);
        }
    }

}