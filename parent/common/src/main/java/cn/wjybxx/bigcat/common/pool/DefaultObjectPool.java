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

import javax.annotation.concurrent.NotThreadSafe;
import java.util.Objects;
import java.util.function.Supplier;

/**
 * 对象池的默认实现
 *
 * @author wjybxx
 * date 2023/4/1
 */
@NotThreadSafe
public class DefaultObjectPool<T> extends AbstractObjectPool<T> {

    private final Supplier<T> factory;
    private final ResetPolicy<T> resetPolicy;

    public DefaultObjectPool(Supplier<T> factory, ResetPolicy<T> resetPolicy) {
        super();
        this.factory = Objects.requireNonNull(factory, "factory");
        this.resetPolicy = Objects.requireNonNull(resetPolicy, "resetPolicy");
    }

    public DefaultObjectPool(Supplier<T> factory, ResetPolicy<T> resetPolicy, int initialCapacity) {
        super(initialCapacity);
        this.factory = Objects.requireNonNull(factory, "factory");
        this.resetPolicy = Objects.requireNonNull(resetPolicy, "resetPolicy");
    }

    public DefaultObjectPool(Supplier<T> factory, ResetPolicy<T> resetPolicy, int initialCapacity, int maxCapacity) {
        super(initialCapacity, maxCapacity);
        this.factory = Objects.requireNonNull(factory, "factory");
        this.resetPolicy = Objects.requireNonNull(resetPolicy, "resetPolicy");
    }

    @Override
    protected T newObject() {
        return factory.get();
    }

    @Override
    protected void reset(T object) {
        resetPolicy.reset(object);
    }
}