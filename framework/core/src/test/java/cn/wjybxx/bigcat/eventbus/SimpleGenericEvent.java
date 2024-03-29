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

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * @author wjybxx
 * date 2023/4/13
 */
public class SimpleGenericEvent<T> implements GenericEvent<T> {

    private final T value;

    public SimpleGenericEvent(T value) {
        this.value = Objects.requireNonNull(value);
    }

    public T getValue() {
        return value;
    }

    @SuppressWarnings("unchecked")
    @Nonnull
    @Override
    public Class<T> childKey() {
        return (Class<T>) value.getClass();
    }

}