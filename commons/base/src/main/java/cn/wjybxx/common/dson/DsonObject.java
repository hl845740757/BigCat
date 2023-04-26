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

package cn.wjybxx.common.dson;

import javax.annotation.Nonnull;
import java.util.Collection;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Set;

/**
 * @author wjybxx
 * date - 2023/4/21
 */
public abstract class DsonObject<K> extends DsonValue implements Map<K, DsonValue> {

    private final Map<K, DsonValue> map;

    DsonObject() {
        this.map = new LinkedHashMap<>();
    }

    DsonObject(int initialCapacity) {
        this.map = new LinkedHashMap<>(initialCapacity);
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.OBJECT;
    }

    /** @return this */
    public DsonObject<K> append(K key, DsonValue value) {
        if (key == null || value == null) {
            throw nameOrValueIsNull();
        }
        map.put(key, value);
        return this;
    }

    @Override
    public DsonValue put(K key, DsonValue value) {
        if (key == null || value == null) {
            throw nameOrValueIsNull();
        }
        return map.put(key, value);
    }

    @Override
    public void putAll(Map<? extends K, ? extends DsonValue> m) {
        // 需要检测key-value的空
        for (Map.Entry<? extends K, ? extends DsonValue> entry : m.entrySet()) {
            put(entry.getKey(), entry.getValue());
        }
    }

    private static IllegalArgumentException nameOrValueIsNull() {
        return new IllegalArgumentException("name and value cant be null");
    }

    // equals和hash不测试header，只要内容一致即可

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonObject<?> that = (DsonObject<?>) o;

        return map.equals(that.map);
    }

    @Override
    public int hashCode() {
        return map.hashCode();
    }

    @Override
    public String toString() {
        return getClass().getSimpleName() + "{" +
                "map=" + map +
                '}';
    }

    // region 代理实现

    @Override
    public int size() {
        return map.size();
    }

    @Override
    public boolean isEmpty() {
        return map.isEmpty();
    }

    @Override
    public boolean containsKey(Object key) {
        return map.containsKey(key);
    }

    @Override
    public boolean containsValue(Object value) {
        return map.containsValue(value);
    }

    @Override
    public DsonValue get(Object key) {
        return map.get(key);
    }

    @Override
    public DsonValue remove(Object key) {
        return map.remove(key);
    }

    @Override
    public void clear() {
        map.clear();
    }

    @Override
    public Set<K> keySet() {
        return map.keySet();
    }

    @Override
    public Collection<DsonValue> values() {
        return map.values();
    }

    @Override
    public Set<Map.Entry<K, DsonValue>> entrySet() {
        return map.entrySet();
    }

    // endregion
}