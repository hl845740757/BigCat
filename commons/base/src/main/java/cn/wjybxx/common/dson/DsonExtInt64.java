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
 * long值的简单扩展
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public class DsonExtInt64 extends DsonValue implements Comparable<DsonExtInt64> {

    private final byte type;
    private final long value;

    public DsonExtInt64(DsonExtInt64Type type, long value) {
        this(type.getValue(), value);
    }

    public DsonExtInt64(byte type, long value) {
        if (type < 0) throw new IllegalArgumentException("invalid type " + type);
        this.type = type;
        this.value = value;
    }

    public byte getType() {
        return type;
    }

    public long getValue() {
        return value;
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.EXT_INT64;
    }

    //
    @Override
    public int compareTo(DsonExtInt64 that) {
        int r = Byte.compare(type, that.type);
        if (r != 0) {
            return r;
        }
        return Long.compare(value, that.value);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonExtInt64 that = (DsonExtInt64) o;

        if (type != that.type) return false;
        return value == that.value;
    }

    @Override
    public int hashCode() {
        int result = type;
        result = 31 * result + (int) (value ^ (value >>> 32));
        return result;
    }

    @Override
    public String toString() {
        return "DsonExtInt64{" +
                "type=" + type +
                ", value=" + value +
                '}';
    }
}
