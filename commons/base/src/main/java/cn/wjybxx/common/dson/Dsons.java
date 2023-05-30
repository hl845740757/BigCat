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

import cn.wjybxx.common.props.IProperties;
import cn.wjybxx.common.props.PropertiesLoader;

import java.util.List;

/**
 * Dson数据结构的一些常量
 *
 * <h3>Object编码</h3>
 * 二进制编码格式：
 * <pre>
 *  length  [namespace classId] + [dsonType + wireType] +  [number  +  idep] + [length] + [subType] + [data] ...
 *  4Bytes   1 Byte    4Bytes       5 bits     3 bits       1~13 bits  3 bits   4 Bytes    1 Byte     0~n Bytes
 *  总长度    uint8                  1 Byte(unit8)             1 ~ 3 VarByte     int32
 * </pre>
 * 1. namespace(uint8)为255时表示未写入classId
 * 2. 数组元素没有fullNumber
 * 3. fullNumber使用varInt编码，1~3个字节
 * 4. 固定长度的属性类型没有length字段
 * 5. 数字没有length字段
 * 6. string是length是uint32变长编码。
 * 7. extInt32、extInt64和extString都是两个简单值的连续写入，因此extString的length不包含subType。
 * 8. binary/Object/Array的length为fixed32编码，以方便扩展 - binary的length包含subType。
 * <p>
 * 文档型编码格式：
 * <pre>
 *  length  [length + classId]  [dsonType + wireType] +  [length + name] +  [length] + [subType] + [data] ...
 *  4Bytes        nBytes          5 bits     3 bits           nBytes         4 Bytes     1 Byte    0~n Bytes
 *  总长度         string            1 Byte(unit8)             string          int32
 * </pre>
 * 文档型编码和二进制的区别：
 * 1.用字符串存储classId，classId按照普通的字符串格式存储(uint32的length + 内容)
 * 2.用字符串存储字段id，name按照普通的字符串格式存储(uint32的length + 内容)
 *
 * <h3>此文档非彼文档</h3>
 * 在Dson中，文档并非常见的Json、Bson这类的文档，在这些文档对象表示法中，Map是看做普通对象的；
 * 但在Dson中，Map只是一个特殊的数组，默认也是按照数组方式序列化的(K,V,K,V..，)，它和普通的对象是不一样的！
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public final class Dsons {

    // region 二进制

    /** 类字段占用的最大比特位数 - 暂不对外开放 */
    private static final int LNUMBER_MAX_BITS = 13;
    /** 类字段最大number */
    public static final short LNUMBER_MAX_VALUE = 8191;

    /** 继承深度占用的比特位 */
    private static final int IDEP_BITS = 3;
    private static final int IDEP_MASK = (1 << IDEP_BITS) - 1;
    /**
     * 支持的最大继承深度 - 7
     * 1.idep的深度不包含Object，没有显式继承其它类的类，idep为0
     * 2.超过7层我认为是你的代码有问题，而不是框架问题
     */
    public static final int IDEP_MAX_VALUE = IDEP_MASK;

    /** {@link DsonType}的最大类型编号 */
    public static final int DSON_TYPE_MAX_VALUE = 31;
    /** {@link DsonType}占用的比特位 */
    private static final int DSON_TYPE_BITES = 5;
    /** {@link WireType}占位的比特位数 */
    private static final int WIRETYPE_BITS = 3;
    private static final int WIRETYPE_MASK = (1 << WIRETYPE_BITS) - 1;

    /** 完整类型信息占用的比特位数 */
    private static final int FULL_TYPE_BITS = DSON_TYPE_BITES + WIRETYPE_BITS;
    private static final int FULL_TYPE_MASK = (1 << FULL_TYPE_BITS) - 1;

    /** 支持读取为bytes和直接写入bytes的数据类型 */
    public static final List<DsonType> VALUE_BYTES_TYPES = List.of(DsonType.STRING,
            DsonType.BINARY, DsonType.ARRAY, DsonType.OBJECT);

    // fieldNumber

    /** 计算一个类的继承深度 */
    public static int calIdep(Class<?> clazz) {
        if (clazz.isInterface() || clazz.isPrimitive()) {
            throw new IllegalArgumentException();
        }
        if (clazz == Object.class) {
            return 0;
        }
        int r = -1; // 去除Object；简单说：Object和Object的直接子类的idep都记为0，这很有意义。
        while ((clazz = clazz.getSuperclass()) != null) {
            r++;
        }
        return r;
    }

    /**
     * @param idep    继承深度[0~7]
     * @param lnumber 字段在类本地的编号
     * @return fullNumber 字段的完整编号
     */
    public static int makeFullNumber(int idep, int lnumber) {
        return (lnumber << IDEP_BITS) | idep;
    }

    public static int lnumberOfFullNumber(int fullNumber) {
        return fullNumber >>> IDEP_BITS;
    }

    public static byte idepOfFullNumber(int fullNumber) {
        return (byte) (fullNumber & IDEP_MASK);
    }

    public static int makeFullNumberZeroIdep(int lnumber) {
        return lnumber << IDEP_BITS;
    }

    // fullType

    /**
     * @param dsonType 数据类型
     * @param wireType 特殊编码类型
     * @return fullType 完整类型
     */
    public static int makeFullType(DsonType dsonType, WireType wireType) {
        return (dsonType.getNumber() << WIRETYPE_BITS) | wireType.getNumber();
    }

    /**
     * @param dsonType 数据类型 5bits[0~31]
     * @param wireType 特殊编码类型 3bits[0~7]
     * @return fullType 完整类型
     */
    public static int makeFullType(int dsonType, int wireType) {
        return (dsonType << WIRETYPE_BITS) | wireType;
    }

    public static int dsonTypeOfFullType(int fullType) {
        return fullType >>> WIRETYPE_BITS;
    }

    public static int wireTypeOfFullType(int fullType) {
        return (fullType & WIRETYPE_MASK);
    }

    // classId
    public static long makeClassGuid(int namespace, int classId) {
        return ((long) namespace << 32) | ((long) classId & 0xFF_FF_FF_FFL);
    }

    public static int namespaceOfClassGuid(long guid) {
        return (int) (guid >>> 32);
    }

    public static int lclassIdOfClassGuid(long guid) {
        return (int) guid;
    }

    // endregion

    // region 文档

    /**
     * 在文档数据解码中是否启用 字段字符串池化
     * 启用池化在大量相同字段名时可能很好地降低内存占用，默认开启
     * 字段名几乎都是常量，因此命中率几乎百分之百。
     */
    public static final boolean enableFieldIntern;
    /**
     * 在文档数据解码中是否启用 类型别名字符串池化
     * 类型数通常不多，开启池化对编解码性能影响较小，默认开启
     * 类型别名也基本是常量，因此命中率几乎百分之百
     */
    public static final boolean enableClassIntern;

    static {
        IProperties properties = PropertiesLoader.wrapProperties(System.getProperties());
        enableFieldIntern = properties.getAsBool("cn.wjybxx.common.dson.enableFieldIntern", true);
        enableClassIntern = properties.getAsBool("cn.wjybxx.common.dson.enableClassIntern", true);
    }

    /** 文档型编码中，一个name最大字节数 */
    public static final int NAME_MAX_BYTES = 32767;

    public static String internField(String fieldName) {
        return (fieldName.length() <= 32 && enableFieldIntern) ? fieldName.intern() : fieldName;
    }

    public static String internClass(String className) {
        // 长度异常的数据不池化
        return (className.length() <= 128 && enableClassIntern) ? className.intern() : className;
    }
    // endregion

    // region 公共方法

    // endregion
}