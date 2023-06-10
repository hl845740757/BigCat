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

import cn.wjybxx.common.CollectionUtils;

import javax.annotation.Nonnull;
import java.util.*;

/**
 * 不再采用二进制和Doc再衍生两个子类的方式，对性能的影响不大，因为直接使用DsonObject的情况较少，但带来的维护成本巨高。
 *
 * @author wjybxx
 * date - 2023/4/21
 */
public abstract class DsonObject<K> extends DsonValue implements Map<K, DsonValue> {

    final Map<K, DsonValue> valueMap;

    DsonObject(Map<K, DsonValue> valueMap) {
        this.valueMap = valueMap;
    }

    //

    /**
     * 创建一个禁止修改的DsonObject
     * 暂时未实现为深度的不可变，存在一些困难，主要是相互引用的问题
     */
    public static <K> DsonObject<K> toImmutable(DsonObject<K> src) {
        return ImmutableDsons.dsonObject(src);
    }

    public static <K> DsonObject<K> empty() {
        return ImmutableDsons.dsonObject();
    }

    @Nonnull
    public abstract DsonHeader<K> getHeader();

    public abstract DsonObject<K> setHeader(DsonHeader<K> header);

    /**
     * 返回对象的第一个键
     *
     * @throws NoSuchElementException 如果对象为空
     */
    public K firstKey() {
        return CollectionUtils.firstKey(valueMap);
    }

    public Map<K, DsonValue> getValueMap() {
        return Collections.unmodifiableMap(valueMap);
    }

    /** @return this */
    public DsonObject<K> append(K key, DsonValue value) {
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
        return DsonType.OBJECT;
    }

    static <K> void checkKeyValue(K key, DsonValue value) {
        if (key == null) {
            throw new IllegalArgumentException("key cant be null");
        }
        if (value == null) {
            throw new IllegalArgumentException("value cant be null");
        }
    }

    // region equals
    // 默认只比较value，不比较header和类型

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        return o instanceof DsonObject<?> that && valueMap.equals(that.valueMap);
    }

    @Override
    public int hashCode() {
        return valueMap.hashCode();
    }

    @Override
    public String toString() {
        return getClass().getSimpleName() + "{" +
                "valueMap=" + valueMap +
                ", header=" + getHeader() +
                '}';
    }
    // endregion

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