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

package cn.wjybxx.bigcat.common.pb.codec;

import cn.wjybxx.bigcat.common.EnumUtils;
import cn.wjybxx.bigcat.common.IndexableEnum;
import cn.wjybxx.bigcat.common.IndexableEnumMapper;
import cn.wjybxx.bigcat.common.codec.binary.TypeId;

/**
 * @author wjybxx
 * date 2023/4/1
 */
public enum BinaryValueType implements IndexableEnum {

    // 0保留
    // ----------------------------------------- 基本值 -----------------------------

    INT(1, Integer.class),
    LONG(2, Long.class),
    FLOAT(3, Float.class),
    DOUBLE(4, Double.class),
    BOOLEAN(5, Boolean.class),
    STRING(6, String.class),
    NULL(7, null),

    /**
     * 二进制数据
     * tag + length(fixed32) + bytes
     * <p>
     * 1.length固定4个字节，以方便跨语言和框架等，直接使用bytes的情况不常见，增加的开销是不多的
     */
    BINARY(9, byte[].class),

    /**
     * 对象（容器） - 自定义Bean,Map,Collection,Array,Message,Enum等都属于该类型。
     * 普通对象格式：tag + length(fixed32) + (namespace + [classId]) + [tag, value], [tag, value], [tag, value]....
     * Message格式：tag + length(fixed32) + (namespace + classId) + messageBytes
     * <p>
     * 1.length固定4个字节 - 自定义类无法提前计算，以平均增加进3个字节的开销换取通用性和性能 -- pb由于没有多态等问题，是可以计算写前size的
     * 2.typeId固定5个字节，是分开写入的 - 1(namespace) + 4(classId)
     * 3.pb的Message我们总是写入typeId，保持稳定的序列化结果，以方便跨语言等
     * 4.pb的Message载荷部分直接是一个bytes
     * 5.我们以{@link TypeId#INVALID_NAMESPACE}表示未写入typeId
     * <p>
     * Q: 为什么length要放前面？
     * A: length放前面时，对象的内容部分是连续的。
     */
    OBJECT(10, null),
    ;

    private final int number;
    private final Class<?> javaType;

    BinaryValueType(int number, Class<?> javaType) {
        this.number = number;
        this.javaType = javaType;
    }

    private static final IndexableEnumMapper<BinaryValueType> mapper = EnumUtils.mapping(values(), true);

    public static BinaryValueType forNumber(int number) {
        final BinaryValueType valueType = mapper.forNumber(number);
        if (null == valueType) {
            throw new IllegalArgumentException("invalid number " + number);
        }
        return valueType;
    }

    @Override
    public int getNumber() {
        return number;
    }

    public Class<?> getJavaType() {
        return javaType;
    }
}