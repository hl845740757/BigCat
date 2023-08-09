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

package cn.wjybxx.common.codec;

import javax.annotation.Nullable;
import java.util.*;

/**
 * @author wjybxx
 * date - 2023/4/26
 */
public class TypeMetaRegistries {

    public static <T> TypeMetaRegistry<T> fromMapper(final Set<Class<?>> typeSet, TypeMetaMapper<T> mapper) {
        List<TypeMeta<T>> typeMetaList = typeSet.stream()
                .map(mapper::map)
                .toList();
        return fromMetas(typeMetaList);
    }

    @SafeVarargs
    public static <T> TypeMetaRegistry<T> fromRegistries(TypeMetaRegistry<T>... registries) {
        List<TypeMeta<T>> typeMetaList = Arrays.stream(registries)
                .flatMap(e -> e.export().stream())
                .toList();
        return fromMetas(typeMetaList);
    }

    @SafeVarargs
    public static <T> TypeMetaRegistry<T> fromMetas(TypeMeta<T>... typeMetas) {
        return fromMetas(Arrays.asList(typeMetas));
    }

    public static <T> TypeMetaRegistry<T> fromMetas(List<TypeMeta<T>> typeMetaList) {
        // 先转为不可变
        typeMetaList = typeMetaList.stream()
                .map(TypeMeta::toImmutable)
                .filter(e -> e.classIds.size() > 0)
                .toList();

        final IdentityHashMap<Class<?>, TypeMeta<T>> type2MetaMap = new IdentityHashMap<>(typeMetaList.size());
        final HashMap<T, TypeMeta<T>> id2MetaMap = new HashMap<>((int) (typeMetaList.size() * 1.5f));
        for (TypeMeta<T> typeMeta : typeMetaList) {
            Class<?> type = typeMeta.clazz;
            if (type2MetaMap.containsKey(type)) {
                throw new IllegalArgumentException("type %s is duplicate".formatted(type));
            }
            type2MetaMap.put(type, typeMeta);

            for (T classId : typeMeta.classIds) {
                if (id2MetaMap.containsKey(classId)) {
                    throw new IllegalArgumentException("classId %s is duplicate".formatted(classId));
                }
                id2MetaMap.put(classId, typeMeta);
            }
        }
        return new TypeMetaRegistryImpl<>(typeMetaList, type2MetaMap, id2MetaMap);
    }

    private static class TypeMetaRegistryImpl<T> implements TypeMetaRegistry<T> {

        private final List<TypeMeta<T>> typeMetas;
        private final Map<Class<?>, TypeMeta<T>> type2MetaMap;
        private final Map<T, TypeMeta<T>> id2MetaMap;

        TypeMetaRegistryImpl(List<TypeMeta<T>> typeMetas, Map<Class<?>, TypeMeta<T>> type2MetaMap, Map<T, TypeMeta<T>> id2MetaMap) {
            this.typeMetas = typeMetas;
            this.type2MetaMap = type2MetaMap;
            this.id2MetaMap = id2MetaMap;
        }

        @Nullable
        @Override
        public TypeMeta<T> ofType(Class<?> type) {
            return type2MetaMap.get(type);
        }

        @Nullable
        @Override
        public TypeMeta<T> ofId(T typeName) {
            return id2MetaMap.get(typeName);
        }

        @Override
        public List<TypeMeta<T>> export() {
            return new ArrayList<>(typeMetas);
        }
    }
}