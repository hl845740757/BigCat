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

/**
 * protobuf枚举值
 *
 * @author wjybxx
 * date - 2023/9/27
 */
public class PBEnumValue extends PBElement {

    /** 数字id */
    private int number;

    @Nonnull
    @Override
    public PBElementKind getKind() {
        return PBElementKind.ENUM_VALUE;
    }

    public int getNumber() {
        return number;
    }

    public PBEnumValue setNumber(int number) {
        this.number = number;
        return this;
    }

    @Override
    protected void toString(StringBuilder sb) {
        sb.append(", number=").append(number);
    }
}