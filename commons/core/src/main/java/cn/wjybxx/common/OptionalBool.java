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

package cn.wjybxx.common;

import java.util.NoSuchElementException;

/**
 * 使用{@link Boolean}类型容易忘记检查null
 * 我发现居然没有类库提供这么一个组件呢 -- 和直接使用包装类型似乎一样。
 *
 * @author wjybxx
 * date - 2023/4/17
 */
public enum OptionalBool implements EnumLite {

    FALSE(false),
    TRUE(true),
    EMPTY(false),
    ;

    private final boolean value;

    OptionalBool(boolean v) {
        this.value = v;
    }

    //
    public static OptionalBool valueOf(Boolean value) {
        if (value == null) return EMPTY;
        return value ? TRUE : FALSE;
    }

    public static OptionalBool valueOf(boolean value) {
        return value ? TRUE : FALSE;
    }
    //

    public boolean getAsBool() {
        if (this == EMPTY) {
            throw new NoSuchElementException("No value present");
        }
        return value;
    }

    public boolean isTrue() {
        return this == TRUE;
    }

    public boolean isFalse() {
        return this == FALSE;
    }

    public boolean isEmpty() {
        return this == EMPTY;
    }

    public boolean isPresent() {
        return this != EMPTY;
    }

    public boolean orElse(boolean value) {
        return this == EMPTY ? value : this.value;
    }

    public boolean orElseThrow() {
        if (this == EMPTY) {
            throw new NoSuchElementException("No value present");
        }
        return value;
    }

    @Override
    public String toString() {
        return "OptionalBool." + name();
    }

    @Override
    public int getNumber() {
        return switch (this) {
            case FALSE -> 0;
            case TRUE -> 1;
            case EMPTY -> 2; // -1不利于序列化
        };
    }

    public static OptionalBool forNumber(int number) {
        return switch (number) {
            case 0 -> FALSE;
            case 1 -> TRUE;
            case 2 -> EMPTY;
            default -> throw new IllegalArgumentException("invalid number " + number);
        };
    }

}