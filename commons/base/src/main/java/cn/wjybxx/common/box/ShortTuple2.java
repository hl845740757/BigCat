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

package cn.wjybxx.common.box;

import cn.wjybxx.common.dson.AutoTypeArgs;
import cn.wjybxx.common.dson.Dsons;
import cn.wjybxx.common.dson.binary.BinaryObjectReader;
import cn.wjybxx.common.dson.binary.BinarySerializable;

/**
 * @author wjybxx
 * date 2023/3/31
 */
@AutoTypeArgs
@BinarySerializable
public class ShortTuple2 {

    private final short first;
    private final short second;

    public ShortTuple2(short first, short second) {
        this.first = first;
        this.second = second;
    }

    public ShortTuple2(BinaryObjectReader reader) {
        this.first = reader.readShort(Dsons.makeFullNumberZeroIdep(0));
        this.second = reader.readShort(Dsons.makeFullNumberZeroIdep(1));
    }

    public short getFirst() {
        return first;
    }

    public short getSecond() {
        return second;
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        ShortTuple2 that = (ShortTuple2) o;

        if (first != that.first) return false;
        return second == that.second;
    }

    @Override
    public int hashCode() {
        int result = first;
        result = 31 * result + (int) second;
        return result;
    }

    @Override
    public String toString() {
        return "ShortTuple2{" +
                "first=" + first +
                ", second=" + second +
                '}';
    }

}