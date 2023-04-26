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

package cn.wjybxx.common.dson.document;

import cn.wjybxx.common.dson.DocClassId;

import javax.annotation.Nonnull;

/**
 * 对类型进行映射，将类型映射为唯一字符串。
 * 在文档型编解码中，可读性是比较重要的，因此类型信息使用字符串，而不是数字。
 * <p>
 * 1.name和类型之间应当是唯一映射的，字符串别名应尽量保持简短
 * 2.提供Mapper主要是方便通过算法映射
 *
 * @author wjybxx
 * date 2023/4/3
 */
public interface TypeNameMapper {

    @Nonnull
    DocClassId map(Class<?> type);

}