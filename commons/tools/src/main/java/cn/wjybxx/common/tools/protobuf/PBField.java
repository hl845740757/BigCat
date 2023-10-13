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

package cn.wjybxx.common.tools.protobuf;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * 消息中的字段
 *
 * @author wjybxx
 * date - 2023/9/27
 */
public class PBField extends PBElement {

    public static final int MODIFIER_OPTIONAL = 1;
    public static final int MODIFIER_REQUIRED = 2;
    public static final int MODIFIER_REPEATED = 3;

    /** 数据类型 */
    private String type;
    /** Map结构的key类型 */
    private String keyType;
    /** Map结构的Value类型 */
    private String valueType;

    /** 数字id */
    private int number;
    /** 修饰符 -- 默认为optional */
    private int modifier = MODIFIER_OPTIONAL;

    // region

    @Nonnull
    @Override
    public PBElementKind getKind() {
        return PBElementKind.FIELD;
    }

    /** Map是另一种形式的repeated结构 */
    public boolean isMap() {
        return Objects.equals(type, "map");
    }

    /** 是否是数组字段 */
    public boolean isRepeated() {
        return modifier == MODIFIER_REPEATED;
    }

    // endregion

    public String getType() {
        return type;
    }

    public PBField setType(String type) {
        this.type = type;
        return this;
    }

    public String getKeyType() {
        return keyType;
    }

    public PBField setKeyType(String keyType) {
        this.keyType = keyType;
        return this;
    }

    public String getValueType() {
        return valueType;
    }

    public PBField setValueType(String valueType) {
        this.valueType = valueType;
        return this;
    }

    public int getNumber() {
        return number;
    }

    public PBField setNumber(int number) {
        this.number = number;
        return this;
    }

    public int getModifier() {
        return modifier;
    }

    public PBField setModifier(int modifier) {
        this.modifier = modifier;
        return this;
    }

    @Override
    protected void toString(StringBuilder sb) {
        sb.append(", type='").append(type).append('\'')
                .append(", keyType='").append(keyType).append('\'')
                .append(", valueType='").append(valueType).append('\'')
                .append(", number=").append(number)
                .append(", modifier=").append(modifier);
    }

}