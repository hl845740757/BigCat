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
public class ClassIdRegistries {

    public static <T> ClassIdRegistry<T> fromMapper(final Set<Class<?>> typeSet, ClassIdMapper<T> mapper) {
        List<ClassIdEntry<T>> entries = typeSet.stream()
                .map(e -> ClassIdEntry.of(e, mapper.map(e)))
                .toList();
        return fromEntries(entries);
    }

    @SafeVarargs
    public static <T> ClassIdRegistry<T> fromRegistries(ClassIdRegistry<T>... registries) {
        List<ClassIdEntry<T>> classIdEntries = Arrays.stream(registries)
                .flatMap(e -> e.export().stream())
                .toList();
        return fromEntries(classIdEntries);
    }

    @SafeVarargs
    public static <T> ClassIdRegistry<T> fromEntries(ClassIdEntry<T>... classIdArray) {
        return fromEntries(Arrays.asList(classIdArray));
    }

    public static <T> ClassIdRegistry<T> fromEntries(List<ClassIdEntry<T>> classIdArray) {
        // 先转为不可变
        classIdArray = classIdArray.stream()
                .map(ClassIdEntry::toImmutable)
                .filter(e -> e.classIds.size() > 0)
                .toList();
        final IdentityHashMap<Class<?>, T> type2NameMap = new IdentityHashMap<>(classIdArray.size());
        final HashMap<T, Class<?>> name2TypeMap = new HashMap<>((int) (classIdArray.size() * 1.5f));
        for (ClassIdEntry<T> entry : classIdArray) {
            Class<?> type = entry.clazz;
            if (type2NameMap.containsKey(type)) {
                throw new IllegalArgumentException("type %s is duplicate".formatted(type));
            }
            type2NameMap.put(type, entry.classIds.get(0));
            for (T classId : entry.classIds) {
                if (name2TypeMap.containsKey(classId)) {
                    throw new IllegalArgumentException("classId %s is duplicate".formatted(classId));
                }
                name2TypeMap.put(classId, type);
            }
        }
        return new ClassIdRegistryImpl<>(classIdArray, type2NameMap, name2TypeMap);
    }

    private static class ClassIdRegistryImpl<T> implements ClassIdRegistry<T> {

        private final List<ClassIdEntry<T>> entries;
        private final Map<Class<?>, T> class2NameMap;
        private final Map<T, Class<?>> name2ClassMap;

        ClassIdRegistryImpl(List<ClassIdEntry<T>> entries, Map<Class<?>, T> class2NameMap, Map<T, Class<?>> name2ClassMap) {
            this.entries = entries;
            this.class2NameMap = class2NameMap;
            this.name2ClassMap = name2ClassMap;
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
        public List<ClassIdEntry<T>> export() {
            return new ArrayList<>(entries);
        }
    }
}