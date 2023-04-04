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

package cn.wjybxx.bigcat.common.codec;

import cn.wjybxx.bigcat.common.codec.binary.BinaryCodecRegistry;
import cn.wjybxx.bigcat.common.codec.binary.BinaryReader;
import cn.wjybxx.bigcat.common.codec.binary.BinaryWriter;
import cn.wjybxx.bigcat.common.codec.document.DocumentReader;
import cn.wjybxx.bigcat.common.codec.document.DocumentWriter;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * 该字段
 * <p>
 * 该注解用于控制字段
 * <p>
 * 用户精确控制字段的编解码，实现插入代码到编解码过程。
 * <p>
 * 该注解用于确定字段的实现类型，以实现精确解析。
 * <h3>什么时候需要？</h3>
 * 当一个字段需要序列化或持久化时，如果其声明类型是抽象的，且其运行时类型不在{@code CodecRegistry}中时，我们需要通过该属性获取如何安全的解析对象。
 *
 * <h3>为什么需要？</h3>
 * 它是由多态产生的，当我们在序列化或持久化一个对象时，如果对象的运行时类型不在{@code CodecRegistry}中，我们就无法精确的进行解析。
 * 举个栗子，当我们传输{@link java.util.LinkedList}时，假设它不在{@code CodecRegistry}中，我们就会当做普通集合序列化，
 * 但反序列化时，我们默认反序列化是{@link java.util.ArrayList}，就可以不兼容，这时候就需要用户告诉我们应该如何解析。
 *
 * <p>
 * Q: 那传输类的全限定名行吗？
 * A: 行不通，为什么呢？
 * 举个栗子：当对方发送一个不可变集合时，你即使有全限定名，也没有办法。所以，关键是接收方期望以什么类型接收，它对发送方并不提要求。
 *
 * <h3>{@link #value()} ()}一些限制</h3>
 * 1. 必须是具体类型
 * 2. 必须拥有public无参构造方法。
 * 3. 使用{@link #readProxy()}时不进行任何限制。
 *
 * <h3>多态</h3>
 * 对于一般的集合和Map，{@link #value()}就可以很好的解决问题，如果目标集合没有无参构造方法，或用户想进行有特殊的构造，
 * 可以使用{@link #readProxy()}解决。
 *
 * <h3>多层嵌套类型</h3>
 * 举个栗子：{@code Map<Integer, Map<String,Object>>}
 * 对于这种类型，要想通过声明的泛型信息精确解析是很困难的，而且泛型参数很可能是抽象的。
 * 要想简单可靠的解决这个问题，用户需要让泛型对应的实例尽可能在{@code CodecRegistry}中，
 * 在{@code CodecRegistry}中的对象是可以精确解析的。
 *
 * <p>
 * 对于这种类型，没有办法约束，因此没有办法保证能解析为正确的类型，因此不建议使用多重嵌套的类型。
 * 不过，如果泛型参数运行时类型存在于{@link BinaryCodecRegistry}中，则可以精确解析。
 * 如果，泛型参数不在注册表中，默认的解析类型如果不满足要求，用户可以通过读代理进行二次转换。
 *
 * @author wjybxx
 * date 2023/3/31
 */
@Retention(RetentionPolicy.SOURCE)
@Target({ElementType.FIELD})
public @interface FieldImpl {

    /**
     * 字段的实现类
     */
    Class<?> value() default Object.class;

    /**
     * 写代理：自定义写方法
     * 1.必须是静态方法
     * 2.两个参数，第一个参数为{@link BinaryWriter}或{@link DocumentWriter}，第二个参数为所属的对象实例
     * <p>
     * 示例：
     * <pre>
     * {@code
     *      public static void writeName(BinaryWriter writer, MyClass instance) {
     *          writer.writeString(instance.name);
     *      }
     * }
     * </pre>
     */
    String writeProxy() default "";

    /**
     * 读代理：自定义读方法
     * 1.必须是静态方法
     * 2.两个参数，第一个参数为{@link BinaryReader}或{@link DocumentReader}，第二个参数为所属的对象实例
     * 3.对于有特殊构造过程的字段是很有帮助的，也可以进行类型转换。
     * <p>
     * 示例：
     * <pre>
     * {@code
     *      public static void readName(BinaryReader reader, MyClass instance) {
     *          instance.name = reader.readString();
     *      }
     * }
     * </pre>
     */
    String readProxy() default "";

}