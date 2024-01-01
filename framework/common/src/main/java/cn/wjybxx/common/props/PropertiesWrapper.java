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

package cn.wjybxx.common.props;

import cn.wjybxx.base.PropertiesUtils;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Collection;
import java.util.Map;
import java.util.Properties;
import java.util.Set;

/**
 * @author wjybxx
 * date 2023/4/15
 */
class PropertiesWrapper implements IProperties {

    private final Properties params;

    PropertiesWrapper(Properties params) {
        this.params = params;
    }

    @Nullable
    @Override
    public String getAsString(String key) {
        return params.getProperty(key);
    }

    @Override
    public String get(Object key) {
        if (key instanceof String sk) {
            return params.getProperty(sk);
        }
        Object value = params.get(key);
        if (value instanceof String sv) {
            return sv;
        }
        return null;
    }

    @Nonnull
    @Override
    public Set<String> keySet() {
        // 这是个高开销操作
        return params.stringPropertyNames();
    }

    @Nonnull
    @Override
    public Collection<String> values() {
        // 开销更高
        return PropertiesUtils.toMap(params).values();
    }

    @Nonnull
    @Override
    public Set<Entry<String, String>> entrySet() {
        // 开销更高
        return PropertiesUtils.toMap(params).entrySet();
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
        return (String) params.put(key, value);
    }

    @Override
    public String remove(Object key) {
        return (String) params.remove(key);
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