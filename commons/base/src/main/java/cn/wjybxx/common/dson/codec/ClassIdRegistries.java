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

package cn.wjybxx.common.dson.codec;

import cn.wjybxx.common.Preconditions;
import cn.wjybxx.common.dson.ClassId;
import cn.wjybxx.common.dson.DocClassId;
import cn.wjybxx.common.dson.document.DocumentPojoCodecImpl;

import javax.annotation.Nullable;
import java.util.*;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date - 2023/4/26
 */
public class ClassIdRegistries {

    public static ClassIdRegistry<DocClassId> fromPojoCodecImpl(final List<? extends DocumentPojoCodecImpl<?>> codecImplList) {
        final Map<Class<?>, DocClassId> typeNameMap = codecImplList.stream()
                .collect(Collectors.toMap(DocumentPojoCodecImpl::getEncoderClass, e -> new DocClassId(e.getTypeName())));
        return fromMapper(typeNameMap.keySet(), typeNameMap::get);
    }

    public static <T extends ClassId> ClassIdRegistry<T> fromClassIdMap(final Map<Class<?>, T> typeNameMap) {
        return fromMapper(typeNameMap.keySet(), typeNameMap::get);
    }

    public static <T extends ClassId> ClassIdRegistry<T> fromMapper(final Set<Class<?>> typeSet, ClassIdMapper<T> classIdMapper) {
        final IdentityHashMap<Class<?>, T> type2NameMap = new IdentityHashMap<>(typeSet.size());
        final HashMap<T, Class<?>> name2TypeMap = new HashMap<>(typeSet.size());

        for (Class<?> type : typeSet) {
            final T classId = classIdMapper.map(type);
            Preconditions.checkNotContains(name2TypeMap, classId, "classId");

            type2NameMap.put(type, classId);
            name2TypeMap.put(classId, type);
        }
        return new ClassIdRegistryImpl<>(type2NameMap, name2TypeMap);
    }

    @SafeVarargs
    public static <T extends ClassId> ClassIdRegistry<T> fromRegistries(ClassIdRegistry<T>... registries) {
        final IdentityHashMap<Class<?>, T> type2NameMap = new IdentityHashMap<>();
        final HashMap<T, Class<?>> name2TypeMap = new HashMap<>();
        for (ClassIdRegistry<T> registry : registries) {
            registry.export().forEach((k, v) -> {
                type2NameMap.put(k, v);
                name2TypeMap.put(v, k);
            });
        }
        if (type2NameMap.size() != name2TypeMap.size()) {
            throw new IllegalArgumentException();
        }
        return new ClassIdRegistryImpl<>(type2NameMap, name2TypeMap);
    }

    private static class ClassIdRegistryImpl<T extends ClassId> implements ClassIdRegistry<T> {

        private final Map<Class<?>, T> class2NameMap;
        private final Map<T, Class<?>> name2ClassMap;

        ClassIdRegistryImpl(Map<Class<?>, T> class2NameMap, Map<T, Class<?>> name2ClassMap) {
            this.class2NameMap = Map.copyOf(class2NameMap);
            this.name2ClassMap = Map.copyOf(name2ClassMap);
        }

        @Nullable
        @Override
        public T ofType(Class<?> type) {
            return class2NameMap.get(type);
        }

        @Nullable
        @Override
        public Class<?> ofId(T typeName) {
            return name2ClassMap.get(typeName);
        }

        @Override
        public Map<Class<?>, T> export() {
            return new HashMap<>(class2NameMap);
        }
    }
}