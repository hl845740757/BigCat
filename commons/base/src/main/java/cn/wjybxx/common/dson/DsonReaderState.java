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
 * Object、Header循环 TYPE-NAME-VALUE
 * 顶层上下文、Array循环 TYPE-VALUE
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
     * 已确定是一个Array/Object，等待用户调用readStartXXX方法
     * 正常情况下不会出现该状态，Object/Array由于存在Header，我们通常需要读取header之后才能正确解码，
     * 我们需要能在读取header之后重新恢复到需要调用readStartXXX的方法，才不会影响业务代码，也避免数据流回退
     * <p>
     * 简单说，用于peek一部分数据之后，重新设置为等待readStartXXX状态，避免数据流的回滚
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
     * 当前对象读取完毕；等待用户调用writeEndXXX
     */
    WAIT_END_OBJECT,

    /** 到达输入的尾部 */
    END_OF_FILE,

}