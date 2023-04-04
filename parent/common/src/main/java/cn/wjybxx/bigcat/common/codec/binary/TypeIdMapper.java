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

package cn.wjybxx.bigcat.common.codec.binary;

import javax.annotation.Nonnull;

/**
 * 对类型进行映射，将类型映射为唯一数字id。
 * 它的主要作用是减少传输量和编解码效率（字符串传输量大，且hash和equals开销大 -- 每次读入都是一个新的字符串，开销加大）。
 *
 * @author wjybxx
 * date 2023/3/31
 */
public interface TypeIdMapper {

    /**
     * 1.必须保证同一个类在所有机器上的映射结果是相同的
     * 2.命名空间不可以为0 （底层保留空间）
     */
    @Nonnull
    TypeId map(Class<?> type);

}