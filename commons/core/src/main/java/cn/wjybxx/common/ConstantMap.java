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

import javax.annotation.Nullable;
import javax.annotation.concurrent.Immutable;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

/**
 * 由于{@link ConstantPool}是可变的，这使得有些查询是高开销的，比如：{@link ConstantPool#values()}
 *
 * @author wjybxx
 * date 2023/4/1
 */
@Immutable
public class ConstantMap<T extends Constant<T>> {

    private final List<T> immutableValues;
    private final List<String> immutableNames;
    private final Map<String, T> constants;

    ConstantMap(ConstantPool<T> pool) {
        final List<T> immutableValues = CollectionUtils.toImmutableList(pool.values());
        this.immutableValues = immutableValues;
        this.immutableNames = immutableValues.stream()
                .map(Constant::name)
                .toList();
        this.constants = immutableValues.stream()
                .collect(Collectors.toUnmodifiableMap(Constant::name, e -> e));
    }

    /**
     * @return 如果给定名字存在关联的常量，则返回true
     */
    public final boolean exists(String name) {
        return constants.containsKey(name);
    }

    /**
     * 获取一个常量，若不存在关联的常量则返回null。
     *
     * @return 返回常量名关联的常量，若不存在则返回null。
     */
    @Nullable
    public final T get(String name) {
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
        final T constant = constants.get(name);
        if (null == constant) {
            throw new IllegalArgumentException(name + " does not exist");
        }
        return constant;
    }

    /** @return 常量池的名字，和{@link #values()}的顺序一致 */
    public final List<String> names() {
        return immutableNames;
    }

    /** @return 已排序的不可变常量集合 */
    public final List<T> values() {
        return immutableValues;
    }

}