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

package cn.wjybxx.bigcat.common.props;

import cn.wjybxx.bigcat.common.annotation.ReadOnlyAfterInit;
import com.google.common.collect.Maps;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;

/**
 * @author wjybxx
 * date 2023/4/15
 */
@ReadOnlyAfterInit
class PropertiesImpl implements IProperties {

    private final Map<String, String> params;

    private PropertiesImpl(Map<String, String> params) {
        this.params = params;
    }

    static PropertiesImpl ofMap(Map<String, String> properties) {
        return new PropertiesImpl(new LinkedHashMap<>(properties)); // 使用LinkedHashMap保留原始顺序
    }

    static PropertiesImpl wrapMap(Map<String, String> properties) {
        return new PropertiesImpl(properties);
    }

    static PropertiesImpl ofProperties(Properties properties) {
        final Set<String> nameSet = properties.stringPropertyNames(); // key本就无序
        final Map<String, String> copied = Maps.newLinkedHashMapWithExpectedSize(nameSet.size());
        for (String name : nameSet) {
            copied.put(name, properties.getProperty(name));
        }
        return new PropertiesImpl(copied);
    }

    static PropertiesImpl partialOfProperties(Properties properties, String namespace) {
        final String filterKey = namespace + ".";
        final Set<String> nameSet = properties.stringPropertyNames();
        final Map<String, String> copied = Maps.newLinkedHashMapWithExpectedSize(nameSet.size());
        nameSet.stream()
                .filter(k -> k.startsWith(filterKey))
                .forEach(k -> copied.put(k.substring(filterKey.length()), properties.getProperty(k)));
        return new PropertiesImpl(copied);
    }

    static PropertiesImpl partialOf(IProperties origin, String namespace) {
        final String filterKey = namespace + ".";
        final Set<String> nameSet = origin.keySet();
        final Map<String, String> copied = Maps.newLinkedHashMapWithExpectedSize(nameSet.size());
        nameSet.stream()
                .filter(k -> k.startsWith(filterKey))
                .forEach(k -> copied.put(k.substring(filterKey.length()), origin.getAsString(k)));
        return new PropertiesImpl(copied);
    }

    @Nullable
    @Override
    public String getAsString(String key) {
        return params.get(key);
    }

    @Nonnull
    @Override
    public Set<String> keySet() {
        return Collections.unmodifiableSet(params.keySet());
    }

    @Override
    public boolean containsKey(String key) {
        return params.containsKey(key);
    }

}