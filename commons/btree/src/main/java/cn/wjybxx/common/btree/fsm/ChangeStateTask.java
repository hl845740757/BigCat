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
    /** 下一个状态的对象缓存，通常延迟加载以避免循环引用 */
    private transient Task<E> nextState;
    /** 目标状态的属性 */
    private Object stateProps;

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
        StateMachineTask<E> stateMachine = StateMachineTask.findStateMachine(this, machineName);
        if (isDisableDelayNotify()) { // 该路径基本不会走到，这里只是给个示例
            int reentryId = getReentryId();
            stateMachine.changeState(nextState, ChangeStateArgs.PLAIN.withDelayMode(delayMode));
            if (!isExited(reentryId)) { // 当前任务如果未被取消则更新为成功
                setSuccess();
            }
        } else {
            // 先设置成功，然后再切换状态，当前Task可保持为成功状态；
            // 记得先把nextState保存下来，因为会先执行exit；最好只在未禁用延迟通知的情况下采用
            setSuccess();
            stateMachine.changeState(nextState, ChangeStateArgs.PLAIN.withDelayMode(delayMode));
        }
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