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

package cn.wjybxx.common.dson;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * 用于为类的所有实例字段生成类型信息 -- {@link TypeArgInfo}
 * 生成的辅助类为{@code XXXTypeArgs}
 * <p>
 * 1.只处理类的实例字段，所有实例字段都被导出。
 * 2.字段关联的FullNumber也会导出，会导出为辅助内的静态内部类 - Numbers
 * 3.由于{@link AutoFields}还有更通用的用途，因此不将Name生成在该类内部。
 *
 * @author wjybxx
 * date 2023/4/3
 */
@Retention(RetentionPolicy.SOURCE)
@Target(ElementType.TYPE)
public @interface AutoTypeArgs {

}