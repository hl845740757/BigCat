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

package cn.wjybxx.common.dson.text;

import java.util.Objects;

/**
 * token可能记录位置更有助于排查问题
 *
 * @author wjybxx
 * date - 2023/6/2
 */
public class DsonToken {

    public static final DsonToken EOF = new DsonToken(TokenType.EOF, "eof");

    public static final DsonToken BEGIN_OBJECT_WITH_HEADER = new DsonToken(TokenType.BEGIN_OBJECT, "{@");
    public static final DsonToken BEGIN_OBJECT = new DsonToken(TokenType.BEGIN_OBJECT, "{");
    public static final DsonToken END_OBJECT = new DsonToken(TokenType.END_OBJECT, "}");

    public static final DsonToken BEGIN_ARRAY_WITH_HEADER = new DsonToken(TokenType.BEGIN_ARRAY, "[@");
    public static final DsonToken BEGIN_ARRAY = new DsonToken(TokenType.BEGIN_ARRAY, "[");
    public static final DsonToken END_ARRAY = new DsonToken(TokenType.END_ARRAY, "]");

    public static final DsonToken COLON = new DsonToken(TokenType.COLON, ":");
    public static final DsonToken COMMA = new DsonToken(TokenType.COMMA, ",");
    public static final DsonToken NULL = new DsonToken(TokenType.NULL, null);
    public static final DsonToken HEADER_OBJECT = new DsonToken(TokenType.HEADER, "@{");

    private final TokenType type;
    private final Object value;

    public DsonToken(TokenType type, Object value) {
        this.type = Objects.requireNonNull(type);
        this.value = value;
    }

    public TokenType getType() {
        return type;
    }

    public Object getValue() {
        return value;
    }

    public String castAsString() {
        return (String) value;
    }

    public char firstChar() {
        String value = (String) this.value;
        return value.charAt(0);
    }

    public char lastChar() {
        String value = (String) this.value;
        return value.charAt(value.length() - 1);
    }

    //
    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonToken dsonToken = (DsonToken) o;

        if (type != dsonToken.type) return false;
        return Objects.equals(value, dsonToken.value);
    }

    @Override
    public int hashCode() {
        int result = type.hashCode();
        result = 31 * result + (value != null ? value.hashCode() : 0);
        return result;
    }

    @Override
    public String toString() {
        return "DsonToken{ " +
                "type= " + type +
                ", value= " + value +
                " }";
    }
}