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

package cn.wjybxx.common.dson.document;

import cn.wjybxx.common.CollectionUtils;
import cn.wjybxx.common.dson.DocClassId;

import javax.annotation.Nullable;
import java.util.*;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date 2023/4/4
 */
public class TypeNameRegistries {

    public static TypeNameRegistry fromTypeNameMap(final Map<Class<?>, DocClassId> typeNameMap) {
        return fromMapper(typeNameMap.keySet(), typeNameMap::get);
    }

    public static TypeNameRegistry fromTypeNameMap(final List<? extends DocumentPojoCodecImpl<?>> codecImplList) {
        final Map<Class<?>, DocClassId> typeNameMap = codecImplList.stream()
                .collect(Collectors.toMap(DocumentPojoCodecImpl::getEncoderClass, e -> new DocClassId(e.getTypeName())));
        return fromMapper(typeNameMap.keySet(), typeNameMap::get);
    }

    public static TypeNameRegistry fromMapper(final Set<Class<?>> typeSet, TypeNameMapper typeNameMapper) {
        final IdentityHashMap<Class<?>, DocClassId> type2NameMap = new IdentityHashMap<>(typeSet.size());
        final HashMap<DocClassId, Class<?>> name2TypeMap = new HashMap<>(typeSet.size());

        for (Class<?> type : typeSet) {
            final DocClassId typeName = typeNameMapper.map(type);
            CollectionUtils.requireNotContains(name2TypeMap, typeName, "typeName");

            type2NameMap.put(type, typeName);
            name2TypeMap.put(typeName, type);
        }
        return new DefaultTypeNameRegistry(type2NameMap, name2TypeMap);
    }

    public static TypeNameRegistry fromRegistries(TypeNameRegistry... registries) {
        final IdentityHashMap<Class<?>, DocClassId> type2NameMap = new IdentityHashMap<>();
        final HashMap<DocClassId, Class<?>> name2TypeMap = new HashMap<>();
        for (TypeNameRegistry registry : registries) {
            registry.export().forEach((k, v) -> {
                type2NameMap.put(k, v);
                name2TypeMap.put(v, k);
            });
        }
        if (type2NameMap.size() != name2TypeMap.size()) {
            throw new IllegalArgumentException();
        }
        return new DefaultTypeNameRegistry(type2NameMap, name2TypeMap);
    }

    private static class DefaultTypeNameRegistry implements TypeNameRegistry {

        private final Map<Class<?>, DocClassId> class2NameMap;
        private final Map<DocClassId, Class<?>> name2ClassMap;

        DefaultTypeNameRegistry(Map<Class<?>, DocClassId> class2NameMap, Map<DocClassId, Class<?>> name2ClassMap) {
            this.class2NameMap = Map.copyOf(class2NameMap);
            this.name2ClassMap = Map.copyOf(name2ClassMap);
        }

        @Nullable
        @Override
        public DocClassId ofType(Class<?> type) {
            return class2NameMap.get(type);
        }

        @Nullable
        @Override
        public Class<?> ofName(DocClassId typeName) {
            return name2ClassMap.get(typeName);
        }

        @Override
        public Map<Class<?>, DocClassId> export() {
            return new HashMap<>(class2NameMap);
        }
    }

}