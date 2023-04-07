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

import cn.wjybxx.bigcat.common.codec.binary.BinaryReader;
import cn.wjybxx.bigcat.common.codec.binary.BinaryWriter;
import cn.wjybxx.bigcat.common.codec.document.DocumentReader;
import cn.wjybxx.bigcat.common.codec.document.DocumentWriter;

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
 * 2.可以实现字段的读写替换，修改writeStartObject/readStartObject的两个参数实现 -- 将字段替换为另一个对象写入。
 * 3.可以实现字段的延迟解析，通过{@link BinaryReader#remainObjectBytes()}实现 -- 目前仅二进制编解码接口提供支持，
 * 4.字段读后的转换，如果字段的默认解码类型不符合要求，可以在读写代理中处理。
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
     * 1.必须是静态方法
     * 2.两个参数，第一个参数为{@link BinaryWriter}或{@link DocumentWriter}，第二个参数为所属的对象实例
     * <p>
     * 示例：
     * <pre>{@code
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
     * <pre>{@code
     *      public static void readName(BinaryReader reader, MyClass instance) {
     *          instance.name = reader.readString();
     *      }
     * }
     * </pre>
     */
    String readProxy() default "";

}