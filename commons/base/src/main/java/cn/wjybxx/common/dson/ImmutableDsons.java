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
import java.util.List;
import java.util.Map;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/5/30
 */
final class ImmutableDsons {

    public static <K> DsonHeader<K> header(DsonHeader<K> src) {
        Objects.requireNonNull(src);
        if (src instanceof ImmutableHeader<K>) {
            return src;
        }
        Map<K, DsonValue> valueMap = CollectionUtils.toImmutableLinkedHashMap(src.getValueMap());
        return new ImmutableHeader<>(valueMap);
    }

    public static <K> DsonObject<K> dsonObject(DsonObject<K> src) {
        Objects.requireNonNull(src);
        if (src instanceof ImmutableDsonObject<K>) {
            return src;
        }
        Map<K, DsonValue> valueMap = CollectionUtils.toImmutableLinkedHashMap(src.valueMap);
        DsonHeader<K> header = header(src.getHeader());
        return new ImmutableDsonObject<>(valueMap, header);
    }

    public static <K> DsonArray<K> dsonArray(DsonArray<K> src) {
        Objects.requireNonNull(src);
        if (src instanceof ImmutableDsonArray<K>) {
            return src;
        }
        DsonHeader<K> header = header(src.getHeader());
        List<DsonValue> values = List.copyOf(src.getValues());
        return new ImmutableDsonArray<>(values, header);
    }

    @SuppressWarnings("unchecked")
    public static <K> DsonArray<K> dsonArray() {
        return (DsonArray<K>) EMPTY_ARRAY;
    }

    @SuppressWarnings("unchecked")
    public static <K> DsonObject<K> dsonObject() {
        return (DsonObject<K>) EMPTY_OBJECT;
    }

    @SuppressWarnings("unchecked")
    public static <K> DsonHeader<K> header() {
        return (DsonHeader<K>) EMPTY_HEADER;
    }

    //
    private static final DsonHeader<?> EMPTY_HEADER = new ImmutableHeader<>(Map.of());
    private static final DsonArray<?> EMPTY_ARRAY = new ImmutableDsonArray<>(List.of(), EMPTY_HEADER);
    private static final DsonObject<?> EMPTY_OBJECT = new ImmutableDsonObject<>(Map.of(), EMPTY_HEADER);

    private static class ImmutableDsonObject<K> extends DsonObject<K> {

        private final DsonHeader<K> header;

        private ImmutableDsonObject(Map<K, DsonValue> valueMap,
                                    DsonHeader<K> header) {
            super(valueMap);
            this.header = header;
        }

        @Nonnull
        @Override
        public DsonHeader<K> getHeader() {
            return header;
        }

        @Override
        public DsonObject<K> setHeader(DsonHeader<K> header) {
            throw new UnsupportedOperationException();
        }

        @Override
        public Map<K, DsonValue> getValueMap() {
            return valueMap;
        }

    }

    private static class ImmutableHeader<K> extends DsonHeader<K> {

        private ImmutableHeader(Map<K, DsonValue> valueMap) {
            super(valueMap);
        }

        @Override
        public Map<K, DsonValue> getValueMap() {
            return valueMap;
        }

    }

    private static class ImmutableDsonArray<K> extends DsonArray<K> {

        private final DsonHeader<K> header;

        public ImmutableDsonArray(List<DsonValue> values, DsonHeader<K> header) {
            super(values);
            this.header = header;
        }

        @Nonnull
        @Override
        public DsonHeader<K> getHeader() {
            return header;
        }

        @Override
        public DsonArray<K> setHeader(DsonHeader<K> header) {
            throw new UnsupportedOperationException();
        }

        @Override
        public List<DsonValue> getValues() {
            return values;
        }
    }

}