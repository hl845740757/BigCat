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

/**
 * Dson - 数据对象表示法，不仅仅是序列化。
 * 提供一层中间层，业务可以尽量少地对接各种库 -- 既可以序列化，也可以为其它接口提供适配。
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public enum DsonType {

    /** 对象的结束标识 */
    END_OF_OBJECT(0),

    INT32(1),
    INT64(2),
    FLOAT(3),
    DOUBLE(4),
    BOOLEAN(5),
    STRING(6),
    NULL(7),

    /**
     * 二进制字节数组
     * 我们为二进制字节数组也提供了一个扩展子类型，实现一定程度上的自解释
     */
    BINARY(8),
    /**
     * Int32的扩展类型
     * 基本类型的int无法直接表达其使用目的，需要额外的类型支持；
     * 通过扩展类型，可避免破坏业务代码，可避免用户自行封装Long
     */
    EXT_INT32(9),
    /**
     * Int64的扩展类型
     * 基本类型的long无法直接表达其使用目的，需要额外的类型支持；
     * 通过扩展类型，可避免破坏业务代码，可避免用户自行封装Long
     */
    EXT_INT64(10),
    /**
     * String的扩展类型
     * 基本类型的String无法直接表达其使用目的，需要额外的类型支持；
     * 通过扩展类型，可避免破坏业务代码，可避免用户自行封装Long
     */
    EXT_STRING(11),
    /**
     * 对象引用
     */
    REFERENCE(12),

    /**
     * 对象头信息，与Object类型编码格式类似
     * 但header不可以再直接嵌入header
     */
    HEADER(29),
    /**
     * 数组(v,v,v...)
     */
    ARRAY(30),
    /**
     * 普通对象(k,v,k,v...)
     */
    OBJECT(31),
    ;
    private final int number;

    DsonType(int number) {
        assert number >= 0 && number <= Dsons.DSON_TYPE_MAX_VALUE;
        this.number = number;
    }

    private static final DsonType[] LOOK_UP = new DsonType[Dsons.DSON_TYPE_MAX_VALUE + 1];

    static {
        for (DsonType type : values()) {
            LOOK_UP[type.getNumber()] = type;
        }
    }

    public int getNumber() {
        return number;
    }

    /** header不属于普通意义上的容器 */
    public boolean isContainer() {
        return this == OBJECT || this == ARRAY;
    }

    public static DsonType forNumber(int number) {
        return LOOK_UP[number];
    }

}