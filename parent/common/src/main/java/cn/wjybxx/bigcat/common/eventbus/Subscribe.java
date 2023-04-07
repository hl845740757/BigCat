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
 * 4. 如果期望订阅多个事件，请查看{@link #childEvents()}和{@link #childKeys()}。
 *
 * <h3>字段优先级</h3>
 * 1.当方法参数为{@link GenericEvent}的实现类时：<br/>
 * 1.1 如果声明了{@link #childEvents()}（不为空），则表示订阅{@link GenericEvent}的子事件，即订阅的key为：【方法参数.class + child1.class】【方法参数.class + child2.class】....<br/>
 * --- childEvent必须可赋值给泛型参数，否则编译错误。<br/>
 * 1.2 如果未声明{@link #childEvents()}，且泛型参数不是通配符{@code ?}，则表示定时特定的子事件，即订阅的key为：【方法参数.class + 泛型参数.class】<br/>
 * 1.3 如果未声明{@link #childEvents()}，且泛型参数是通配符，表示订阅该类型的所有事件 -- 等于一个普通的订阅<br/>
 * 1.4 {@link #childKeys()}无效。<br/>
 * <p>
 * 2.当方法参数是其它类型时：<br/>
 * 2.1 如果声明了{@link #childEvents()}（不为空），表示订阅多个主事件类型，即订阅的key为：【child1.class】【child2.class】....<br/>
 * --- childEvent必须是方法参数的子类型，否则编译错误。<br/>
 * 2.2 如果未声明{@link #childEvents()}，则方法参数表示主事件类型，{@link #childKeys()}有效。<br/>
 * 2.3 如果声明了{@link #childKeys()}（不为空），则表示订阅方法参数主事件下的特定子事件；否则只订阅主事件。
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
     * 作用：
     * 1.监听一大类{@link GenericEvent}事件中的特定几个子事件。
     * 2.监听多个大类型事件。
     */
    Class<?>[] childEvents() default {};

    /**
     * 作用：
     * 1.在声明了该属性时（不为空），{@code childKeys}表示为该类型里的静态常量字段，表示事件的子键为{@code childDeclared.xxx}。
     * 2.在没有声明该属性时，{@code childKeys}表示使用普通字符串作为子键。
     * <h3>建议：</h3>
     * 1.当用于枚举和常量类时，可以使用{@link AutoFields}生成对应的常量字符串。
     * 2.建议都声明该属性，可以保持很好的可读性和扩展性 -- 通过访问类常量的方式支持任意类型的子键（int,string,enum...）。
     */
    Class<?> childDeclared() default Object.class;

    /**
     * 如果声明了该属性（不为空），则表示订阅主事件下特定子事件；否则只订阅主事件。
     * <p>
     * 1.常量字段的名字
     * 2.普通字符串
     */
    String[] childKeys() default {};

    /**
     * 自定义扩展数据，通常是json或cmd格式，与搭配的EventBus相关。
     * <p>
     * Q: 它的作用？
     * A: 告诉特定的事件处理器以实现一些特定的切面功能，比如：限定协议的频率。
     */
    String customData() default "";

}