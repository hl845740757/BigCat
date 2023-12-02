package cn.wjybxx.common.btree.fsm;

import cn.wjybxx.common.btree.LeafTask;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/12/1
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class ChangeStateTask<E> extends LeafTask<E> {

    /** 下一个状态的guid -- 延迟加载 */
    private String nextStateGuid;
    /** 下一个状态的引用 */
    private Task<E> nextState;
    /** 目标状态的属性 */
    private Object stateProps;

    /** 目标状态机的名字，以允许切换更顶层的状态机 */
    private String machineName;
    /** 延迟模式 */
    private int delayMode;

    @Override
    protected void execute() {
        if (nextState == null) {
            nextState = getTaskEntry().getTreeLoader().loadRootTask(nextStateGuid);
        }
        if (stateProps != null) {
            nextState.setSharedProps(stateProps);
        }

        int rid = getReentryId();
        StateMachineTask.findStateMachine(this, machineName).changeState(nextState, delayMode);
        if (isExited(rid)) { // 正常情况下，该节点通常也是状态机的（非直接）子节点，因此调用changeState后该task任务会被父节点取消
            return;
        }
        setSuccess();
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) throws Exception {

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

    // endregion
}