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

package cn.wjybxx.bigcat.common.codec.document;

import cn.wjybxx.bigcat.common.CollectionUtils;

import javax.annotation.Nullable;
import java.util.HashMap;
import java.util.IdentityHashMap;
import java.util.Map;
import java.util.Set;

/**
 * @author wjybxx
 * date 2023/4/4
 */
public class TypeNameRegistries {

    public static TypeNameRegistry fromTypeNameMap(final Map<Class<?>, String> typeNameMap) {
        return fromMapper(typeNameMap.keySet(), typeNameMap::get);
    }

    public static TypeNameRegistry fromMapper(final Set<Class<?>> typeSet, TypeNameMapper typeNameMapper) {
        // 编解码的频率非常高，使用更稀疏的散列
        final IdentityHashMap<Class<?>, String> type2NameMap = new IdentityHashMap<>(typeSet.size());
        final HashMap<String, Class<?>> name2TypeMap = new HashMap<>(typeSet.size());

        for (Class<?> type : typeSet) {
            final String typeName = typeNameMapper.map(type);
            CollectionUtils.requireNotContains(name2TypeMap, typeName, "typeName");

            type2NameMap.put(type, typeName);
            name2TypeMap.put(typeName, type);
        }
        return new DefaultTypeNameRegistry(type2NameMap, name2TypeMap);
    }

    private static class DefaultTypeNameRegistry implements TypeNameRegistry {

        private final Map<Class<?>, String> class2NameMap;
        private final Map<String, Class<?>> name2ClassMap;

        DefaultTypeNameRegistry(Map<Class<?>, String> class2NameMap, Map<String, Class<?>> name2ClassMap) {
            this.class2NameMap = Map.copyOf(class2NameMap);
            this.name2ClassMap = Map.copyOf(name2ClassMap);
        }

        @Nullable
        @Override
        public String ofType(Class<?> type) {
            return class2NameMap.get(type);
        }

        @Nullable
        @Override
        public Class<?> ofName(String typeName) {
            return name2ClassMap.get(typeName);
        }

    }

}