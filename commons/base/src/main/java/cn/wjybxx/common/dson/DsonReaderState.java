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
 * Object循环 TYPE-NAME-VALUE
 * Array循环 TYPE-VALUE
 * 顶层上下文循环 VALUE-DONE
 * <p>
 * 由于type必须先读取，因此type需要独立的状态；又由于name可以单独读取，因此name也需要单独的转态，因此需要type-name-value三个状态。
 *
 * @author wjybxx
 * date - 2023/4/22
 */
@Internal
public enum DsonReaderState {

    /** 顶层上下文的初始状态 */
    INITIAL,

    /**
     * 已确定是一个Array或Object，已读取ClassId，等待调用start方法
     * 之所以要支持这个中间状态，是考虑到input频繁peek和回滚的开销较大。
     */
    WAIT_START_OBJECT,
    /**
     * 等待读取类型
     */
    TYPE,
    /**
     * 等待用户读取name(fullNumber)
     */
    NAME,
    /**
     * 等待读取value
     */
    VALUE,
    /**
     * 当前对象读取完毕；状态下等待用户调用writeEnd
     */
    WAIT_END_OBJECT,

    /** 顶层上下文的读取一个对象完毕 */
    DONE,

}