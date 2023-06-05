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

import cn.wjybxx.common.CollectionUtils;
import cn.wjybxx.common.Preconditions;
import it.unimi.dsi.fastutil.longs.Long2ObjectMap;
import it.unimi.dsi.fastutil.longs.Long2ObjectOpenHashMap;

import javax.annotation.Nullable;
import java.util.HashMap;
import java.util.IdentityHashMap;
import java.util.Map;
import java.util.Set;

/**
 * @author wjybxx
 * date - 2023/5/28
 */
public class ClassDescRegistries {

    public static ClassDescRegistry fromClassDescMap(final Map<Class<?>, ClassDesc> typeNameMap) {
        return fromMapper(typeNameMap.keySet(), typeNameMap::get);
    }

    public static ClassDescRegistry fromMapper(final Set<Class<?>> typeSet, ClassDescMapper classIdMapper) {
        final IdentityHashMap<Class<?>, ClassDesc> type2DescMap = CollectionUtils.newIdentityHashMap(typeSet.size());
        final HashMap<String, ClassDesc> name2DescMap = CollectionUtils.newHashMap(typeSet.size());
        final Long2ObjectMap<ClassDesc> classId2DescMap = new Long2ObjectOpenHashMap<>(typeSet.size());
        final HashMap<String, Class<?>> name2TypeMap = CollectionUtils.newHashMap(typeSet.size());
        final Long2ObjectMap<Class<?>> classId2TypeMap = new Long2ObjectOpenHashMap<>(typeSet.size());

        for (Class<?> type : typeSet) {
            final ClassDesc classDesc = classIdMapper.map(type);
            Preconditions.checkNotContains(type2DescMap, type, "type");
            Preconditions.checkNotContains(name2DescMap, classDesc.getClassName(), "className");
            Preconditions.checkNotContains(classId2DescMap, classDesc.getClassId(), "classId");

            type2DescMap.put(type, classDesc);
            name2DescMap.put(classDesc.getClassName(), classDesc);
            classId2DescMap.put(classDesc.getClassId(), classDesc);
            name2TypeMap.put(classDesc.getClassName(), type);
            classId2TypeMap.put(classDesc.getClassId(), type);
        }
        return new ClassDescRegistryImpl(type2DescMap, name2DescMap, classId2DescMap,
                name2TypeMap, classId2TypeMap);
    }

    public static ClassDescRegistry fromRegistries(ClassDescRegistry... registries) {
        final IdentityHashMap<Class<?>, ClassDesc> type2DescMap = new IdentityHashMap<>();
        for (ClassDescRegistry registry : registries) {
            type2DescMap.putAll(registry.export());
        }
        return fromMapper(type2DescMap.keySet(), type2DescMap::get);
    }

    private static class ClassDescRegistryImpl implements ClassDescRegistry {

        final Map<Class<?>, ClassDesc> type2DescMap;
        final Map<String, ClassDesc> name2DescMap;
        final Long2ObjectMap<ClassDesc> classId2DescMap;
        final Map<String, Class<?>> name2TypeMap;
        final Long2ObjectMap<Class<?>> classId2TypeMap;

        public ClassDescRegistryImpl(Map<Class<?>, ClassDesc> type2DescMap,
                                     Map<String, ClassDesc> name2DescMap,
                                     Long2ObjectMap<ClassDesc> classId2DescMap,
                                     Map<String, Class<?>> name2TypeMap,
                                     Long2ObjectMap<Class<?>> classId2TypeMap) {
            this.type2DescMap = type2DescMap;
            this.name2DescMap = name2DescMap;
            this.classId2DescMap = classId2DescMap;
            this.name2TypeMap = name2TypeMap;
            this.classId2TypeMap = classId2TypeMap;
        }

        @Nullable
        @Override
        public ClassDesc ofType(Class<?> type) {
            return type2DescMap.get(type);
        }

        @Nullable
        @Override
        public ClassDesc ofId(long classId) {
            return classId2DescMap.get(classId);
        }

        @Nullable
        @Override
        public ClassDesc ofName(String name) {
            return name2DescMap.get(name);
        }

        @Nullable
        @Override
        public Class<?> typeOfId(long classId) {
            return classId2TypeMap.get(classId);
        }

        @Nullable
        @Override
        public Class<?> typeOfName(String name) {
            return name2TypeMap.get(name);
        }

        @Override
        public Map<Class<?>, ClassDesc> export() {
            return new HashMap<>(type2DescMap);
        }
    }
}