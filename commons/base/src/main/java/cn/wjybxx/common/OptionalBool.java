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
 * 我发现居然没有类库提供这么一个组件呢。
 *
 * @author wjybxx
 * date - 2023/4/17
 */
public enum OptionalBool {

    EMPTY(false),
    TRUE(true),
    FALSE(false);

    private final boolean _value;

    OptionalBool(boolean v) {
        this._value = v;
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
        return _value;
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
        return this == EMPTY ? value : _value;
    }

    public boolean orElseThrow() {
        if (this == EMPTY) {
            throw new NoSuchElementException("No value present");
        }
        return _value;
    }

    @Override
    public String toString() {
        return "OptionalBool." + name();
    }

}