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

import cn.wjybxx.common.btree.fsm.StateMachineTask;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

import javax.annotation.Nonnull;
import java.util.Objects;
import java.util.stream.Stream;

/**
 * 任务入口（可联想程序的Main）
 * <p>
 * 1. 该实现并不是典型的行为树实现，而是更加通用的任务树，因此命名TaskEntry。
 * 2. 该类允许继承，以提供一些额外的方法，但核心方法是禁止重写的。
 * 3. Entry的数据尽量也保存在黑板中，尤其是绑定的实体（Entity），尽可能使业务逻辑仅依赖黑板即可完成。
 * 4. Entry默认不检查{@link #getGuard()}，如果需要由用户（逻辑上的control）检查。
 * 5. 如果要复用行为树，应当以树为单位整体复用，万莫以Task为单位复用 -- 节点之间的引用千丝万缕，容易内存泄漏。
 * 6. 该行为树虽然是事件驱动的，但心跳不是事件，仍需要每一帧调用{@link #update(int)}方法。
 * 7. 避免直接使用外部的{@link CancelToken}，可将Entry的Token注册为外部的Child -- {@link CancelToken#addChild(CancelToken)}。
 *
 * @author wjybxx
 * date - 2023/11/25
 */
@BinarySerializable
@DocumentSerializable
public class TaskEntry<E> extends Task<E> {

    /** 行为树的名字 */
    private String name;
    /** 行为树的根节点 */
    private Task<E> rootTask;
    /** 行为树的类型 -- 表示用途 */
    private int type;

    /** 行为树绑定的实体 -- 最好也存储在黑板里；这里的字段本是为了提高性能 */
    private transient Object entity;
    /** 行为树加载器 -- 用于加载Task或配置 */
    private transient TreeLoader treeLoader;
    /** 当前帧号 */
    private transient int curFrame;
    /** 用于Entry的事件驱动 */
    private transient TaskEntryHandler<E> handler;

    public TaskEntry() {
        this(null, null, null, null, null);
    }

    public TaskEntry(String name,
                     Task<E> rootTask, E blackboard,
                     Object entity, TreeLoader treeLoader) {
        this.name = name;
        this.rootTask = rootTask;
        this.blackboard = blackboard;
        this.entity = entity;
        this.treeLoader = Objects.requireNonNullElse(treeLoader, TreeLoader.nullLoader());

        taskEntry = this;
        cancelToken = new CancelToken();
    }

    // setter
    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public Task<E> getRootTask() {
        return rootTask;
    }

    public void setRootTask(Task<E> rootTask) {
        this.rootTask = rootTask;
    }

    public int getType() {
        return type;
    }

    public void setType(int type) {
        this.type = type;
    }

    public void setEntity(Object entity) {
        this.entity = entity;
    }

    public TreeLoader getTreeLoader() {
        return treeLoader;
    }

    public final void setTreeLoader(TreeLoader treeLoader) {
        this.treeLoader = Objects.requireNonNullElse(treeLoader, TreeLoader.nullLoader());
    }

    public TaskEntryHandler<E> getHandler() {
        return handler;
    }

    public void setHandler(TaskEntryHandler<E> handler) {
        this.handler = handler;
    }

    // endregion

    // region logic

    /**
     * 获取根状态机
     * 状态机太重要了，值得我们为其提供各种快捷方法
     */
    public final StateMachineTask<E> getRootStateMachine() {
        if (rootTask instanceof StateMachineTask<E> stateMachine) {
            return stateMachine;
        }
        throw new IllegalStateException("rootTask is not state machine task");
    }

    /**
     * 用户需要在每一帧调用该方法以驱动心跳逻辑
     */
    public void update(int curFrame) {
        this.curFrame = curFrame;
        if (getStatus() == Status.RUNNING) {
            template_execute();
        } else {
            assert isInited();
            template_enterExecute(null, 0);
        }
    }

    @Override
    protected void execute() {
        template_runChild(rootTask);
    }

    @Override
    protected void onChildRunning(Task<E> child) {

    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        setCompleted(child.getStatus(), true);
        cancelToken.clear(); // 避免内存泄漏
        if (handler != null) {
            handler.onEntryCompleted(this);
        }
    }

    @Override
    public boolean canHandleEvent(@Nonnull Object event) {
        if (isRunning()) {
            return true;
        }
        return rootTask != null && blackboard != null; // 只测isInited的关键属性即可
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) {
        rootTask.onEvent(event);
    }

    @Override
    public final Object getEntity() {
        return entity;
    }

    @Override
    public final int getCurFrame() {
        return curFrame;
    }

    @Override
    public void resetForRestart() {
        super.resetForRestart();
        cancelToken.clear();
        curFrame = 0;
    }

    final boolean isInited() {
        return rootTask != null && blackboard != null && cancelToken != null && treeLoader != null;
    }

    // endregion

    // region child

    @Override
    public final int indexChild(Task<?> task) {
        if (task != null && task == this.rootTask) {
            return 0;
        }
        return -1;
    }

    @Override
    public final Stream<Task<E>> childStream() {
        return Stream.ofNullable(rootTask);
    }

    @Override
    public final int getChildCount() {
        return rootTask == null ? 0 : 1;
    }

    @Override
    public final Task<E> getChild(int index) {
        if (index == 0 && rootTask != null) {
            return rootTask;
        }
        throw new IndexOutOfBoundsException(index);
    }

    @Override
    protected final int addChildImpl(Task<E> task) {
        if (rootTask != null) {
            throw new IllegalStateException("A task entry cannot have more than one child");
        }
        rootTask = task;
        return 0;
    }

    @Override
    protected final Task<E> setChildImpl(int index, Task<E> task) {
        if (index == 0 && rootTask != null) {
            Task<E> r = this.rootTask;
            rootTask = task;
            return r;
        }
        throw new IndexOutOfBoundsException(index);
    }

    @Override
    protected final Task<E> removeChildImpl(int index) {
        if (index == 0 && rootTask != null) {
            Task<E> r = this.rootTask;
            rootTask = null;
            return r;
        }
        throw new IndexOutOfBoundsException(index);
    }

    // endregion
}