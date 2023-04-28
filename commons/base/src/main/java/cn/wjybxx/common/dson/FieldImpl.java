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

import cn.wjybxx.common.dson.binary.BinaryObjectReader;
import cn.wjybxx.common.dson.binary.BinaryObjectWriter;
import cn.wjybxx.common.dson.document.DocumentObjectReader;
import cn.wjybxx.common.dson.document.DocumentObjectWriter;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * 该注解的作用：
 * 1.用于简单情况确定字段的实现类型，以实现精确解析 {@link #value()}。
 * 2.读写代理可以让用户对字段进行细粒度的控制，eg：多态问题，读写替换问题，lazyDecode...
 *
 * <h3>value的应用场景</h3>
 * {@link #value()}属性用于简单多态解决方案。
 * 1.非顶层接口的抽象Map和Collection的精确解析，用户指定实现类可调用实现类的构造方法创建实例。
 * 2.非抽象类（包含自定义）类也可以指定实现类，在解码时可替换为子类实例。
 *
 * <h3>读写代理的应用场景</h3>
 * 读写代理可以实现字段的高自由度读写。
 * 1.可以解决上面提到的多态问题。
 * 2.可以实现字段的读写替换：由于需要自行调用writeStart，因此可以替换要写入的内容。
 * 3.可以实现字段的延迟解析：通过{@link BinaryObjectReader#readValueAsBytes(int)} -- 目前仅二进制编解码接口提供支持，
 * 4.字段读后的转换：如果字段的默认解码类型不符合要求，可以在读写代理中处理。
 *
 * <h3>多层嵌套类型</h3>
 * 举个栗子：{@code Map<Integer, Map<String,Object>>}
 * 对于这种类型，要想通过声明的泛型信息精确解析是很困难的，而且泛型参数很可能是抽象的。
 * 要想简单可靠的解决这个问题，用户需要让泛型对应的实例尽可能在{@code CodecRegistry}中，
 * 运行时类型在{@code CodecRegistry}中的对象是可以精确解析的。
 *
 * @author wjybxx
 * date 2023/3/31
 */
@Retention(RetentionPolicy.SOURCE)
@Target({ElementType.FIELD})
public @interface FieldImpl {

    // region tag

    /**
     * 字段在当前类中的编号，用于二进制序列化
     * 1.用户的编号取值范围 [0, 8191]。
     * 2.该值不受继承关系影响，每个类都从0开始。
     * 3.修改该值可能产生兼容性问题。
     * 4.默认情况下，number按照定义顺序从0开始递增，遇见自定义idep或number后；从当前当前idep和当前number+1开始递增。
     * <pre>{code
     *  class Example {
     *      int fa;
     *      int fb;
     *      FieldImpl(number = 5)
     *      int fc;
     *      int fd;
     *  }
     *  numbers => 0,1,5,6
     * }
     * </pre>
     */
    int number() default -1;

    /**
     * 字段所属的继承深度
     * 注意：
     * 1.你通常不应该使用该属性，只有接收方和发送方的继承深度不一致时才可以使用属性、
     * 2.使用idep属性时，应当保持超类的字段在前（递归规则）。
     * 3.取值范围[0, 7] - idep的深度不包含Object
     */
    int idep() default -1;

    /**
     * 数字类型属性的编码格式
     * 设定合适的类型有助于优化二进制编码，修改该值不产生兼容性问题。
     */
    WireType wireType() default WireType.VARINT;

    /**
     * 数据关联的{@link DsonType}，通常不需要声明该属性，需要特殊序列化的字段才需要。
     * 1.可将一个int值声明为特殊的类型，使用{@link DsonType#EXT_INT32}时需同时指定{@link #extInt32Type()}
     * 2.可将一个long值声明为特殊的类型，使用{@link DsonType#EXT_INT64}时需同时指定{@link #extInt64Type()}
     * 3.可将一个String值声明为特殊的类型，使用{@link DsonType#EXT_STRING}时需同时指定{@link #extStringType()}
     * 4.可将一个byte[]值声明为特殊的类型，使用{@link DsonType#BINARY}时需同时指定{@link #binaryType()}
     */
    DsonType dsonType() default DsonType.END_OF_OBJECT;

    /** 二进制的类型 */
    DsonBinaryType binaryType() default DsonBinaryType.NORMAL;

    /** int值的特殊释义 */
    DsonExtInt32Type extInt32Type() default DsonExtInt32Type.NORMAL;

    /** long值的特殊释义 */
    DsonExtInt64Type extInt64Type() default DsonExtInt64Type.NORMAL;

    /** String值的特殊释义 */
    DsonExtStringType extStringType() default DsonExtStringType.NORMAL;

    // endregion

    // region 多态解析

    /**
     * 字段的实现类，用于生成{@link TypeArgInfo#factory}
     * <h3>限制</h3>
     * 1. 必须是具体类型
     * 2. 必须拥有public无参构造方法 -- 生成的代码可访问。
     * 3. 使用{@link #readProxy()}时不进行任何限制。
     * <p>
     * PS：自定义类型也可以指定实现类，但实现类需要包含无参构造参数
     */
    Class<?> value() default Object.class;

    /**
     * 写代理：自定义写方法
     * 1.必须是单参实例方法
     * 2.参数限定为{@link BinaryObjectWriter}或{@link DocumentObjectWriter}
     * 示例：
     * <pre>{@code
     *      public void writeName(BinaryWriter writer) {
     *          writer.writeString(this.name);
     *      }
     * }
     * </pre>
     */
    String writeProxy() default "";

    /**
     * 读代理：自定义读方法
     * 1.必须是单参实例方法
     * 2.参数限定为{@link BinaryObjectReader}或{@link DocumentObjectReader}
     * 3.对于有特殊构造过程的字段是很有帮助的，也可以进行类型转换。
     * <p>
     * 示例：
     * <pre>{@code
     *      public void readName(BinaryReader reader) {
     *          this.name = reader.readString();
     *      }
     * }
     * </pre>
     */
    String readProxy() default "";

    // endregion
}