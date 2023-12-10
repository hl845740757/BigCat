/*
 *  Copyright 2023 wjybxx
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to iBn writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

package cn.wjybxx.common.codec;

import java.lang.annotation.*;

/**
 * 主要用于为引入的外部库中的Bean自动生成Codec。
 * 1.字段的类型就是我们要自动生成Codec的类型，泛型等信息会被忽略
 * 2.仅适用简单的Bean，复杂的Bean还是需要用户自行实现。
 * 2.1 必须包含无参构造函数；
 * 2.2 transient字段将被忽略；
 * 2.3 要序列化的字段必须包含getter/setter；
 *
 * @author wjybxx
 * date - 2023/12/10
 */
@Target(ElementType.FIELD)
@Retention(RetentionPolicy.SOURCE)
public @interface CodecLinker {

    /**
     * 为类型附加的信息
     * 大致语法:
     * <pre>{@code
     *      @CodecLinker(classImpl = @ClassImpl)
     * }</pre>
     */
    ClassImpl classImpl();

    /** 为生成的文件添加的注解 */
    Class<? extends Annotation>[] annotations() default {};

}