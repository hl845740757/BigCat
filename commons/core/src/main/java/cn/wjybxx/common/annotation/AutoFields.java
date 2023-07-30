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

package cn.wjybxx.common.annotation;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * 用于为类的字段名字生成对应的常量，这是个通用的注解功能。
 * <p>
 * 对于枚举类：只有枚举常量才会被导出。
 * 对于普通类，默认只导出实例字段。
 *
 * <h3>约定</h3>
 * 1. 必须使用该注解，才会生成对应的辅助类。
 * 2. 生成的工具类的类名： 类名 + Fields，如：Player生成的工具类为 PlayerFields。
 * 3. 默认为每一个字段生成一个对应的常量。
 * 4. 常量字段名始终与字段名相同，常量的值通常与字段名也相同。
 * <p>
 * Q：这个注解有什么用？
 * A：可避免手写字符串，也为生成的代码提供常量。
 *
 * @author wjybxx
 * date 2023/3/31
 */
@Retention(RetentionPolicy.SOURCE)
@Target(ElementType.TYPE)
public @interface AutoFields {

    /**
     * 跳过静态字段
     */
    boolean skipStatic() default true;

    /**
     * 跳过实例字段
     */
    boolean skipInstance() default false;

}