/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.common.props;


import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;

/**
 * @author wjybxx
 * date 2023/4/15
 */
public class PropertiesImpl implements IProperties {

    private final Map<String, String> params;

    public PropertiesImpl() {
        this.params = new LinkedHashMap<>();
    }

    private PropertiesImpl(Map<String, String> params) {
        this.params = Objects.requireNonNull(params);
    }

    public static PropertiesImpl ofMap(Map<String, String> properties) {
        return new PropertiesImpl(new LinkedHashMap<>(properties)); // 使用LinkedHashMap保留原始顺序
    }

    public static PropertiesImpl wrapMap(Map<String, String> properties) {
        return new PropertiesImpl(properties);
    }

    public static PropertiesImpl ofProperties(Properties properties) {
        return new PropertiesImpl(PropertiesUtils.toMap(properties));
    }

    @Nullable
    @Override
    public String getAsString(String key) {
        return params.get(key);
    }

    @Override
    public String get(Object key) {
        return params.get(key);
    }

    @Nonnull
    @Override
    public Set<String> keySet() {
        return Collections.unmodifiableSet(params.keySet());
    }

    @Override
    public Collection<String> values() {
        return Collections.unmodifiableCollection(params.values());
    }

    @Override
    public Set<Entry<String, String>> entrySet() {
        return Collections.unmodifiableSet(params.entrySet());
    }

    //

    @Override
    public int size() {
        return params.size();
    }

    @Override
    public boolean isEmpty() {
        return params.isEmpty();
    }

    @Override
    public boolean containsKey(Object key) {
        return params.containsKey(key);
    }

    @Override
    public boolean containsValue(Object value) {
        return params.containsValue(value);
    }

    @Override
    public String put(String key, String value) {
        return params.put(key, value);
    }

    @Override
    public String remove(Object key) {
        return params.remove(key);
    }

    @Override
    public void putAll(Map<? extends String, ? extends String> m) {
        params.putAll(m);
    }

    @Override
    public void clear() {
        params.clear();
    }

}