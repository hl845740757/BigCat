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

import cn.wjybxx.common.EnumLite;
import cn.wjybxx.common.EnumUtils;
import cn.wjybxx.common.codec.ClassImpl;
import cn.wjybxx.common.codec.FieldImpl;

import java.lang.annotation.*;

/**
 * 用于标注一个类的对象可序列化为二进制结构
 *
 * <h3>注解处理器</h3>
 * 对于带有该注解的类：
 * 1. 如果是枚举，必须实现{@link EnumLite}并提供静态非private的{@code forNumber(int)}方法 - {@link EnumUtils#mapping(EnumLite[])}。
 * 2. 如果是普通类，必须提供<b>非私有无参构造方法</b>，或提供非私有的{@link BinaryObjectReader}的单参构造方法。
 * 3. 对于普通类，所有托管给生成代码读的字段，必须提供setter或直接写权限。
 * 4. 对于普通类，所有托管给生成代码写的字段，必须提供getter或直接读权限。
 * <p>
 * 普通类钩子方法：
 * 1. 如果类提供了非私有的{@link BinaryObjectReader}的单参构造方法，将自动调用 -- 该方法可用于final和忽略字段。
 * 2. 如果类提供了非私有的{@code readObject(BinaryObjectReader)}的反序列化方法，将自动调用 -- 该方法可用于忽略字段。
 * 3. 如果类提供了非私有的{@code writeObject(BinaryObjectWriter)}的序列化方法，将自动调用 -- 该方法可用于final和忽略字段。
 * 4. 如果类提供了非私有的{@code afterDecode}方法，将在反序列化后调用 -- 该方法用于解码后构建缓存字段。
 * 5. 如果字段通过{@link FieldImpl#readProxy()}指定了读代理，则不要求setter权限
 * 6. 如果字段通过{@link FieldImpl#writeProxy()}指定了写代理，则不要求getter权限
 *
 * <h3>序列化的字段</h3>
 * 1. 默认所有字段都序列化。但如果有{@code transient}修饰或使用{@link BinaryIgnore}进行注解，则不序列化。
 * 2. {@link BinaryIgnore}也可以用于将{@code transient}字段加入编解码。
 * 3. 如果你提供了解码构造器和写对象方法，你可以在其中写入忽略字段。
 *
 * <h3>多态字段</h3>
 * 1. 如果对象的运行时类型存在于{@code CodecRegistry}中，则总是可以精确解析，因此不需要特殊处理。
 * 2. 否则用户需要指定实现类或读代理实现精确解析，请查看{@link FieldImpl}注解。
 *
 * <h3>final字段</h3>
 * 详见：{@link ClassImpl}
 *
 * <h3>读写忽略字段</h3>
 * 用户可以通过构造解码器和写对象方法实现。
 *
 * <h3>扩展</h3>
 * Q: 是否可以不使用注解，也能序列化？
 * A: 如果不使用注解，需要手动实现{@link BinaryPojoCodecImpl}，并将其添加到注册表中。
 * （也可以加入到Scanner的扫描路径）
 *
 * <h3>一些建议</h3>
 * 1.一般而言，建议使用注解{@link BinarySerializable}，并遵循相关规范，由注解处理器生成的类负责解析，而不是实现{@link BinaryPojoCodecImpl}。
 * 2.并不建议都实现为javabean格式，final字段可以通过解析构造方法处理。
 * 3.如果手写编解码实现，读写顺序必须保持一致，否则抛出异常。
 *
 * <h3>辅助类类名</h3>
 * 生成的辅助类为{@code XXXCodec}
 *
 * @author wjybxx
 * date 2023/3/31
 */
@Retention(RetentionPolicy.SOURCE)
@Target(ElementType.TYPE)
public @interface BinarySerializable {

    /**
     * 为生成的文件添加的注解
     * 比如：可以添加{@link BinaryPojoCodecScanIgnore}以使得生成的代码在扫描Codec时被忽略。
     */
    Class<? extends Annotation>[] annotations() default {};

}