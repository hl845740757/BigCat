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

import cn.wjybxx.bigcat.common.eventbus.GenericEvent;

import javax.annotation.Nonnull;
import java.util.Collection;

/**
 * @author wjybxx
 * date 2023/4/7
 */
class CollectionEvent<T extends Collection<?>> implements GenericEvent<T> {

    private final T collection;

    public CollectionEvent(T collection) {
        this.collection = collection;
    }

    public T getCollection() {
        return collection;
    }

    @SuppressWarnings("unchecked")
    @Nonnull
    @Override
    public Class<T> childKey() {
        return (Class<T>) collection.getClass();
    }

}