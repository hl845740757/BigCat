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

package cn.wjybxx.common.codec.binary;

import cn.wjybxx.common.codec.ConverterUtils;
import cn.wjybxx.common.codec.TypeArgInfo;

import javax.annotation.Nonnull;

/**
 * 自定义POJO对象编解码实现接口，该接口与{@link BinaryPojoCodec}协同工作，为典型的桥接模式。
 * <p>
 * 实体类序列化工具类，每一个{@link BinaryPojoCodecImpl}只负责一个固定类型的解析。
 * 生成的代码会实现该接口，用户手动实现编解码时也实现该接口。
 * <br>-------------------------------什么时候手写实现？-----------------------<br>
 * 1. 一般而言，建议使用注解{@link BinarySerializable}，并遵循相关规范，由注解处理器生成的类负责解析，而不是手写实现{@link BinaryPojoCodecImpl}。
 * 一旦手写实现，必须持久的进行维护。
 * 2. 如果对象存在复杂的构造过程的时候，考虑手动实现。
 * 3. 对象存在final字段时，考虑手动实现（最好只负责final字段解析）
 * 4. 单例和池化对象，如果想解析时避免创建新的对象，可以自实现Codec处理。
 * <br>-------------------------------实现时要注意什么？----------------------<br>
 * 1. 必须保证线程安全，最好是无状态的。
 * 2. 最好实现为目标类的静态内部类，且最好是private级别，不要暴露给外层。
 * 3. 需提供无参构造方法(可以private，否则不会被扫描到) - 反射创建对象。
 * 4. 在二进制编码中是按照顺序读写的，因此要小心读的顺序，如果你调整了字段顺序。
 *
 * @author wjybxx
 * date 2023/3/31
 */
public interface BinaryPojoCodecImpl<T> {

    /**
     * 获取负责编解码的类对象
     */
    @Nonnull
    Class<T> getEncoderClass();

    /**
     * 将对象写入输出流。
     *
     * @param typeArgInfo 类型描述信息，用于判断元素的类型是否写入，是一个上下文
     */
    void writeObject(T instance, BinaryObjectWriter writer, TypeArgInfo<?> typeArgInfo);

    /**
     * 从输入流中解析指定对象。
     *
     * @param typeArgInfo 类型描述信息，用于精确解析持有的元素，是一个上下文
     */
    T readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo);

    /**
     * 当前对象是否按照数组格式编码
     * 默认情况下，Map是被看做普通的数组的
     */
    default boolean isWriteAsArray() {
        return ConverterUtils.isEncodeAsArray(getEncoderClass());
    }

    /**
     * 该方法用于告知{@link BinaryPojoCodec}是否自动调用以下方法
     * {@link BinaryObjectWriter#writeStartObject(Object, TypeArgInfo)} ()}
     * {@link BinaryObjectWriter#writeEndObject()}
     * {@link BinaryObjectReader#readStartObject(TypeArgInfo)}
     * {@link BinaryObjectReader#readEndObject()}
     * <p>
     * Q：禁用该属性有什么用？
     * A: 对于读：你可以使用另一个codec来解码当前二进制对象；对于写；你可以将当前转换为另一个对象，然后再使用对应的codec进行编码。
     * 即：关闭该属性可以实现读替换(readReplace)和写替换(writeReplace)功能。
     * 另外，还可以自行决定是写为Array还是Object。
     */
    default boolean autoStartEnd() {
        return true;
    }

}