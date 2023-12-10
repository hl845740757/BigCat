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
package cn.wjybxx.common.btree.fsm;

import cn.wjybxx.common.btree.Task;

/**
 * @author wjybxx
 * date - 2023/12/3
 */
@FunctionalInterface
public interface StateMachineListener<E> {

    /**
     * 1.两个参数最多一个为null
     * 2.可以设置新状态的黑板和其它数据
     * 3.用户此时可为新状态分配上下文；同时清理前一个状态的上下文
     * 4.用户此时可拿到新状态{@link ChangeStateArgs}，后续则不可
     * 5.如果task需要感知redo和undo，则由用户将信息写入黑板
     *
     * @param stateMachineTask 状态机
     * @param curState         当前状态
     * @param nextState        下一个状态
     */
    void beforeChangeState(StateMachineTask<E> stateMachineTask, Task<E> curState, Task<E> nextState);

}