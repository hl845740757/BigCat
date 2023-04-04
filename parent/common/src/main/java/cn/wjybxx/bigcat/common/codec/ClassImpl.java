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
import cn.wjybxx.bigcat.common.codec.document.DocumentReader;

/**
 * Class的一些实现信息
 * <p>
 * Q：该注解的目的？
 * A：现在底层去除了反射支持，final字段必须由用户自己解析（性能和安全性的双重考虑）。
 * 如果说对象一共100个字段，就一个是final，难道就因为一个字段，用户就要维护所有的字段编解码吗？
 * 真这么设计的话有点变态（太难用），会导致都不使用final字段。
 * 所以这里提供了一个选择，{@link #autoReadNonFinalFields()}，当用户提供了解码构造器和写对象方法的时候，剩余的非final字段仍交由生成的代码处理。
 * <p>
 * 注意：
 * 我们建议用户维护尽可能少的字段，能托管的就托管。简单说就是：只写解码构造器，且只处理final字段。
 * <p>
 * Q：为什么二进制和文档型编解码使用同一个注解？
 * A：我们建议用户如果自定义实现的话，应当尽可能保持两者一致，减少对某类序列化的特别依赖。
 *
 * @author wjybxx
 * date 2023/4/1
 */
public @interface ClassImpl {

    /**
     * 是否在有解码构造器时还自动读取<b>所有非final字段</b>
     * <p>
     * 该值仅在存在非私有的{@link BinaryReader}的单参构造方法或非私有的{@link DocumentReader}单参构造方法时有效，
     * 通过这种方式，用户就可以通过插入少量代码优雅地解决final字段的解析问题。
     * <p>
     * 默认值为{@code true}，是因为这是常态。
     */
    boolean autoReadNonFinalFields() default true;

    /**
     * 是否在有{@code writeObject}方法时还自动写入<b>所有非final字段</b>。
     * <p>
     * 该值仅在存在非私有的{@code writeObject(BinaryWriter)}的序列化方法或非私有的{@code writeObject(DocumentWriter)}的序列化方法时有效。
     * <p>
     * 默认值为{@code true}，是因为我们建议非特殊字段(final和忽略字段)尽量交给生成的代码托管。
     */
    boolean autoWriteNonFinalFields() default true;

    /**
     * 是否在有{@code writeObject}方法时还自动写入<b>所有final字段</b>。
     * 该值仅在存在非私有的{@code writeObject(BinaryWriter)}的序列化方法或非私有的{@code writeObject(DocumentWriter)}的序列化方法时有效。
     * <p>
     * 默认值为{@code false}，是因为我们建议如果用户提供{@code writeObject}方法，则应该在方法中负责所有的final字段和忽略的编码，
     * 和解码构造器保持对应。
     */
    boolean autoWriteFinalFields() default false;

}