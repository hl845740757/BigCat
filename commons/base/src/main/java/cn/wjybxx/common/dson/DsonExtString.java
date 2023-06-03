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
import java.util.Objects;

/**
 * 字符串的简单扩展
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public class DsonExtString extends DsonValue implements Comparable<DsonExtString> {

    private final int type;
    private final String value;

    public DsonExtString(int type, String value) {
        this.type = type;
        this.value = Objects.requireNonNull(value);
    }

    public int getType() {
        return type;
    }

    public String getValue() {
        return value;
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.EXT_STRING;
    }

    //
    @Override
    public int compareTo(DsonExtString that) {
        int r = Integer.compare(type, that.type);
        if (r != 0) {
            return r;
        }
        return value.compareTo(that.value);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonExtString that = (DsonExtString) o;

        if (type != that.type) return false;
        return value.equals(that.value);
    }

    @Override
    public int hashCode() {
        int result = type;
        result = 31 * result + value.hashCode();
        return result;
    }

    @Override
    public String toString() {
        return "DsonExtString{" +
                "type=" + type +
                ", value='" + value + '\'' +
                '}';
    }
}