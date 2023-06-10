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

package cn.wjybxx.common.dson;

import javax.annotation.Nonnull;
import java.util.Collection;
import java.util.Collections;
import java.util.Map;
import java.util.Set;

/**
 * 1.Header不可以再持有header，否则陷入死循环
 * 2.Header的结构应该是简单清晰的，可简单编解码
 *
 * @author wjybxx
 * date - 2023/5/27
 */
public abstract class DsonHeader<K> extends DsonValue implements Map<K, DsonValue> {

    protected final Map<K, DsonValue> valueMap;

    protected DsonHeader(Map<K, DsonValue> valueMap) {
        this.valueMap = valueMap;
    }

    public static <K> DsonHeader<K> toImmutable(DsonHeader<K> src) {
        return ImmutableDsons.header(src);
    }

    public static <K> DsonHeader<K> empty() {
        return ImmutableDsons.header();
    }

    //
    static <K> void checkKeyValue(K key, DsonValue value) {
        if (key == null) {
            throw new IllegalArgumentException("key cant be null");
        }
        if (value == null) {
            throw new IllegalArgumentException("value cant be null");
        }
    }

    public Map<K, DsonValue> getValueMap() {
        return Collections.unmodifiableMap(valueMap);
    }

    /** @return this */
    public DsonHeader<K> append(K key, DsonValue value) {
        checkKeyValue(key, value);
        valueMap.put(key, value);
        return this;
    }

    @Override
    public DsonValue put(K key, DsonValue value) {
        checkKeyValue(key, value);
        return valueMap.put(key, value);
    }

    @Override
    public void putAll(Map<? extends K, ? extends DsonValue> m) {
        // 需要检测key-value的空
        for (Entry<? extends K, ? extends DsonValue> entry : m.entrySet()) {
            put(entry.getKey(), entry.getValue());
        }
    }

    @Nonnull
    @Override
    public final DsonType getDsonType() {
        return DsonType.HEADER;
    }

    // 默认只比较value，不判断类型

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        return o instanceof DsonHeader<?> that && valueMap.equals(that.valueMap);
    }

    @Override
    public int hashCode() {
        return valueMap.hashCode();
    }

    @Override
    public String toString() {
        return getClass().getSimpleName() + "{" +
                "valueMap=" + valueMap +
                '}';
    }

    // region 代理实现
    @Override
    public int size() {
        return valueMap.size();
    }

    @Override
    public boolean isEmpty() {
        return valueMap.isEmpty();
    }

    @Override
    public boolean containsKey(Object key) {
        return valueMap.containsKey(key);
    }

    @Override
    public boolean containsValue(Object value) {
        return valueMap.containsValue(value);
    }

    @Override
    public DsonValue get(Object key) {
        return valueMap.get(key);
    }

    @Override
    public DsonValue remove(Object key) {
        return valueMap.remove(key);
    }

    @Override
    public void clear() {
        valueMap.clear();
    }

    @Override
    public Set<K> keySet() {
        return valueMap.keySet();
    }

    @Override
    public Collection<DsonValue> values() {
        return valueMap.values();
    }

    @Override
    public Set<Entry<K, DsonValue>> entrySet() {
        return valueMap.entrySet();
    }
    // endregion

}