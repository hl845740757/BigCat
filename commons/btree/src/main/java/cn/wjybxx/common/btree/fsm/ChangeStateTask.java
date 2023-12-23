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

import cn.wjybxx.common.btree.LeafTask;
import cn.wjybxx.common.btree.Status;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/12/1
 */
@BinarySerializable
@DocumentSerializable
public class ChangeStateTask<E> extends LeafTask<E> {

    /** 下一个状态的guid -- 延迟加载 */
    private String nextStateGuid;
    /** 下一个状态的对象缓存，通常延迟加载以避免循环引用 */
    private transient Task<E> nextState;
    /** 目标状态的属性 */
    private Object stateProps;
    /** 为当前状态设置结果 -- 用于避免当前状态进入被取消状态；使用该特性时避免curState为自身 */
    private int curStateResult;

    /** 目标状态机的名字，以允许切换更顶层的状态机 */
    private String machineName;
    /** 延迟模式 */
    private int delayMode;

    public ChangeStateTask() {
    }

    public ChangeStateTask(Task<E> nextState) {
        this.nextState = nextState;
    }

    @Override
    protected void execute() {
        if (nextState == null) {
            nextState = getTaskEntry().getTreeLoader().loadRootTask(nextStateGuid);
        }
        if (stateProps != null) {
            nextState.setSharedProps(stateProps);
        }
        final StateMachineTask<E> stateMachine = StateMachineTask.findStateMachine(this, machineName);
        final Task<E> curState = stateMachine.getCurState();
        // 在切换状态前将当前状态标记为成功或失败；只有延迟通知的情况下才可以设置状态的结果，否则状态机会切换到其它状态
        if (Status.isCompleted(curStateResult) && curState != null && !curState.isDisableDelayNotify()) {
            curState.setCompleted(curStateResult, false);
        }

        if (!isDisableDelayNotify()) {
            // 先设置成功，然后再切换状态，当前Task可保持为成功状态；
            // 记得先把nextState保存下来，因为会先执行exit；最好只在未禁用延迟通知的情况下采用
            setSuccess();
            stateMachine.changeState(nextState, ChangeStateArgs.PLAIN.withDelayMode(delayMode));
        } else {
            // 该路径基本不会走到，这里只是给个示例
            int reentryId = getReentryId();
            stateMachine.changeState(nextState, ChangeStateArgs.PLAIN.withDelayMode(delayMode));
            if (!isExited(reentryId)) {
                setSuccess();
            }
        }
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) {

    }

    // region

    public String getNextStateGuid() {
        return nextStateGuid;
    }

    public void setNextStateGuid(String nextStateGuid) {
        this.nextStateGuid = nextStateGuid;
    }

    public Task<E> getNextState() {
        return nextState;
    }

    public void setNextState(Task<E> nextState) {
        this.nextState = nextState;
    }

    public Object getStateProps() {
        return stateProps;
    }

    public void setStateProps(Object stateProps) {
        this.stateProps = stateProps;
    }

    public String getMachineName() {
        return machineName;
    }

    public void setMachineName(String machineName) {
        this.machineName = machineName;
    }

    public int getDelayMode() {
        return delayMode;
    }

    public void setDelayMode(int delayMode) {
        this.delayMode = delayMode;
    }

    public int getCurStateResult() {
        return curStateResult;
    }

    public void setCurStateResult(int curStateResult) {
        this.curStateResult = curStateResult;
    }

    // endregion
}