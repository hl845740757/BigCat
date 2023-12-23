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
package cn.wjybxx.common.btree;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Objects;
import java.util.stream.Stream;

/**
 * <h3>取消</h3>
 * 1.取消默认是协作式的，依赖于任务自身检查；如果期望更及时的响应取消信号，则需要注册注册监听器。
 * 2.通常在执行外部代码后都应该检测.
 * 3.一般而言，不管理上下文的节点在子节点取消时都应该取消自己（因为是同一个CancelToken）
 * 4.Task类默认只在心跳方法中检测取消信号，任何的回调和事件方法中都由用户自身检测。
 *
 * <h3>心跳+事件驱动</h3>
 * 1.心跳为主，事件为辅。
 * 2.心跳不是事件！心跳自顶向下驱动，事件则无规律。
 *
 * @param <E> 黑板的类型
 * @author wjybxx
 * date - 2023/11/25
 */
public abstract class Task<E> {

    public static final Logger logger = LoggerFactory.getLogger(Task.class);

    /** 低6位记录前一次的运行结果，范围 [0, 63] */
    private static final int MASK_PREV_STATUS = 63;
    private static final int MASK_ENTER_EXECUTE = 1 << 6;
    private static final int MASK_STOP_EXIT = 1 << 7;
    private static final int MASK_STILLBORN = 1 << 8;
    private static final int MASK_EXECUTING = 1 << 9;
    private static final int MASK_RUNNING_FIRED = 1 << 10;
    private static final int MASK_DISABLE_NOTIFY = 1 << 11;
    private static final int MASK_INHERITED_BLACKBOARD = 1 << 12;
    private static final int MASK_INHERITED_CANCEL_TOKEN = 1 << 13;
    private static final int MASK_INHERITED_PROPS = 1 << 14;

    private static final int MASK_LOCK1 = 1 << 20;
    private static final int MASK_LOCK2 = 1 << 21;
    private static final int MASK_LOCK3 = 1 << 22;
    private static final int MASK_LOCK4 = 1 << 23;
    private static final int MASK_LOCK_ALL = MASK_LOCK1 | MASK_LOCK2 | MASK_LOCK3 | MASK_LOCK4;

    // 高8位为控制流程相关bit，对外开放
    public static final int MASK_DISABLE_ENTER_EXECUTE = 1 << 24;
    public static final int MASK_DISABLE_DELAY_NOTIFY = 1 << 25;
    public static final int MASK_DISABLE_AUTO_CHECK_CANCEL = 1 << 26;
    public static final int MASK_AUTO_LISTEN_CANCEL = 1 << 27;
    public static final int MASK_AUTO_RESET_CHILDREN = 1 << 28;
    public static final int MASK_CONTROL_FLOW_FLAGS = 0xFF00_0000;

    /** 任务树的入口(缓存以避免递归查找) */
    transient TaskEntry<E> taskEntry;
    /** 任务的控制节点，通常是Task的Parent节点 */
    transient Task<E> control;
    /**
     * 任务运行时依赖的黑板（主要上下文）
     * 1.每个任务可有独立的黑板（数据）；
     * 2.运行时不能为null；
     * 3.如果是自动继承的，exit后自动删除；如果是Control赋值的，则由control删除。
     */
    protected transient E blackboard;
    /**
     * 取消令牌（取消上下文）
     * 1.每个任务可有独立的取消信号；
     * 2.运行时不能为null；
     * 3.如果是自动继承的，exit后自动删除；如果是Control赋值的，则由control删除。
     */
    protected transient CancelToken cancelToken;
    /**
     * 共享属性（配置上下文）
     * 1.用于解决【数据和行为分离】架构下的配置需求，主要解决策划的配置问题，减少维护工作量。
     * 2.共享属性应该在运行前赋值，不应该也不能被序列化。
     * 3.共享属性应该是只读的、可共享的，因为它是配置。
     * 4.如果是自动继承的，exit后自动删除；如果是Control赋值的，则由control删除。
     * <p>
     * 举个例子：部分项目的角色技能是有等级的，且数值不是根据等级计算的，而是一级一级配置的，
     * 那么每一级的数值就是props，技能流程就是task。
     */
    protected transient Object sharedProps;
    /**
     * Control为管理子节点存储在子节点上的数据
     * 1.避免额外映射，提高性能和易用性
     * 2.entry的逻辑control是用户，因此也可以存储用户的数据
     * 3.该属性不自动继承，不属于运行上下文。
     */
    transient Object controlData;

    /** 任务的状态 -- {@link Status}，使用int以支持用户返回更详细的错误码 */
    private transient int status;
    /** 任务运行时的控制信息(bits) -- 每次运行时会重置为0 */
    private transient int ctl;
    /** 启动时的帧号 -- 每次运行时重置为0 */
    private transient int enterFrame;
    /** 结束时的帧号 -- 每次运行时重置为0 */
    private transient int exitFrame;
    /** 重入Id，只增不减 -- 用于事件驱动下检测冲突（递归）；reset时不重置，甚至也增加 */
    private transient short reentryId;

    /**
     * 任务绑定的前置条件(precondition太长...)
     * 编程经验告诉我们：前置条件和行为是由控制节点组合的，而不是行为的属性。
     * 但由于Task只能有一个Control，因此将前置条件存储在Task可避免额外的映射，从而可提高查询性能和易用性；
     * 另外，将前置条件存储在Task上，可以让行为树的结构更加清晰。
     */
    private Task<E> guard;
    /**
     * 任务的自定义标识
     * 1.对任务进行标记是一个常见的需求，我们将其定义在顶层以简化使用
     * 2.在运行期间不应该变动
     * 3.高8位为流程控制特征值，会在任务运行前拷贝到ctl -- 以支持在编辑器导出文件中指定。
     */
    protected int flags;

    // region 简单查询

    public final TaskEntry<E> getTaskEntry() {
        return taskEntry;
    }

    public final Task<E> getControl() {
        return control;
    }

    public final E getBlackboard() {
        return blackboard;
    }

    public final void setBlackboard(E blackboard) {
        this.blackboard = blackboard;
    }

    public final CancelToken getCancelToken() {
        return cancelToken;
    }

    public final void setCancelToken(CancelToken cancelToken) {
        this.cancelToken = cancelToken;
    }

    public final Object getControlData() {
        return controlData;
    }

    public final void setControlData(Object controlData) {
        this.controlData = controlData;
    }

    public final Object getSharedProps() {
        return sharedProps;
    }

    public final void setSharedProps(Object sharedProps) {
        this.sharedProps = sharedProps;
    }

    public final boolean isRunning() {
        return status == Status.RUNNING;
    }

    public final boolean isCompleted() {
        return status >= Status.SUCCESS;
    }

    public final boolean isSucceeded() {
        return status == Status.SUCCESS;
    }

    public final boolean isCancelled() {
        return status == Status.CANCELLED;
    }

    public final boolean isFailed() {
        return status > Status.CANCELLED;
    }

    public final boolean isFailedOrCancelled() {
        return status >= Status.CANCELLED;
    }

    /** 获取原始的状态码 */
    public final int getStatus() {
        return status;
    }

    /** 获取归一化后的状态码，所有的失败码都转换为{@link Status#ERROR} */
    public final int getNormalizedStatus() {
        return Math.min(status, Status.ERROR);
    }

    /**
     * 获取任务前一次的执行结果
     * 1.取值范围[0,63] -- 其实只要能区分成功失败就够；
     * 2.这并不是一个运行时必须的属性，而是为Debug和Ui视图用的；Java端暂不实现了，C#会实现
     */
    public final int getPrevStatus() {
        return ctl & MASK_PREV_STATUS;
    }

    public final int getEnterFrame() {
        return enterFrame;
    }

    /** 慎重调用 */
    public void setEnterFrame(int enterFrame) {
        this.enterFrame = enterFrame;
    }

    public final int getExitFrame() {
        return exitFrame;
    }

    /** 慎重调用 */
    public void setExitFrame(int exitFrame) {
        this.exitFrame = exitFrame;
    }

    /** 获取行为树绑定的实体 -- 最好让Entity也在黑板中 */
    public Object getEntity() {
        if (taskEntry == null) {
            throw new IllegalStateException("This task has never run");
        }
        return taskEntry.getEntity();
    }

    /** 获取当前的帧号 */
    public int getCurFrame() {
        if (taskEntry == null) {
            throw new IllegalStateException("This task has never run");
        }
        return taskEntry.getCurFrame();
    }

    /** 获取行为树入口绑定的黑板 */
    public final E getEntryBlackboard() {
        if (taskEntry == null) {
            throw new IllegalStateException("This task has never run");
        }
        return taskEntry.getBlackboard();
    }

    // endregion

    // region core

    /**
     * 该方法用于初始化对象。
     * 1.不命名为init，是因为init通常让人觉得只调用一次。
     * 2.该方法不可以使自身进入完成状态。
     */
    protected void beforeEnter() {

    }

    /**
     * 该方法在Task进入运行状态时执行
     * 1.数据初始化需要放在{@link #beforeEnter()}中，避免执行逻辑时对象未初始化完成。
     * 2.如果要初始化子节点，也放到{@link #beforeEnter()}方法;
     * 3.允许更新自己为完成状态
     *
     * @param reentryId 用于判断父类是否使任务进入了完成状态；虽然也可先捕获再调用超类方法，但传入会方便许多。
     */
    protected void enter(int reentryId) {

    }

    /**
     * Task的心跳方法，在Task进入完成状态之前会反复执行。
     * 1.可以根据{@link #isExecuteTriggeredByEnter()}判断是否是与{@link #enter(int)}连续执行的。
     * 2.运行中可通过{@link #setSuccess()}、{@link #setFailed(int)} ()}、{@link #setCancelled()}将自己更新为完成状态。
     * 3.不建议直接调用该方法，而是通过模板方法运行。
     */
    protected abstract void execute();

    /**
     * 该方法在Task进入完成状态时执行
     * 1.该方法与{@link #enter(int)}对应，通常用于停止{@link #enter(int)}中启动的逻辑。
     * 2.该方法也用于清理部分运行时产生的临时数据。
     * 3.一定记得取消注册的各种监听器。
     */
    protected void exit() {

    }

    /** 设置为运行成功 */
    public final void setSuccess() {
        assert this.status == Status.RUNNING;
        this.status = Status.SUCCESS;
        template_exit(0);
        if (checkImmediateNotifyMask(ctl) && control != null) {
            control.onChildCompleted(this);
        }
    }

    /** 设置为取消 */
    public final void setCancelled() {
        setCompleted(Status.CANCELLED, false);
    }

    /** 设置为执行失败 -- 兼容{@link #setGuardFailed(Task)} */
    public final void setFailed(int status) {
        if (status < Status.ERROR) {
            throw new IllegalArgumentException("status " + status);
        }
        setCompleted(status, false);
    }

    /**
     * 设置为前置条件测试失败
     * 1.该方法仅适用于control测试child的guard失败，令child在未运行的情况下直接失败的情况。
     * 2.对于运行中的child，如果发现child的guard失败，不能继续运行，应当取消子节点的执行（stop）。
     *
     * @param control 由于task未运行，其control可能尚未赋值，因此要传入；传null可不接收通知
     */
    public final void setGuardFailed(Task<E> control) {
        assert status != Status.RUNNING;
        if (control != null) { //测试null，适用entry的guard失败
            setControl(control);
        }
        setCompleted(Status.GUARD_FAILED, false);
    }

    /** 设置为完成 -- 通常用于通过子节点的结果设置自己 */
    public final void setCompleted(int status, boolean fromChild) {
        if (status < Status.SUCCESS) throw new IllegalArgumentException();
        if (fromChild && status == Status.GUARD_FAILED) {
            status = Status.ERROR; // GUARD_FAILED 不能向上传播
        }
        final int prevStatus = this.status;
        if (prevStatus == Status.RUNNING) {
            if (status == Status.GUARD_FAILED) {
                throw new IllegalArgumentException("Running task cant fail with 'GUARD_FAILED'");
            }
            this.status = status;
            template_exit(0);
        } else {
            // 未调用Enter和Exit，需要补偿 -- 保留当前的ctl会更好
            ctl &= ~MASK_PREV_STATUS;
            ctl |= Math.min(MASK_PREV_STATUS, prevStatus);
            ctl |= MASK_STILLBORN;
            this.status = status;
            this.enterFrame = exitFrame;
            this.reentryId++;
        }
        if (checkImmediateNotifyMask(ctl) && control != null) {
            control.onChildCompleted(this);
        }
    }

    /**
     * 子节点还需要继续运行
     * 1.child在运行期间只会通知一次
     * 2.该方法不应该触发状态迁移，即不应该使自己进入完成状态
     */
    protected abstract void onChildRunning(Task<E> child);

    /**
     * 子节点进入完成状态
     * 1.避免方法数太多，实现类测试task的status即可
     * 2.{@link #getNormalizedStatus()}有助于switch测试
     * 3.task可能是取消状态，甚至可能没运行过直接失败（前置条件失败）
     * 4.钩子任务和guard不会调用该方法
     * 5.{@link #isExecuting()}有助于检测冲突，减少调用栈深度
     * 6.同一子节点连续通知的情况下，completed的逻辑应当覆盖{@link #onChildRunning(Task)}的影响。
     * 7.任何的回调和事件方法中都由用户自身检测取消信号
     */
    protected abstract void onChildCompleted(Task<E> child);

    /**
     * Task收到外部事件
     *
     * @see #onEventImpl(Object)
     */
    public final void onEvent(@Nonnull Object event) {
        if (canHandleEvent(event)) {
            onEventImpl(event);
        }
    }

    /**
     * 该方法用于测试自己的状态和事件数据
     * 如果通过条件Task来实现事件过滤，那么通常的写法如下：
     * <pre>
     *     blackboard.set("event", event); // task通过黑板获取事件对象
     *     try {
     *         return template_checkGuard(eventFilter);
     *     } finally {
     *         blackboard.remove("event");
     *     }
     * </pre>
     * ps: 如果想支持编辑器中测试事件属性，event通常需要实现为KV结构。
     */
    public boolean canHandleEvent(@Nonnull Object event) {
        return status == Status.RUNNING;
    }

    /**
     * 对于控制节点，通常将事件派发给约定的子节点或钩子节点。
     * 对于叶子节点，通常自身处理事件。
     * 注意：
     * 1.转发事件时应该调用子节点的{@link #onEvent(Object)}方法
     * 2.在AI这样的领域中，建议将事件转化为信息存储在Task或黑板中，而不是尝试立即做出反应。
     * 3.{@link #isExecuting()}方法很重要
     */
    protected abstract void onEventImpl(@Nonnull Object event);

    /**
     * 取消令牌的回调方法
     * 注意：如果未启动自动监听，手动监听时也建议绑定到该方法
     */
    protected void onCancelRequested(CancelToken cancelToken) {
        if (isRunning()) setCancelled();
    }

    /**
     * 强制停止任务
     * 1.只应该由Control调用，因此不会通知Control
     * 2.未完成的任务默认会进入Cancelled状态
     * 3.不命名为cancel，否则容易误用；我们设计的cancel是协作式的，可通过{@link #cancelToken}发出请求请求。
     */
    public final void stop() {
        // 被显式调用stop的task一定不能通知父节点，只要任务执行过就需要标记
        if (status == Status.RUNNING) {
            status = Status.CANCELLED;
            template_exit(MASK_STOP_EXIT);
        } else if (status != Status.NEW) {
            ctl |= MASK_STOP_EXIT; // 可能是一个先将自己更新为完成状态，又执行了逻辑的子节点；
        }
    }

    /**
     * 停止所有运行中的子节点
     * 1.该方法在自身的exit之前调用
     * 2.如果有特殊的子节点（钩子任务），也需要在这里停止
     */
    protected void stopRunningChildren() {
        // 停止child时默认逆序停止；一般而言都是后面的子节点依赖前面的子节点
        for (int idx = getChildCount() - 1; idx >= 0; idx--) {
            final Task<E> child = getChild(idx);
            if (child.status == Status.RUNNING) {
                child.stop();
            }
        }
    }

    /**
     * 重置任务以便重新启动(清理运行产生的所有临时数据)
     * <p>
     * 1. 和exit一样，清理的是运行时产生的临时数据，而不是所有数据；不过该方法是比exit更彻底的清理。
     * 2. 钩子任务也应当被重置，可重写{@link #resetChildrenForRestart()}或该方法清理。
     * 3. 与{@link #beforeEnter()}相同，重写方法时，应先执行父类逻辑，再重置自身属性。
     * 4. 有临时数据的Task都应该重写该方法，行为树通常是需要反复执行的。
     */
    public void resetForRestart() {
        if (status == Status.NEW) {
            return;
        }
        if (status == Status.RUNNING) {
            stop();
        }
        resetChildrenForRestart();
        if (guard != null) {
            guard.resetForRestart();
        }
        if (this != taskEntry) { // unsetControl
            unsetControl();
        }
        status = 0;
        ctl = 0;
        enterFrame = 0;
        exitFrame = 0;
        reentryId++; // 上下文变动，和之前的执行分开
    }

    /**
     * 重置所有的子节点
     * 1.如果有需要重置的特殊子节点，可以重写该方法以确保无遗漏
     */
    protected void resetChildrenForRestart() {
        // 逆序重置，与stop一致
        for (int idx = getChildCount() - 1; idx >= 0; idx--) {
            Task<E> child = getChild(idx);
            if (child.status != Status.NEW) {
                child.resetForRestart();
            }
        }
    }

    /**
     * 是否正在执行{@link #execute()}方法
     * 该方法非常重要，主要处理心跳和事件方法之间的冲突。
     * 利用得当可大幅降低代码复杂度，减少调用栈深度，提高性能。
     */
    protected final boolean isExecuting() {
        return (ctl & MASK_EXECUTING) != 0;
    }

    /**
     * 检查取消
     *
     * @param rid 重入id；方法保存的局部变量
     * @return 任务是否已进入完成状态；如果返回true，调用者应立即退出
     * @see #isExited(int)
     */
    public final boolean checkCancel(int rid) {
        if (rid != this.reentryId) { // exit
            return true;
        }
        if (cancelToken.isCancelling()) {
            setCancelled();
            return true;
        }
        return false;
    }

    /**
     * 重入id对应的任务是否已退出，即：是否已执行{@link #exit()}方法。
     * 1.如果已退出，当前逻辑应该立即退出。
     * 2.通常在执行外部代码后都应该检测，eg：运行子节点，派发事件，执行用户钩子...
     * 3.通常循环体中的代码应该调用{@link #checkCancel(int)}
     *
     * @param rid 重入id；方法保存的局部变量
     * @return 重入id对应的任务是否已退出
     */
    public final boolean isExited(int rid) {
        return rid != this.reentryId;
    }

    /**
     * 查询任务是否已被重入
     * 1.若任务已经重入，则表示产生了递归执行
     * 2.若任务未被重入，则可能正在运行或已结束
     */
    public final boolean isReentered(int rid) {
        return (rid != this.reentryId) && (rid + 1 != this.reentryId);
    }

    /**
     * 获取重入id
     * 1.重入id用于解决事件（或外部逻辑）可能使当前Task进入完成状态的问题。
     * 2.如果执行的外部逻辑可能触发状态切换，在执行外部逻辑前最好捕获重入id，再执行外部逻辑后以检查是否可进行运行。
     */
    public final int getReentryId() {
        return reentryId; // 勿修改返回值类型，以便以后扩展
    }

    /**
     * 任务是否未启动就失败了。常见原因：
     * 1. 前置条件失败
     * 2. 任务开始前检测到取消
     */
    public final boolean isStillborn() {
        return (ctl & MASK_STILLBORN) != 0;
    }

    /**
     * 运行的帧数
     * 1.任务如果在首次{@link #execute()}的时候就进入完成状态，那么运行帧数0
     * 2.运行帧数是非常重要的统计属性，值得我们定义在顶层.
     */
    public final int getRunFrames() {
        if (status == Status.RUNNING) {
            return taskEntry.getCurFrame() - enterFrame;
        }
        if (taskEntry == null) {
            return 0;
        }
        return exitFrame - enterFrame;
    }

    /**
     * run方法是否是enter触发的
     * 1.用于{@link #execute()}方法判断当前是否和{@link #enter(int)}在同一帧，以决定是否执行某些逻辑。
     * 2.如果仅仅是想在下一帧运行{@link #execute()}的逻辑，可通过{@link #setDisableEnterExecute(boolean)} 实现。
     * 3.部分Task的{@link #execute()}可能在一帧内执行多次，因此不能通过运行帧数为0代替。
     */
    public final boolean isExecuteTriggeredByEnter() {
        return (ctl & MASK_ENTER_EXECUTE) != 0;
    }

    /** exit方法是否是由{@link #stop()}方法触发的 */
    public final boolean isExitTriggeredByStop() {
        return (ctl & MASK_STOP_EXIT) != 0;
    }

    /**
     * 告知模板方法是否自动检测取消
     * 1.默认值由{@link #flags}中的信息指定，默认自动检测
     * 2.自动检测取消信号是一个动态的属性，可随时更改 -- 因此不要轻易缓存。
     */
    public final void setAutoCheckCancel(boolean enable) {
        setCtlBit(MASK_DISABLE_AUTO_CHECK_CANCEL, !enable);
    }

    public final boolean isAutoCheckCancel() {
        return (ctl & MASK_DISABLE_AUTO_CHECK_CANCEL) == 0; // 执行频率很高，不调用封装方法
    }

    /**
     * 告知模板方法是否自动监听取消事件
     * 1.默认值由{@link #flags}中的信息指定，默认不自动监听！自动监听有较大的开销，绝大多数业务只需要在Entry监听。
     * 2.要覆盖默认值应当在{@link #beforeEnter()}方法中调用
     */
    public final void setAutoListenCancel(boolean enable) {
        setCtlBit(MASK_AUTO_LISTEN_CANCEL, enable);
    }

    public final boolean isAutoListenCancel() {
        return (ctl & MASK_AUTO_LISTEN_CANCEL) != 0;
    }

    /**
     * 告知模板方法否将{@link #enter(int)}和{@link #execute()}方法分开执行，
     * 1.默认值由{@link #flags}中的信息指定，默认不禁用
     * 2.要覆盖默认值应当在{@link #beforeEnter()}方法中调用
     * 3.该属性运行期间不应该调整
     */
    public final void setDisableEnterExecute(boolean disable) {
        setCtlBit(MASK_DISABLE_ENTER_EXECUTE, disable);
    }

    public final boolean isDisableEnterExecute() {
        return (ctl & MASK_DISABLE_ENTER_EXECUTE) != 0;
    }

    /**
     * 告知模板方法是否在{@link #enter(int)}前自动调用{@link #resetChildrenForRestart()}
     * 1.默认值由{@link #flags}中的信息指定，默认不启用
     * 2.要覆盖默认值应当在{@link #beforeEnter()}方法中调用
     * 3.部分任务可能在调用{@link #resetForRestart()}之前不会再次运行
     */
    public final void setAutoResetChildren(boolean enable) {
        setCtlBit(MASK_AUTO_RESET_CHILDREN, enable);
    }

    public final boolean isAutoResetChildren() {
        return (ctl & MASK_AUTO_RESET_CHILDREN) != 0;
    }

    /**
     * 是否禁用延迟通知
     * 1.默认值由{@link #flags}中的信息指定，默认不禁用（即默认延迟通知）
     * 2.要覆盖默认值应当在{@link #beforeEnter()}方法中调用
     * 3.用于解决事件驱动模式下调用栈深度问题 -- 类似尾递归优化，可减少一半栈深度
     * 4.先更新自己为完成状态，再通知父节点，这有时候非常有用 -- 在状态机中，你的State可以感知自己是成功或是失败。
     * 5.该属性运行期间不应该调整
     * <p>
     * 理论基础：99.99% 的情况下，Task在调用 setRunning 等方法后会立即return，那么在当前方法退出后再通知父节点就不会破坏时序。
     */
    public final void setDisableDelayNotify(boolean disable) {
        setCtlBit(MASK_DISABLE_DELAY_NOTIFY, disable);
    }

    public final boolean isDisableDelayNotify() {
        return (ctl & MASK_DISABLE_DELAY_NOTIFY) != 0;
    }

    private static boolean checkNotifyMask(int ctl) {
        return (ctl & (MASK_DISABLE_NOTIFY | MASK_STOP_EXIT)) == 0;
    }

    private static boolean checkDelayNotifyMask(int ctl) {
        return (ctl & (MASK_DISABLE_NOTIFY | MASK_STOP_EXIT | MASK_DISABLE_DELAY_NOTIFY)) == 0;
    }

    private static boolean checkImmediateNotifyMask(int ctl) {
        return (ctl & (MASK_DISABLE_NOTIFY | MASK_STOP_EXIT)) == 0 // 被stop取消的任务不能通知
                && ((ctl & MASK_DISABLE_DELAY_NOTIFY) != 0 || (ctl & MASK_EXECUTING) == 0); // 前者多为true，否则多为false
    }
    // endregion

    // region 模板方法

    /** enter方法不暴露，否则以后难以改动 */
    final void template_enterExecute(final Task<E> control, int initMask) {
        initMask |= (flags & MASK_CONTROL_FLOW_FLAGS);
        if (control != null) {
            initMask |= captureContext(control);
        }
        ctl = initMask; // 初始化基础上下文后才可以检测取消，包括控制流标记

        final CancelToken cancelToken = this.cancelToken;
        if (cancelToken.isCancelling() && isAutoCheckCancel()) { // 胎死腹中
            releaseContext();
            setCompleted(Status.CANCELLED, false);
            return;
        }

        int prevStatus = this.status;
        initMask |= Math.min(MASK_PREV_STATUS, prevStatus);
        initMask |= (MASK_ENTER_EXECUTE | MASK_EXECUTING);
        ctl = initMask;

        status = Status.RUNNING; // 先更新为running状态，以避免执行过程中外部查询task的状态时仍处于上一次的结束status
        enterFrame = exitFrame = taskEntry.getCurFrame();
        final int reentryId = ++this.reentryId;  // 和上次执行的exit分开
        try {
            beforeEnter();
            if (prevStatus != Status.NEW && isAutoResetChildren()) {
                resetChildrenForRestart();
            }
            enter(reentryId);
            if (isExited(reentryId)) { // enter 可能导致结束
                if (reentryId + 1 == this.reentryId && checkDelayNotifyMask(ctl) && control != null) {
                    control.onChildCompleted(this);
                }
                return;
            }
            if (isDisableEnterExecute()) { // 需要下一帧执行execute
                checkFireRunningAndCancel(control, cancelToken);
                return;
            }

            if (cancelToken.isCancelling() && isAutoCheckCancel()) { // token基本为false，autoCheck基本为true
                setDisableDelayNotify(true);
                setCancelled();
                return;
            }
            if (isAutoListenCancel()) {
                cancelToken.addListener(this);
            }
            execute();
            if (isExited(reentryId)) {
                if (reentryId + 1 == this.reentryId && checkDelayNotifyMask(ctl) && control != null) {
                    control.onChildCompleted(this);
                }
            } else {
                checkFireRunningAndCancel(control, cancelToken);
            }
        } finally {
            if (reentryId == this.reentryId || reentryId + 1 == this.reentryId) { // 否则可能清理掉递归任务的数据
                ctl &= ~(MASK_ENTER_EXECUTE | MASK_EXECUTING);
            }
        }
    }

    private void checkFireRunningAndCancel(Task<E> control, CancelToken cancelToken) {
        if (cancelToken.isCancelling() && isAutoCheckCancel()) {
            setDisableDelayNotify(true);
            setCancelled();
            return;
        }
        if (checkNotifyMask(ctl) && control != null) {
            control.onChildRunning(this);
        }
    }

    /**
     * execute模板方法
     * 注：
     * 1.如果想减少方法调用，对于运行中的子节点，可直接调用子节点的模板方法。
     * 2.该方法不可以递归执行，如果在正在执行的情况下想执行{@link #execute()}方法，就直接调用 -- {@link #isExecuting()}
     */
    public final void template_execute() {
        final CancelToken cancelToken = this.cancelToken;
        final int reentryId = this.reentryId;
        if (cancelToken.isCancelling() && isAutoCheckCancel()) {
            setDisableDelayNotify(true);
            setCancelled();
            return;
        }
        ctl |= MASK_EXECUTING;
        try {
            execute();
        } finally {
            if (reentryId == this.reentryId || reentryId + 1 == this.reentryId) { // 否则可能清理掉递归任务的数据
                ctl &= ~MASK_EXECUTING;
            }
        }
        if (isExited(reentryId)) {
            if (reentryId + 1 == this.reentryId && checkDelayNotifyMask(ctl) && control != null) {
                control.onChildCompleted(this);
            }
        } else {
            if (cancelToken.isCancelling() && isAutoCheckCancel()) {
                setDisableDelayNotify(true);
                setCancelled();
            }
        }
    }

    private void template_exit(int extraMask) {
        if (extraMask != 0) {
            ctl |= extraMask;
        }
        exitFrame = taskEntry.getCurFrame();
        if (isAutoListenCancel()) {
            cancelToken.removeListener(this);
        }
        try {
            stopRunningChildren();
            exit();
        } finally {
            reentryId++;
            releaseContext();
        }
    }

    /**
     * 运行子节点，会检查子节点的前置条件
     *
     * @param child 普通子节点，或需要接收通知的钩子任务
     */
    public final void template_runChild(Task<E> child) {
        assert isReady() : "Task is not ready";
        if (child.status == Status.RUNNING) {
            child.template_execute();
        } else if (child.guard == null || template_checkGuard(child.guard)) {
            child.template_enterExecute(this, 0);
        } else {
            child.setGuardFailed(this);
        }
    }

    /** 运行子节点，不检查子节点的前置条件 */
    public final void template_runChildDirectly(Task<E> child) {
        assert isReady() : "Task is not ready";
        if (child.status == Status.RUNNING) {
            child.template_execute();
        } else {
            child.template_enterExecute(this, 0);
        }
    }

    /**
     * 执行钩子任务，会检查前置条件
     * 1.钩子任务不会触发{@link #onChildRunning(Task)}和{@link #onChildCompleted(Task)}
     * 2.前置条件其实是特殊的钩子任务
     *
     * @param hook 钩子任务，或不需要接收事件通知的子节点
     */
    public final void template_runHook(Task<E> hook) {
        assert isReady() : "Task is not ready";
        if (hook.status == Status.RUNNING) {
            hook.template_execute();
        } else if (hook.guard == null || template_checkGuard(hook.guard)) {
            hook.template_enterExecute(this, MASK_DISABLE_NOTIFY);
        } else {
            hook.setGuardFailed(this);
        }
    }

    /** 执行钩子任务，不检查前置条件 */
    public final void template_runHookDirectly(Task<E> hook) {
        assert isReady() : "Task is not ready";
        if (hook.status == Status.RUNNING) {
            hook.template_execute();
        } else {
            hook.template_enterExecute(this, MASK_DISABLE_NOTIFY);
        }
    }

    /**
     * 检查前置条件
     * 1.如果未显式设置guard的上下文，会默认捕获当前Task的上下文
     * 2.guard的上下文在运行结束后会被清理
     * 3.guard只应该依赖共享上下文(黑板和props)，不应当对父节点做任何的假设。
     * 4.guard永远是检查当前Task的上下文，子节点的guard也不例外。
     * 5.guard通常不应该修改数据
     *
     * @param guard 前置条件；可以是子节点的guard属性，也可以是条件子节点，也可以是外部的条件节点
     */
    public final boolean template_checkGuard(@Nullable Task<E> guard) {
        assert isReady() : "Task is not ready";
        if (guard == null) {
            return true;
        }
        try {
            // 极少情况下会有前置的前置，更推荐组合节点，更清晰；guard的guard也是检测当前上下文
            if (guard.guard != null && !template_checkGuard(guard.guard)) {
                return false;
            }
            guard.template_enterExecute(this, MASK_DISABLE_NOTIFY);
            switch (guard.getNormalizedStatus()) {
                case Status.SUCCESS -> {
                    return true;
                }
                case Status.ERROR -> {
                    return false;
                }
                default -> {
                    throw new IllegalStateException("Illegal guard status '%d'. Guards must either succeed or fail in one step."
                            .formatted(guard.getStatus()));
                }
            }
        } finally {
            guard.unsetControl(); // 条件类节点总是及时清理
        }
    }

    /** @return 内部使用的mask */
    private int captureContext(Task<E> control) {
        this.taskEntry = control.taskEntry;
        this.control = control;

        // 如果黑板不为null，则认为是control预设置的；其它属性同理
        int r = 0;
        if (this.blackboard == null) {
            this.blackboard = Objects.requireNonNull(control.blackboard);
            r |= MASK_INHERITED_BLACKBOARD;
        }
        if (this.cancelToken == null) {
            this.cancelToken = Objects.requireNonNull(control.cancelToken);
            r |= MASK_INHERITED_CANCEL_TOKEN;
        }
        if (this.sharedProps == null && control.sharedProps != null) {
            this.sharedProps = control.sharedProps;
            r |= MASK_INHERITED_PROPS;
        }
        return r;
    }

    /** 释放自动捕获的上下文 -- 如果保留上次的上下文，下次执行就会出错（guard是典型） */
    private void releaseContext() {
        final int ctl = this.ctl;
        if ((ctl & MASK_INHERITED_BLACKBOARD) != 0) {
            blackboard = null;
        }
        if ((ctl & MASK_INHERITED_CANCEL_TOKEN) != 0) {
            cancelToken = null;
        }
        if ((ctl & MASK_INHERITED_PROPS) != 0) {
            sharedProps = null;
        }
    }

    /**
     * 设置任务的控制节点
     */
    public final void setControl(Task<E> control) {
        assert control != this;
        if (this == taskEntry) {
            throw new Error();
        }
        this.taskEntry = Objects.requireNonNull(control.taskEntry);
        this.control = control;
    }

    /**
     * 删除任务的控制节点(用于清理)
     * 该方法在任务结束时并不会自动调用，因为Task上的数据可能是有用的，不能立即删除，只有用户知道是否可以清理。
     */
    public final void unsetControl() {
        if (this == taskEntry) {
            throw new Error();
        }
        this.taskEntry = null;
        this.control = null;
        this.blackboard = null;
        this.cancelToken = null;
        this.sharedProps = null;
        this.controlData = null;
    }
    // endregion

    // region child维护

    /**
     * 1.尽量不要在运行时增删子节点（危险操作）
     * 2.不建议将Task从一棵树转移到另一棵树，可能产生内存泄漏（引用未删除干净）
     * 3.如果需要知道task的索引，可提前调用{@link #getChildCount()}
     *
     * @param task 要添加的子节点
     * @return this
     */
    public final Task<E> addChild(final Task<E> task) {
        checkAddChild(task);
        addChildImpl(task);
        return this;
    }

    /**
     * 替换指定索引位置的child
     * （该方法可避免Task的结构发生变化，也可以减少事件数）
     *
     * @return index对应的旧节点
     */
    public final Task<E> setChild(int index, Task<E> newTask) {
        checkAddChild(newTask);
        return setChildImpl(index, newTask);
    }

    private void checkAddChild(Task<E> child) {
        if (null == child) {
            throw new NullPointerException("child");
        }
        if (child.control != this) {
            // 必须先从旧的父节点上删除，但有可能是自己之前放在一边的子节点
            if (child.taskEntry != null || child.control != null) {
                throw new IllegalArgumentException("child.control is not null");
            }
        }
        if (this == child) {
            throw new IllegalArgumentException("add self to children");
        }
        assert !child.isRunning();
    }

    public final boolean removeChild(final Task<?> task) {
        if (null == task) {
            throw new NullPointerException("task");
        }
        // child未启动的情况下，control可能尚未赋值，因此不能检查control来判别
        int index = indexChild(task);
        if (index > 0) {
            removeChildImpl(index);
            task.unsetControl();
            return true;
        }
        return false;
    }

    /** 删除指定索引的child */
    public final Task<E> removeChild(int index) {
        Task<E> child = removeChildImpl(index);
        child.unsetControl();
        return child;
    }

    /** 删除所有的child -- 不是个常用方法 */
    public void removeAllChild() {
        for (int idx = 0, size = getChildCount(); idx < size; idx++) {
            removeChildImpl(idx).unsetControl();
        }
    }

    /** @return index or -1 */
    public int indexChild(Task<?> task) {
        for (int idx = 0, size = getChildCount(); idx < size; idx++) {
            if (getChild(idx) == task) {
                return idx;
            }
        }
        return -1;
    }

    /** 该接口主要用于测试，该接口有一定的开销 */
    public abstract Stream<Task<E>> childStream();

    /** 子节点的数量（仅包括普通意义上的child，不包括钩子任务） */
    public abstract int getChildCount();

    /** 获取指定索引的child */
    public abstract Task<E> getChild(int index);

    /** @return 为child分配的index */
    protected abstract int addChildImpl(Task<E> task);

    /** @return 索引位置旧的child */
    protected abstract Task<E> setChildImpl(int index, final Task<E> task);

    /** @return index对应的child */
    protected abstract Task<E> removeChildImpl(int index);

    // endregion

    // region util

    /**
     * 进行互斥
     * 1.该锁是简单的互斥锁，不支持重入。
     * 2.底层会在exit的时候清理数据，不会影响下次运行。
     *
     * @param lockId 锁id，范围 [1 ~ 4]
     */
    public final void lock(int lockId) {
        final int mask = maskOfLock(lockId);
        if ((mask & ctl) != 0) {
            throw new IllegalStateException("Lock reentry is not supported, lock: " + lockId);
        }
        ctl |= mask;
    }

    public final boolean tryLock(int lockId) {
        final int mask = maskOfLock(lockId);
        if ((mask & ctl) != 0) {
            return false;
        }
        ctl |= mask;
        return true;
    }

    public final void unlock(int lockId) {
        final int mask = maskOfLock(lockId);
        if ((mask & ctl) == 0) {
            throw new IllegalStateException("You do not own the lock: " + lockId);
        }
        ctl &= ~mask;
    }

    public final boolean isLocked(int lockId) {
        return (ctl & maskOfLock(lockId)) != 0;
    }

    private static int maskOfLock(int lockId) {
        return switch (lockId) {
            case 1 -> MASK_LOCK1;
            case 2 -> MASK_LOCK2;
            case 3 -> MASK_LOCK3;
            case 4 -> MASK_LOCK4;
            default -> throw new IllegalArgumentException("unsupported lockId: " + lockId);
        };
    }

    public static void stop(@Nullable Task<?> task) {
        // java不支持 obj?.method 语法，我们只能封装额外的方法实现
        if (task != null && task.status == Status.RUNNING) {
            task.stop();
        }
    }

    public static void stopSafely(@Nullable Task<?> task) {
        if (task != null && task.status == Status.RUNNING) {
            try {
                task.stop();
            } catch (Exception | AssertionError e) {
                logger.warn("task stop caught exception", e);
            }
        }
    }

    public static void resetForRestart(@Nullable Task<?> task) {
        if (task != null && task.status != Status.NEW) {
            task.resetForRestart();
        }
    }

    /** 测试Task是否处于可执行状态 -- 该测试并不完全，仅用于简单的断言 */
    private boolean isReady() {
        if (isRunning()) {
            return true;
        }
        if (this == taskEntry) {
            return taskEntry.isInited();
        }
        return taskEntry != null && control != null
                && blackboard != null && cancelToken != null;
    }

    private void setCtlBit(int mask, boolean enable) {
        if (enable) {
            ctl |= mask;
        } else {
            ctl &= ~mask;
        }
    }

    private boolean getCtlBit(int mask) {
        return (ctl & mask) != 0;
    }

    // endregion

    // region 序列化

    public final Task<E> getGuard() {
        return guard;
    }

    public final Task<E> setGuard(Task<E> guard) {
        this.guard = guard;
        return this;
    }

    public final int getFlags() {
        return flags;
    }

    public final Task<E> setFlags(int flags) {
        this.flags = flags;
        return this;
    }

    // endregion
}