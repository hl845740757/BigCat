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
 * @author wjybxx
 * date - 2023/6/2
 */
public class DsonToken {

    private final DsonTokenType type;
    private final Object value;

    public DsonToken(DsonTokenType type, Object value) {
        this.type = Objects.requireNonNull(type);
        this.value = value;
    }

    public DsonTokenType getType() {
        return type;
    }

    public Object getValue() {
        return value;
    }

    public String castAsString() {
        return (String) value;
    }

    @Override
    public String toString() {
        return "DsonToken[ " +
                "type= " + type +
                ", value= " + value +
                " ]";
    }
}