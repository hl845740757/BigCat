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

    public static TypeMetaRegistry fromMapper(final Set<Class<?>> typeSet, TypeMetaMapper mapper) {
        List<TypeMeta> typeMetaList = new ArrayList<>();
        for (Class<?> clazz : typeSet) {
            typeMetaList.add(mapper.map(clazz));
        }
        return fromMetas(typeMetaList);
    }

    public static TypeMetaRegistry fromRegistries(TypeMetaRegistry... registries) {
        List<TypeMeta> typeMetaList = new ArrayList<>();
        for (TypeMetaRegistry e : registries) {
            typeMetaList.addAll(e.export());
        }
        return fromMetas(typeMetaList);
    }

    public static TypeMetaRegistry fromMetas(TypeMeta... typeMetas) {
        return fromMetas(Arrays.asList(typeMetas));
    }

    public static TypeMetaRegistry fromMetas(List<TypeMeta> typeMetaList) {
        // 先转为不可变
        typeMetaList = typeMetaList.stream()
                .map(TypeMeta::toImmutable)
                .toList();

        final IdentityHashMap<Class<?>, TypeMeta> type2MetaMap = new IdentityHashMap<>(typeMetaList.size());
        final HashMap<ClassId, TypeMeta> id2MetaMap = new HashMap<>((int) (typeMetaList.size() * 1.5f));
        final HashMap<String, TypeMeta> name2MetaMap = new HashMap<>((int) (typeMetaList.size() * 1.5f));

        for (TypeMeta typeMeta : typeMetaList) {
            Class<?> type = typeMeta.clazz;
            if (type2MetaMap.containsKey(type)) {
                throw new IllegalArgumentException("type %s is duplicate".formatted(type));
            }
            type2MetaMap.put(type, typeMeta);

            for (String className : typeMeta.classNames) {
                if (name2MetaMap.containsKey(className)) {
                    throw new IllegalArgumentException("className %s is duplicate".formatted(className));
                }
                name2MetaMap.put(className, typeMeta);
            }
            for (ClassId classId : typeMeta.classIds) {
                if (id2MetaMap.containsKey(classId)) {
                    throw new IllegalArgumentException("classId %s is duplicate".formatted(classId));
                }
                id2MetaMap.put(classId, typeMeta);
            }
        }
        return new TypeMetaRegistryImpl(typeMetaList, type2MetaMap, id2MetaMap, name2MetaMap);
    }

    private static class TypeMetaRegistryImpl implements TypeMetaRegistry {

        private final List<TypeMeta> typeMetas;
        private final Map<Class<?>, TypeMeta> type2MetaMap;
        private final Map<ClassId, TypeMeta> id2MetaMap;
        private final HashMap<String, TypeMeta> name2MetaMap;

        TypeMetaRegistryImpl(List<TypeMeta> typeMetas,
                             Map<Class<?>, TypeMeta> type2MetaMap,
                             Map<ClassId, TypeMeta> id2MetaMap,
                             HashMap<String, TypeMeta> name2MetaMap) {
            this.typeMetas = typeMetas;
            this.type2MetaMap = type2MetaMap;
            this.id2MetaMap = id2MetaMap;
            this.name2MetaMap = name2MetaMap;
        }

        @Nullable
        @Override
        public TypeMeta ofType(Class<?> type) {
            return type2MetaMap.get(type);
        }

        @Nullable
        @Override
        public TypeMeta ofId(ClassId clsId) {
            return id2MetaMap.get(clsId);
        }

        @Override
        public TypeMeta ofName(String clsName) {
            return name2MetaMap.get(clsName);
        }

        @Override
        public List<TypeMeta> export() {
            return new ArrayList<>(typeMetas);
        }
    }
}