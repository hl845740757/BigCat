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

package cn.wjybxx.bigcat.common.eventbus;

import cn.wjybxx.bigcat.common.codec.document.AutoFields;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * 该注解用于订阅一个事件，注解处理器会为其生成注册到{@link EventHandlerRegistry}的辅助代码。
 * 生成的代理类为 XXXBusRegister。
 * <h3>约定</h3>
 * <br>
 * 1. 使用该注解的方法必须有且仅有一个参数，返回值无要求。<br>
 * 2. 方法参数不可以是基本类型，因为发布事件的时候会封装为Object，基本类型会被装箱，因此无法接收到基本类型的事件。<br>
 * 3. 方法不能是private - 至少是包级访问权限。<br>
 * 4. 如果期望订阅多个事件，请查看{@link #masterEvents()}{@link #childEvents()}和{@link #childKeys()}{@link #intChildKeys()}。
 * <p>
 * Q: 如果使用多个EventBus，如果避免订阅方法注册到不该注册的地方？
 * A: 有3种选择，
 * 1. 手动注册，自行绑定。
 * 2. 在EventBus中添加拦截器，不接收特定类型的事件的监听器。
 * 3. 使用多个类进行订阅（内部类也可以），内部类A订阅EventBusA的事件，内部类B订阅EventBusB的事件.。。
 * 推荐使用内部类的方式，不过需要对应的方法可访问（通常大于等于包级别即可）
 *
 * @author wjybxx
 * date 2023/4/6
 */
@Target(ElementType.METHOD)
@Retention(RetentionPolicy.SOURCE)
public @interface Subscribe {

    /**
     * 更多的主事件类型
     * 当声明了该属性时（不为空），则其它属性全部无效
     * 该方法用于一个方法订阅多个主事件类型。
     */
    Class<?>[] masterEvents() default {};

    // GenericEvent

    /**
     * 声明需要订阅子事件。
     * 1.如果{@link GenericEvent}的泛型参数为通配符{@code ?}，则该属性无效。
     * 2.其它情况下，如果该属性不为空，则表示只订阅子事件，此时泛型参数应该子事件的超类，否则编译错误。
     * 如果在订阅子事件的情况下，还需订阅泛型参数关联的事件，你可以声明两个订阅方法。
     * <p>
     * Q：有什么用途？
     * A：以协议监听为例，你可以在一个方法中监听多个协议。
     */
    Class<?>[] childEvents() default {};

    // 普通事件类型

    /**
     * 声明子键的类
     * 在声明了该属性时，{@code childKeys}表示为该类型里的静态常量字段(兼容枚举)。
     * 在没有声明该属性时，{@code childKeys}表示普通字符串。
     * 当用于枚举和常量类时，可以使用{@link AutoFields}生成对应的常量字符串。
     *
     * @see #childKeys()
     */
    Class<?> childDeclared() default Object.class;

    /**
     * 字符串或常量（含枚举）名字类型的子键
     */
    String[] childKeys() default {};

    /**
     * 用于支持int类型的子事件key
     */
    int[] intChildKeys() default {};

    // 切面数据

    /**
     * 自定义扩展数据，通常是json或cmd格式，与搭配的EventBus相关。
     * <p>
     * Q: 它的作用？
     * A: 告诉特定的事件处理器以实现一些特定的切面功能，比如：限定协议的频率。
     */
    String customData() default "";

}