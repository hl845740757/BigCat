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

package cn.wjybxx.common.dson;

/**
 * Long的扩展类型
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public enum DsonExtInt64Type {

    /** 等同普通的Int64，只是装箱了 */
    NORMAL(0),
    /** 用户自定义类型 */
    USER_DEFINED1(1),
    USER_DEFINED2(2),
    USER_DEFINED3(3),
    USER_DEFINED4(4),
    USER_DEFINED5(5),
    USER_DEFINED6(6),
    USER_DEFINED7(7),
    USER_DEFINED8(8),
    USER_DEFINED9(9),

    /**
     * 日期时间（本地时区的 epoch millis）
     * int64的特殊扩展，由于日期时间使用的频率也较高，因此提供快捷定义方式
     * PS：删除了时间戳支持，时间戳现在的单位普遍是秒，而我日常其实多使用毫秒。
     */
    DATETIME(10),
    ;

    private final byte number;

    DsonExtInt64Type(int number) {
        this.number = (byte) number;
    }

    public byte getValue() {
        return number;
    }

}