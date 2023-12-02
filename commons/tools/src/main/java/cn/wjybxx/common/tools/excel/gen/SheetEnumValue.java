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

package cn.wjybxx.common.tools.excel.gen;

import java.util.Objects;

/**
 * 表格定义的简单枚举值
 *
 * @author wjybxx
 * date - 2023/10/15
 */
public class SheetEnumValue {

    /** 枚举名 */
    public final String name;
    /** 枚举数 */
    public final int number;
    /** 字符串值 */
    public final String value;
    /** 注释 */
    public final String comment;

    public SheetEnumValue(String name, int number, String comment) {
        this.name = name;
        this.number = number;
        this.comment = comment;
        this.value = null;
    }

    public SheetEnumValue(String name, String value, String comment) {
        this.name = name;
        this.value = Objects.requireNonNull(value);
        this.comment = comment;
        this.number = -1;
    }

    public final boolean isStringEnum() {
        return value != null;
    }
}