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

package cn.wjybxx.common.codec;

import cn.wjybxx.common.codec.binary.BinaryObjectReader;
import cn.wjybxx.common.codec.binary.BinaryObjectWriter;
import cn.wjybxx.common.codec.document.DocumentObjectReader;
import cn.wjybxx.common.codec.document.DocumentObjectWriter;
import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.WireType;
import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;

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
 * 5.可用于通知注解处理器不自动读或写
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
@Target({ElementType.FIELD})
@Retention(RetentionPolicy.RUNTIME)
public @interface FieldImpl {

    /** 用于文档型序列化时字段名 */
    String name() default "";

    // region 特殊命名处理

    /** 指定字段的getter方法，避免由于字段名特殊或特殊封装情况下无法自动序列化的问题 */
    String getter() default "";

    /** 指定字段的setter方法 */
    String setter() default "";

    // endregion

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
     *  numbers => 0,1,5,6,7
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
     * 数据关联的{@link DsonType}，配合{@link #dsonSubType()}使用
     * 1.可声明 byte[] 的子类型
     * 2.可将普通的int32/int64/double/string声明为带标签的对应结构
     */
    DsonType dsonType() default DsonType.END_OF_OBJECT;

    /**
     * 用于声明子类型，项目可以定义一个自己的常量类
     * {@link DsonType#BINARY}
     * {@link DsonType#EXT_INT32}
     * {@link DsonType#EXT_INT64}
     * {@link DsonType#EXT_STRING}
     */
    int dsonSubType() default 0;

    /** 数字类型字段的文本格式 */
    NumberStyle numberStyle() default NumberStyle.SIMPLE;

    /** 字符串类型字段的文本格式 */
    StringStyle stringStyle() default StringStyle.AUTO;

    /**
     * 对象类型字段的文本格式
     * 注意：该属性只有显式声明才有效，当未声明该属性时，将使用目标类型的默认格式，而不是使用这里的默认值。
     */
    ObjectStyle objectStyle() default ObjectStyle.INDENT;

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
     * 1.参数限定为两个，第一个参数为writer，第二个为name
     * 2.参数限定为{@link BinaryObjectWriter}或{@link DocumentObjectWriter}
     * 示例：
     * <pre>{@code
     *      public void writeName(BinaryObjectWriter writer, int name) {
     *          writer.writeString(name, this.name);
     *      }
     *      public void writeName(DocumentObjectWriter writer, String name) {
     *          writer.writeString(name, this.name);
     *      }
     * }
     * </pre>
     * 注意：如果声明了该属性，且指向空字符串，则表示告知注解处理器不自动写。
     */
    String writeProxy() default "";

    /**
     * 读代理：自定义读方法
     * 1.参数限定为两个，第一个参数为reader，第二个为name
     * 2.参数限定为{@link BinaryObjectReader}或{@link DocumentObjectReader}
     * 3.对于有特殊构造过程的字段是很有帮助的，也可以进行类型转换。
     * <p>
     * 示例：
     * <pre>{@code
     *      public void readName(BinaryObjectReader reader, int name) {
     *          this.name = reader.readString(name);
     *      }
     *      public void readName(DocumentObjectReader reader, String name) {
     *          this.name = reader.readString(name);
     *      }
     * }
     * </pre>
     * 注意：如果声明了该属性，且指向空字符串，则表示告知注解处理器不自动读。
     */
    String readProxy() default "";

    // endregion
}