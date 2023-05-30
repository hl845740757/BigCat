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

/**
 * @author wjybxx
 * date - 2023/4/19
 */
public class DsonBool extends DsonValue implements Comparable<DsonBool> {

    public static final DsonBool TRUE = new DsonBool(true);
    public static final DsonBool FALSE = new DsonBool(false);

    @FieldImpl(getter = "getValue")
    private final boolean value;

    public DsonBool(boolean value) {
        this.value = value;
    }

    public boolean getValue() {
        return value;
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.BOOLEAN;
    }

    public static DsonBool valueOf(final boolean value) {
        return value ? TRUE : FALSE;
    }

    //
    @Override
    public int compareTo(DsonBool that) {
        return Boolean.compare(value, that.value);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonBool dsonBool = (DsonBool) o;

        return value == dsonBool.value;
    }

    @Override
    public int hashCode() {
        return (value ? 1 : 0);
    }

    @Override
    public String toString() {
        return "DsonBool{" +
                "value=" + value +
                '}';
    }
}