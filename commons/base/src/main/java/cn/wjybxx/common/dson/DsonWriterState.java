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

package cn.wjybxx.common.dson;

import cn.wjybxx.common.annotation.Internal;

/**
 * Object、Header循环： NAME-VALUE
 * 顶层上下文、Array循环： VALUE
 * <p>
 * type总是和value同时写入，因此不需要额外的 TYPE 状态，但name和value可能分开写入，因此需要两个状态。
 *
 * @author wjybxx
 * date - 2023/4/22
 */
@Internal
public enum DsonWriterState {

    /** 底层上下文初始状态 */
    INITIAL,

    /**
     * 等待写入name(fullNumber)
     */
    NAME,
    /**
     * 等待写入Value
     */
    VALUE,

}