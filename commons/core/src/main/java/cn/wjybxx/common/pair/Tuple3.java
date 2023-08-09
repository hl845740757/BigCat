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

package cn.wjybxx.common.pair;

import java.util.Objects;

/**
 * 三元组
 *
 * @author wjybxx
 * date 2023/3/31
 */
public final class Tuple3<A, B, C> {

    private final Tuple2<A, B> tuple2;
    private final C third;

    public Tuple3(A first, B second, C third) {
        this.tuple2 = new Tuple2<>(first, second);
        this.third = third;
    }

    public static <A, B, C> Tuple3<A, B, C> of(A first, B second, C third) {
        return new Tuple3<>(first, second, third);
    }

    public Tuple2<A, B> asTuple2() {
        return tuple2;
    }

    public A getFirst() {
        return tuple2.getFirst();
    }

    public B getSecond() {
        return tuple2.getSecond();
    }

    public C getThird() {
        return third;
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        Tuple3<?, ?, ?> tuple3 = (Tuple3<?, ?, ?>) o;

        if (!tuple2.equals(tuple3.tuple2)) return false;
        return Objects.equals(third, tuple3.third);
    }

    @Override
    public int hashCode() {
        int result = tuple2.hashCode();
        result = 31 * result + (third != null ? third.hashCode() : 0);
        return result;
    }

    @Override
    public String toString() {
        return "Tuple3{" +
                "first=" + tuple2.getFirst() +
                ", second=" + tuple2.getSecond() +
                ", third=" + third +
                '}';
    }

}