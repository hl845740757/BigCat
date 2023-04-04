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

package cn.wjybxx.bigcat.common.codec.binary;

import cn.wjybxx.bigcat.common.CollectionUtils;
import it.unimi.dsi.fastutil.Hash;
import it.unimi.dsi.fastutil.longs.Long2ObjectMap;
import it.unimi.dsi.fastutil.longs.Long2ObjectOpenHashMap;
import it.unimi.dsi.fastutil.objects.Object2LongMap;
import it.unimi.dsi.fastutil.objects.Object2LongOpenHashMap;

import javax.annotation.Nullable;
import java.util.Map;
import java.util.Set;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public class TypeIdRegistries {

    public static TypeIdRegistry fromTypeIdMap(final Map<Class<?>, TypeId> typeIdMap) {
        return fromMapper(typeIdMap.keySet(), typeIdMap::get);
    }

    public static TypeIdRegistry fromMapper(final Set<Class<?>> typeSet, TypeIdMapper typeIdMapper) {
        // 编解码的频率非常高，使用更稀疏的散列
        final Object2LongMap<Class<?>> type2IdentifierMap = new Object2LongOpenHashMap<>(typeSet.size(), Hash.FAST_LOAD_FACTOR);
        final Long2ObjectMap<Class<?>> number2TypeMap = new Long2ObjectOpenHashMap<>(typeSet.size(), Hash.FAST_LOAD_FACTOR);

        for (Class<?> type : typeSet) {
            final TypeId typeId = typeIdMapper.map(type);
            CollectionUtils.requireNotContains(number2TypeMap, typeId.toGuid(), "id");

            type2IdentifierMap.put(type, typeId.toGuid());
            number2TypeMap.put(typeId.toGuid(), type);
        }
        return new DefaultTypeIdRegistry(type2IdentifierMap, number2TypeMap);
    }

    /** 事实不可变 */
    private static class DefaultTypeIdRegistry implements TypeIdRegistry {

        private final Object2LongMap<Class<?>> type2IdentifierMap;
        private final Long2ObjectMap<Class<?>> number2TypeMap;

        private DefaultTypeIdRegistry(Object2LongMap<Class<?>> type2IdentifierMap, Long2ObjectMap<Class<?>> number2TypeMap) {
            this.type2IdentifierMap = type2IdentifierMap;
            this.number2TypeMap = number2TypeMap;
        }

        @Override
        public long ofType(Class<?> type) {
            return type2IdentifierMap.getLong(type);
        }

        @Nullable
        @Override
        public Class<?> ofId(long typeId) {
            return number2TypeMap.get(typeId);
        }

    }
}