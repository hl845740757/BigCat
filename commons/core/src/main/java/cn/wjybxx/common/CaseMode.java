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

/**
 * 大小写模式
 *
 * @author wjybxx
 * date 2023/4/1
 */
public enum CaseMode {

    UPPER_CASE,
    LOWER_CASE;

    public boolean isUpperCase() {
        return this == UPPER_CASE;
    }

    public boolean isLowerCase() {
        return this == LOWER_CASE;
    }

    public CaseMode invert() {
        return this == UPPER_CASE ? LOWER_CASE : UPPER_CASE;
    }

    public String toCase(String value) {
        if (this == UPPER_CASE) {
            return value.toUpperCase();
        } else {
            return value.toLowerCase();
        }
    }

}