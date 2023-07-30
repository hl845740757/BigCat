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

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * 用于为类的所有实例字段生成类型信息 -- {@link TypeArgInfo}
 * 生成的辅助类为{@code XXXSchema}
 * <p>
 * Q: 为什么要定义该注解？numbers和names不可以直接生成在对应的Codec吗？
 * A：这可以避免用户依赖生成的Codec，用户可以仅仅使用该注解生成Schema，然后自行实现对应的Codec。
 * <p>
 * Q：为什么要将所有的信息生成到同一个类？
 * A：1.可以减少生成的类数量，现在生成的类数量确实有点多。
 * 2.方便以后提供根据number查询name，根据name查询number的接口。
 *
 * @author wjybxx
 * date 2023/4/3
 */
@Retention(RetentionPolicy.SOURCE)
@Target(ElementType.TYPE)
public @interface AutoSchema {

}