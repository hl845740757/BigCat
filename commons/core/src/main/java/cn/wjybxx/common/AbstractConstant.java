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

import javax.annotation.Nonnull;
import java.util.Objects;
import java.util.concurrent.atomic.AtomicLong;

/**
 * @author wjybxx
 * date - 2023/5/19
 */
public abstract class AbstractConstant<T extends AbstractConstant<T>> implements Constant<T> {

    /**
     * 为防止id溢出，这里使用long。
     * 不过，如果程序大量创建常量，那么程序可能是有问题的，程序可能创建了大量的临时常量。
     */
    private static final AtomicLong uniqueIdGenerator = new AtomicLong();

    /** 常量在其所属常量池下的唯一id */
    private final int id;
    /** 常量的名字 */
    private final String name;
    /**
     * 常量的全局唯一标识。
     * 注意：该标识受到常量创建顺序的影响。
     */
    private final long uniqueId;

    protected AbstractConstant(Builder<?> builder) {
        this.id = builder.checkedGetId();
        this.name = Objects.requireNonNull(builder.getName());
        this.uniqueId = uniqueIdGenerator.incrementAndGet();
    }

    @Override
    public final int id() {
        return id;
    }

    @Override
    public final String name() {
        return name;
    }

    @Override
    public String toString() {
        // 通常不应该覆盖该方法
        return name;
    }

    @Override
    public final int hashCode() {
        // 不对hashCode做任何假设
        return super.hashCode();
    }

    @Override
    public final boolean equals(Object obj) {
        // 只使用 == 比较相等性
        return this == obj;
    }

    @Override
    protected final Object clone() throws CloneNotSupportedException {
        throw new CloneNotSupportedException();
    }

    @Override
    public final int compareTo(final @Nonnull T other) {
        if (this == other) {
            return 0;
        }

        // 注意：
        // 1. 未比较名字也未比较其它信息 - 这可以保证同一个类中定义的常量，其结果与定义顺序相同，就像枚举。
        // 2. uniqueId与类初始化顺序有关，因此无法保证不同类中定义的常量的顺序。
        // 3. 有个例外，超类中定义的常量总是在子类前面，这是因为超类总是在子类之前初始化。
        final AbstractConstant<?> that = other;
        if (uniqueId < that.uniqueId) {
            return -1;
        }
        if (uniqueId > that.uniqueId) {
            return 1;
        }
        throw new Error("failed to compare two different constants, this: " + name + ", that: " + that.name);
    }

}