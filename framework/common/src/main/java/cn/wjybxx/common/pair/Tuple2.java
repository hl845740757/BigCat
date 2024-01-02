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

package cn.wjybxx.common.pair;

import java.util.Map;
import java.util.Objects;

/**
 * 二元组
 *
 * @author wjybxx
 * date 2023/3/31
 */
public final class Tuple2<A, B> implements Map.Entry<A, B> {

    private final A first;
    private final B second;

    public Tuple2(A first, B second) {
        this.first = first;
        this.second = second;
    }

    public static <A, B> Tuple2<A, B> of(A first, B second) {
        return new Tuple2<>(first, second);
    }

    public A getFirst() {
        return first;
    }

    public B getSecond() {
        return second;
    }

    @Override
    public A getKey() {
        return first;
    }

    @Override
    public B getValue() {
        return second;
    }

    @Override
    public B setValue(B value) {
        throw new UnsupportedOperationException();
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        Tuple2<?, ?> tuple2 = (Tuple2<?, ?>) o;

        if (!Objects.equals(first, tuple2.first)) return false;
        return Objects.equals(second, tuple2.second);
    }

    @Override
    public int hashCode() {
        int result = first != null ? first.hashCode() : 0;
        result = 31 * result + (second != null ? second.hashCode() : 0);
        return result;
    }

    @Override
    public String toString() {
        return "Tuple2{" +
                "first=" + first +
                ", second=" + second +
                '}';
    }

}