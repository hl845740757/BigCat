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

package cn.wjybxx.bigcat.common.codec.document;

import javax.annotation.Nonnull;

/**
 * 对类型进行映射，将类型映射为唯一字符串。
 * 在文档型编解码中，可读性是比较重要的，因此类型信息使用字符串，而不是数字。
 * 不过要避免太长的类型名
 *
 * @author wjybxx
 * date 2023/4/3
 */
public interface TypeNameMapper {

    @Nonnull
    String map(Class<?> type);

}