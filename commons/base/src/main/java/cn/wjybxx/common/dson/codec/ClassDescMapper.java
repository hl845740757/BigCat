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

package cn.wjybxx.common.dson.codec;

import javax.annotation.Nonnull;

/**
 * 类型id映射函数
 * <p>
 * 1.classId和类型之间应当是唯一映射的，如果是字符串别名，应尽量保持简短
 * 2.在文档型编解码中，可读性是比较重要的，因此
 * 3.提供Mapper主要为方便通过算法映射
 *
 * @author wjybxx
 * date - 2023/4/26
 */
@FunctionalInterface
public interface ClassDescMapper {

    @Nonnull
    ClassDesc map(Class<?> type);

}