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