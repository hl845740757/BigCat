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
import java.util.Arrays;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/19
 */
public class DsonBinary extends DsonValue {

    private final byte type;
    private final byte[] data;

    public DsonBinary(DsonBinaryType type, byte[] data) {
        this(type.getValue(), data);
    }

    public DsonBinary(byte[] data) {
        this((byte) 0, data);
    }

    public DsonBinary(byte type, byte[] data) {
        if (type < 0) throw new IllegalArgumentException("invalid type " + type);
        this.type = type;
        this.data = Objects.requireNonNull(data);
    }

    public byte getType() {
        return type;
    }

    /** 最好不要持有data的引用，否则可能由于共享数据产生错误 */
    public byte[] getData() {
        return data;
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.BINARY;
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonBinary that = (DsonBinary) o;

        if (type != that.type) return false;
        return Arrays.equals(data, that.data);
    }

    @Override
    public int hashCode() {
        int result = type;
        result = 31 * result + Arrays.hashCode(data);
        return result;
    }

    @Override
    public String toString() {
        return "DsonBinary{" +
                "type=" + type +
                ", data=" + Arrays.toString(data) +
                '}';
    }

}