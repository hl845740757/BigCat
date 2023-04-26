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

package cn.wjybxx.common.dson.binary;

import cn.wjybxx.common.CollectionUtils;
import cn.wjybxx.common.annotation.ReadOnlyAfterInit;
import cn.wjybxx.common.dson.BinClassId;
import it.unimi.dsi.fastutil.Hash;
import it.unimi.dsi.fastutil.longs.Long2ObjectMap;
import it.unimi.dsi.fastutil.longs.Long2ObjectOpenHashMap;

import javax.annotation.Nullable;
import java.util.HashMap;
import java.util.IdentityHashMap;
import java.util.Map;
import java.util.Set;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public class TypeIdRegistries {

    public static TypeIdRegistry fromTypeIdMap(final Map<Class<?>, BinClassId> typeIdMap) {
        return fromMapper(typeIdMap.keySet(), typeIdMap::get);
    }

    public static TypeIdRegistry fromMapper(final Set<Class<?>> typeSet, TypeIdMapper typeIdMapper) {
        final Map<Class<?>, BinClassId> type2IdMap = new HashMap<>(typeSet.size());
        final Long2ObjectMap<Class<?>> id2TypeMap = new Long2ObjectOpenHashMap<>(typeSet.size());

        for (Class<?> type : typeSet) {
            final BinClassId binClassId = typeIdMapper.map(type);
            CollectionUtils.requireNotContains(id2TypeMap, binClassId.getGuid(), "id");

            type2IdMap.put(type, binClassId);
            id2TypeMap.put(binClassId.getGuid(), type);
        }
        return new DefaultTypeIdRegistry(type2IdMap, id2TypeMap);
    }

    public static TypeIdRegistry fromRegistries(TypeIdRegistry... registries) {
        final Map<Class<?>, BinClassId> type2IdMap = new HashMap<>(100);
        final Long2ObjectOpenHashMap<Class<?>> id2TypeMap = new Long2ObjectOpenHashMap<>(100);
        for (TypeIdRegistry registry : registries) {
            registry.export().forEach((k, v) -> {
                type2IdMap.put(k, v);
                id2TypeMap.put(v.getGuid(), k);
            });
        }
        return new DefaultTypeIdRegistry(type2IdMap, id2TypeMap);
    }

    @ReadOnlyAfterInit
    private static class DefaultTypeIdRegistry implements TypeIdRegistry {

        private final Map<Class<?>, BinClassId> class2IdMap;
        private final Long2ObjectMap<Class<?>> id2ClassMap;

        DefaultTypeIdRegistry(Map<Class<?>, BinClassId> class2IdMap,
                              Long2ObjectMap<Class<?>> id2ClassMap) {
            assert class2IdMap.size() == id2ClassMap.size();
            this.class2IdMap = new IdentityHashMap<>(class2IdMap);
            this.id2ClassMap = new Long2ObjectOpenHashMap<>(id2ClassMap, Hash.FAST_LOAD_FACTOR);
        }

        @Override
        public BinClassId ofType(Class<?> type) {
            return class2IdMap.get(type);
        }

        @Nullable
        @Override
        public Class<?> ofId(BinClassId classId) {
            return id2ClassMap.get(classId.getGuid());
        }

        @Override
        public Map<Class<?>, BinClassId> export() {
            return new HashMap<>(class2IdMap);
        }
    }

}