package cn.wjybxx.common.btree.fsm;

import cn.wjybxx.common.btree.Task;

/**
 * @author wjybxx
 * date - 2023/12/3
 */
public interface StateMachineHandler<E> {

    /**
     * 下个状态的前置条件检查失败
     *
     * @param stateMachineTask 状态机
     * @param nextState        下一个状态
     */
    default void onNextStateGuardFailed(StateMachineTask<E> stateMachineTask, Task<E> nextState) {

    }

    /**
     * 当状态机没有下一个状态时调用该方法，以避免无可用状态
     * 注意：
     * 1.状态机启动时不会调用该方法
     * 2.如果该方法返回后仍无可用状态，将触发无状态逻辑
     * 3.【不可延迟新状态】，否则将导致错误；框架难以安全检测，由用户自身保证
     *
     * @param stateMachineTask 状态机
     * @param preState         前一个状态
     * @return 用户是否执行了状态切换操作
     */
    boolean onNextStateAbsent(StateMachineTask<E> stateMachineTask, Task<E> preState);

}