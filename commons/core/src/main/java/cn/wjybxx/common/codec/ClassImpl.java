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

import cn.wjybxx.dson.text.ObjectStyle;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * Class的一些实现信息
 * <p>
 * Q：为什么二进制和文档型编解码使用同一个注解？
 * A：我们建议用户如果自定义实现的话，应当尽可能保持两者一致，减少对某类序列化的特别依赖。
 * <p>
 * Q：为什么修改了保留策略为{@link RetentionPolicy#RUNTIME}？
 * A：在最初的版本中，ClassName默认持有在Codec中，但在后来的思考中发现这种方式存在一些问题。
 * 于是将ClassName完全交由Registry管理，Codec不再关注ClassName；
 * 这种情况下用户需要收集到所有的类型信息并交给Converter，因此需要运行时可获得。
 *
 * @author wjybxx
 * date 2023/4/1
 */
@Target(ElementType.TYPE)
@Retention(RetentionPolicy.RUNTIME)
public @interface ClassImpl {

    /**
     * 序列化时的字符串id
     * Q：为什么是个数组？
     * A：这允许定义别名，以支持简写 -- 比如：'@Vector3' 可以简写为 '@V3'；而数字id通常不需要该支持。
     */
    String[] className() default {};

    /** 序列化时的数字id */
    long classId() default 0;

    /**
     * 单例对象获取实例的静态方法
     * 1.如果该属性不为空，则表示对象是单例；序列化时不写入字段，反序列化时直接返回单例。
     * 2.用户可以通过实现Codec实现单例和特殊多例的解析，这里只是对常见情况提供快捷方式。
     */
    String singleton() default "";

    /** 序列化时的缩进格式 */
    ObjectStyle style() default ObjectStyle.INDENT;

    /**
     * 声明不需要自动序列化的字段（自身或超类的）
     * 注意：被跳过的字段仍然会占用字段编号和name。
     * <p>
     * 该属性主要用户处理继承得来的不能直接序列化的字段，以避免编译时检查不通过（无法自动序列化）。
     * 跳过这些字段后，你可以在解析构造方法、readObject、writeObject方法中处理。
     */
    String[] skipFields() default {};

}