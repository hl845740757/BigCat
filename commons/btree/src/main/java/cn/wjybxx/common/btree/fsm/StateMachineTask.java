package cn.wjybxx.common.btree.fsm;

import cn.wjybxx.common.ObjectUtils;
import cn.wjybxx.common.btree.Decorator;
import cn.wjybxx.common.btree.Status;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;
import cn.wjybxx.common.collect.Dequeue;
import cn.wjybxx.common.collect.EmptyDequeue;
import cn.wjybxx.common.collect.SlidingDequeue;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Objects;

/**
 * 状态机节点
 * 1. redo和undo是很有用的特性，因此我们在顶层给予支持，但默认的队列不会保存状态
 *
 * @author wjybxx
 * date - 2023/12/1
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class StateMachineTask<E> extends Decorator<E> {

    /** 不延迟 */
    public static final int DELAY_NONE = 0;
    /** 仅在当前子节点完成的时候切换 -- 其它延迟模式也会触发；通常用于状态主动退出时； */
    public static final int DELAY_CURRENT_COMPLETED = 1;
    /** 下一帧执行 */
    public static final int DELAY_NEXT_FRAME = 2;

    /** 状态机名字 */
    private String name;
    /** 无可用状态时状态码 */
    private int noneChildStatus = Status.RUNNING;
    /** 初始状态 */
    private Task<E> initState;
    /** 初始状态的属性 */
    private Object initStateProps;

    private transient Task<E> tempNextState;
    private transient Dequeue<Task<E>> undoQueue = EmptyDequeue.getInstance();
    private transient Dequeue<Task<E>> redoQueue = EmptyDequeue.getInstance();

    private transient Listener<E> listener;
    private transient StateMachineHandler<E> stateMachineHandler;

    // region

    /** 获取当前状态 */
    public final Task<E> getCurState() {
        return child;
    }

    /** 获取临时的下一个状态 */
    public final Task<E> getTempNextState() {
        return tempNextState;
    }

    /** 丢弃未切换的临时状态 */
    public final Task<E> discardTempNextState() {
        Task<E> r = tempNextState;
        if (r != null) tempNextState = null;
        return r;
    }

    /** 对当前当前状态发出取消命令 */
    public final void cancelCurState(int cancelCode) {
        if (child != null && child.isRunning()) {
            child.getCancelToken().cancel(cancelCode);
        }
    }

    /** 查看undo对应的state */
    public final Task<E> peekUndoState() {
        return undoQueue.peekLast();
    }

    /** 查看redo对应的state */
    public final Task<E> peekRedoState() {
        return redoQueue.peekFirst();
    }

    /** 开放以允许填充 */
    public final Dequeue<Task<E>> getUndoQueue() {
        return undoQueue;
    }

    /** 开放以允许填充 */
    public final Dequeue<Task<E>> getRedoQueue() {
        return redoQueue;
    }

    /**
     * @param maxSize 最大大小；0表示禁用；大于0启用
     * @return 最新的queue
     */
    public final Dequeue<Task<E>> setUndoQueueSize(int maxSize) {
        if (maxSize < 0) throw new IllegalArgumentException("maxSize: " + maxSize);
        return undoQueue = setQueueMaxSize(undoQueue, maxSize, true);
    }

    /**
     * @param maxSize 最大大小；0表示禁用；大于0启用
     * @return 最新的queue
     */
    public final Dequeue<Task<E>> setRedoQueueSize(int maxSize) {
        if (maxSize < 0) throw new IllegalArgumentException("maxSize: " + maxSize);
        return redoQueue = setQueueMaxSize(redoQueue, maxSize, false);
    }

    private static <E> Dequeue<E> setQueueMaxSize(Dequeue<E> queue, int maxSize, boolean discardHead) {
        if (maxSize == 0) {
            queue.clear();
            return EmptyDequeue.getInstance();
        }
        if (queue == EmptyDequeue.INSTANCE) {
            return new SlidingDequeue<>(maxSize);
        } else {
            SlidingDequeue<E> slidingDequeue = (SlidingDequeue<E>) queue;
            slidingDequeue.setMaxSize(maxSize, discardHead);
            return queue;
        }
    }

    /**
     * 撤销到前一个状态
     *
     * @return 如果有前一个状态则返回true
     */
    public final boolean undoChangeState() {
        return undoChangeState(DELAY_NONE);
    }

    /**
     * 撤销到前一个状态
     *
     * @param delayMode 延迟模式
     * @return 如果有前一个状态则返回true
     */
    public final boolean undoChangeState(int delayMode) {
        Task<E> prevState = undoQueue.peekLast(); // 真正切换以后再删除
        if (prevState == null) {
            return false;
        }
        changeState(prevState, newControlData(ControlData.CMD_UNDO, delayMode));
        return true;
    }

    /**
     * 重新进入到下一个状态
     *
     * @return 如果有下一个状态则返回true
     */
    public final boolean redoChangeState() {
        return redoChangeState(DELAY_NONE);
    }

    /**
     * 重新进入到下一个状态
     *
     * @param delayMode 延迟模式
     * @return 如果有下一个状态则返回true
     */
    public final boolean redoChangeState(int delayMode) {
        Task<E> nextState = redoQueue.peekFirst();  // 真正切换以后再删除
        if (nextState == null) {
            return false;
        }
        changeState(nextState, newControlData(ControlData.CMD_REDO, delayMode));
        return true;
    }

    /** 切换状态 -- 如果状态机处于运行中，则立即切换 */
    public final void changeState(Task<E> nextState) {
        changeState(nextState, ControlData.NONE);
    }

    /***
     * 切换状态
     * 1.如果当前有一个待切换的状态，则会被悄悄丢弃(可以增加一个通知)
     * 2.无论何种模式，在当前状态进入完成状态时一定会触发
     * 3.如果状态机未运行，则仅仅保存在那里，等待下次运行的时候执行
     * 4.当前状态可先正常完成，然后再切换状态，就可以避免进入被取消状态；可参考{@link ChangeStateTask}
     * <pre>{@code
     *      Task<E> nextState = nextState();
     *      setSuccess();
     *      stateMachine.changeState(nextStata)
     * }
     * </pre>
     *
     * @param nextState 要进入的下一个状态
     * @param delayMode 延迟模式
     */
    public final void changeState(Task<E> nextState, int delayMode) {
        changeState(nextState, newControlData(ControlData.CMD_NONE, delayMode));
    }

    protected void changeState(Task<E> nextState, ControlData controlData) {
        if (nextState == null) {
            throw new NullPointerException("nextState cant be null");
        }
        int delayMode = controlData.delayMode;
        checkDelayMode(delayMode);

        nextState.setControlData(controlData);
        tempNextState = nextState;
        if (!isRunning()) { // 需要保留命令
            nextState.setControlData(controlData.withDelayMode(DELAY_NONE));
            return;
        }
        if (delayMode == DELAY_NONE) {
            if (isExecuting()) {
                execute();
            } else {
                template_execute();
            }
        }
    }

    protected final void checkDelayMode(int delayMode) {
        if (delayMode < DELAY_NONE || delayMode > DELAY_NEXT_FRAME) {
            throw new IllegalArgumentException("invalid delayMode: " + delayMode);
        }
    }

    protected final ControlData newControlData(int cmd, int delayMode) {
        if (delayMode == DELAY_NEXT_FRAME) {
            if (getTaskEntry() == null) { // 尚未运行过
                return new ControlData(cmd, delayMode, 0);
            } else {
                return new ControlData(cmd, delayMode, getCurFrame() + 1);
            }
        } else {
            return new ControlData(cmd, delayMode, 0);
        }
    }
    // endregion

    @Override
    public void resetForRestart() {
        super.resetForRestart();
        if (initState != null) {
            initState.resetForRestart();
        }
        if (child != null) {
            removeChild(0);
        }
        tempNextState = null;
        undoQueue.clear(); // 保留用户的设置
        redoQueue.clear();
    }

    @Override
    protected void beforeEnter() {
        super.beforeEnter();
        noneChildStatus = Math.max(Status.RUNNING, noneChildStatus); // 兼容编辑器忘记赋值
        if (initState != null && initStateProps != null) {
            initState.setSharedProps(initStateProps);
        }

        if (tempNextState == null && initState != null) { // 允许运行前调用changeState
            tempNextState = initState;
        }
        if (child != null) {
            logger.warn("The child of StateMachine is not null");
            removeChild(0);
        }
    }

    @Override
    protected void exit() {
        if (child != null) {
            removeChild(0);
        }
        tempNextState = null;
        undoQueue.clear();
        redoQueue.clear();
        super.exit();
    }

    @Override
    protected void execute() {
        Task<E> curState = this.child;
        Task<E> nextState = this.tempNextState;
        if (nextState != null && isReady(curState, nextState)) {
            this.tempNextState = null;
            if (!template_checkGuard(nextState.getGuard())) { // 下个状态无效
                nextState.setGuardFailed(null);
                if (stateMachineHandler != null) { // 通知特殊情况
                    stateMachineHandler.onNextStateGuardFailed(this, nextState);
                }
            } else {
                if (curState != null) {
                    curState.stop();
                }
                ControlData controlData = (ControlData) Objects.requireNonNullElse(nextState.getControlData(), ControlData.NONE);
                switch (controlData.cmd) {
                    case ControlData.CMD_UNDO -> {
                        undoQueue.pollLast();
                        if (curState != null) {
                            redoQueue.offerFirst(curState);
                        }
                    }
                    case ControlData.CMD_REDO -> {
                        redoQueue.pollFirst();
                        if (curState != null) {
                            undoQueue.offerLast(curState);
                        }
                    }
                    default -> {
                        // 进入新状态，需要清理redo队列
                        redoQueue.clear();
                        if (curState != null) {
                            undoQueue.offerLast(curState);
                        }
                    }
                }
                notifyChangeState(curState, nextState);

                curState = nextState;
                curState.setCancelToken(cancelToken.newChild()); // state可独立取消
                curState.setControlData(null);
                if (child != null) {
                    setChild(0, curState);
                } else {
                    addChild(curState);
                }
            }
        }
        if (curState == null) { // 当前无可用状态
            onNoChildRunning();
            return;
        }
        template_runChildDirectly(curState); // 继续运行或新状态enter
    }

    protected final void onNoChildRunning() {
        if (noneChildStatus == Status.RUNNING) {
            setRunning();
        } else {
            setCompleted(noneChildStatus, false);
        }
    }

    protected final boolean isReady(@Nullable Task<E> curState, Task<?> nextState) {
        if (curState == null) {
            return true;
        }
        ControlData controlData = (ControlData) nextState.getControlData();
        if (controlData == null) {// 可能是未初始化的
            return true;
        }
        if (controlData.delayMode == DELAY_CURRENT_COMPLETED) {
            return false;
        }
        if (controlData.delayMode == DELAY_NEXT_FRAME) {
            return getCurFrame() >= controlData.frame;
        }
        return true;
    }

    protected final void notifyChangeState(@Nullable Task<E> curState, @Nullable Task<E> nextState) {
        assert curState != null || nextState != null;
        if (listener != null) listener.beforeChangeState(this, curState, nextState);
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        assert this.child == child;
        if (tempNextState == null) {
            if (stateMachineHandler != null && stateMachineHandler.onNextStateAbsent(this, child)) {
                return;
            }
            undoQueue.offerLast(child);
            removeChild(0);
            notifyChangeState(child, null);
            onNoChildRunning();
        } else {
            ControlData controlData = (ControlData) tempNextState.getControlData();
            if (controlData != null) { // 需要保留命令
                tempNextState.setControlData(controlData.withDelayMode(DELAY_NONE));
            }
            if (isExecuting()) {
                execute();
            } else {
                template_execute();
            }
        }
    }

    // region

    /** 查找task最近的状态机节点 -- 仅递归查询父节点 */
    public static <E> StateMachineTask<E> findStateMachine(Task<E> task) {
        Task<E> control;
        while ((control = task.getControl()) != null) {
            if (control instanceof StateMachineTask<E> stateMachineTask) {
                return stateMachineTask;
            }
            task = control;
        }
        throw new IllegalStateException("cant find stateMachine from controls");
    }

    /**
     * 查找task最近的状态机节点
     * 1.名字不为空的情况下，支持从兄弟节点中查询
     * 2.有限测试父节点，然后测试兄弟节点
     */
    @Nonnull
    public static <E> StateMachineTask<E> findStateMachine(Task<E> task, String name) {
        if (ObjectUtils.isBlank(name)) {
            return findStateMachine(task);
        }
        Task<E> control;
        StateMachineTask<E> stateMachine;
        while ((control = task.getControl()) != null) {
            // 父节点
            if ((stateMachine = castAsStateMachine(control, name)) != null) {
                return stateMachine;
            }
            // 兄弟节点（要排除自己）
            for (int i = 0, n = control.getChildCount(); i < n; i++) {
                final Task<E> brother = control.getChild(i);
                if (task == brother) continue;
                if ((stateMachine = castAsStateMachine(brother, name)) != null) {
                    return stateMachine;
                }
            }
            task = control;
        }
        throw new IllegalStateException("cant find stateMachine from controls and brothers");
    }

    private static <E> StateMachineTask<E> castAsStateMachine(Task<E> task, String name) {
        if (task instanceof StateMachineTask<E> stateMachineTask
                && Objects.equals(name, stateMachineTask.getName())) {
            return stateMachineTask;
        }
        return null;
    }

    @FunctionalInterface
    public interface Listener<E> {

        /**
         * 1.两个参数最多一个为null
         * 2.可以设置新状态的黑板和其它数据
         *
         * @param stateMachineTask 状态机
         * @param curState         当前状态
         * @param nextState        下一个状态
         */
        void beforeChangeState(StateMachineTask<E> stateMachineTask, Task<E> curState, Task<E> nextState);

    }

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

    // endregion

    //
    public Task<E> getInitState() {
        return initState;
    }

    public void setInitState(Task<E> initState) {
        this.initState = initState;
    }

    public Object getInitStateProps() {
        return initStateProps;
    }

    public void setInitStateProps(Object initStateProps) {
        this.initStateProps = initStateProps;
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public int getNoneChildStatus() {
        return noneChildStatus;
    }

    public void setNoneChildStatus(int noneChildStatus) {
        this.noneChildStatus = noneChildStatus;
    }

    public Listener<E> getListener() {
        return listener;
    }

    public void setListener(Listener<E> listener) {
        this.listener = listener;
    }

    public StateMachineHandler<E> getStateMachineHandler() {
        return stateMachineHandler;
    }

    public void setStateMachineHandler(StateMachineHandler<E> stateMachineHandler) {
        this.stateMachineHandler = stateMachineHandler;
    }

}
