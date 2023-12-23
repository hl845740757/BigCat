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

import cn.wjybxx.common.btree.fsm.ChangeStateArgs;
import cn.wjybxx.common.btree.fsm.ChangeStateTask;
import cn.wjybxx.common.btree.fsm.StateMachineTask;
import cn.wjybxx.common.btree.leaf.Success;
import cn.wjybxx.common.btree.leaf.WaitFrame;
import org.apache.commons.lang3.mutable.MutableBoolean;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/12/2
 */
public class StateMachineTest {

    private static int global_count = 0;
    private static boolean delayChange = false;
    private static final int queue_size = 5;

    @BeforeEach
    void setUp() {
        global_count = 0;
        delayChange = false;
    }

    private static TaskEntry<Blackboard> newStateMachineTree() {
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry();
        taskEntry.setRootTask(new StateMachineTask<>());

        StateMachineTask<Blackboard> stateMachineTask = taskEntry.getRootStateMachine();
        stateMachineTask.setName("RootStateMachine");
        stateMachineTask.setUndoQueueSize(queue_size);
        stateMachineTask.setRedoQueueSize(queue_size);
        stateMachineTask.setNoneChildStatus(Status.SUCCESS);
        return taskEntry;
    }

    // region reentry

    /** 不延迟的情况下，三个任务都会进入被取消状态 */
    @Test
    void testCount() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        taskEntry.getRootStateMachine().changeState(new StateA<>());
        BtreeTestUtil.untilCompleted(taskEntry);
        taskEntry.getRootStateMachine().setListener((stateMachineTask, curState, nextState) -> {
            Assertions.assertTrue(curState.isCancelled());
        });
        Assertions.assertEquals(3, global_count);
    }

    /** 延迟到当前状态退出后切换，三个任务都会进入成功完成状态 */
    @Test
    void testCountDelay() {
        delayChange = true;
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        taskEntry.getRootStateMachine().changeState(new StateA<>());
        BtreeTestUtil.untilCompleted(taskEntry);
        taskEntry.getRootStateMachine().setListener((stateMachineTask, curState, nextState) -> {
            Assertions.assertTrue(curState.isSucceeded());
        });
        Assertions.assertEquals(3, global_count);
    }

    /** 测试同一个状态重入 */
    @Test
    void testReentry() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StateA<Blackboard> stateA = new StateA<>();
        StateB<Blackboard> stateB = new StateB<>();
        stateA.nextState = stateB;
        stateB.nextState = stateA;
        taskEntry.getRootStateMachine().changeState(stateA);

        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertEquals(3, global_count);
    }

    private static class StateA<E> extends ActionTask<E> {

        Task<E> nextState;

        @Override
        protected int executeImpl() {
            if (global_count++ == 0) {
                if (nextState == null) {
                    nextState = new StateB<>();
                }
                ChangeStateArgs args = delayChange ? ChangeStateArgs.PLAIN_WHEN_COMPLETED : ChangeStateArgs.PLAIN;
                StateMachineTask.findStateMachine(this).changeState(nextState, args);
            }
            return Status.SUCCESS;
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) {

        }
    }

    private static class StateB<E> extends ActionTask<E> {

        Task<E> nextState;

        @Override
        protected int executeImpl() {
            if (global_count++ == 1) {
                if (nextState == null) {
                    nextState = new StateA<>();
                }
                ChangeStateArgs args = delayChange ? ChangeStateArgs.PLAIN_WHEN_COMPLETED : ChangeStateArgs.PLAIN;
                StateMachineTask.findStateMachine(this).changeState(nextState, args);
            }
            return Status.SUCCESS;
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) {

        }
    }
    // endregion

    // region redo/undo


    /** redo，计数从 0 加到 5 */
    @Test
    void testRedo() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StateMachineTask<Blackboard> stateMachine = taskEntry.getRootStateMachine();
        fillRedoQueue(stateMachine);

        stateMachine.setStateMachineHandler((stateMachineTask, preState) -> stateMachineTask.redoChangeState());
        stateMachine.redoChangeState(); // 初始化

        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertEquals(queue_size, global_count);
    }

    private static void fillRedoQueue(StateMachineTask<Blackboard> stateMachine) {
        stateMachine.getRedoQueue().addLast(new RedoState<>(0));
        stateMachine.getRedoQueue().addLast(new RedoState<>(1));
        stateMachine.getRedoQueue().addLast(new RedoState<>(2));
        stateMachine.getRedoQueue().addLast(new RedoState<>(3));
        stateMachine.getRedoQueue().addLast(new RedoState<>(4));
    }

    /** undo，计数从 5 减到 0 */
    @Test
    void testUndo() {
        global_count = queue_size;
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StateMachineTask<Blackboard> stateMachine = taskEntry.getRootStateMachine();
        fillUndoQueue(stateMachine);

        stateMachine.setStateMachineHandler((stateMachineTask, preState) -> stateMachineTask.undoChangeState());
        stateMachine.undoChangeState(); // 初始化

        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertEquals(0, global_count);
    }

    private static void fillUndoQueue(StateMachineTask<Blackboard> stateMachine) {
        stateMachine.getUndoQueue().addLast(new UndoState<>(1)); // addLast容易写
        stateMachine.getUndoQueue().addLast(new UndoState<>(2));
        stateMachine.getUndoQueue().addLast(new UndoState<>(3));
        stateMachine.getUndoQueue().addLast(new UndoState<>(4));
        stateMachine.getUndoQueue().addLast(new UndoState<>(5));
    }

    /** redo再undo，计数从0加到5，再减回0 */
    @Test
    void testRedoUndo() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StateMachineTask<Blackboard> stateMachine = taskEntry.getRootStateMachine();
        fillRedoQueue(stateMachine);

        MutableBoolean redoFinished = new MutableBoolean(false);
        stateMachine.setStateMachineHandler((stateMachineTask, preState) -> {
            if (!redoFinished.booleanValue()) {
                if (stateMachineTask.redoChangeState()) {
                    return true;
                }
                Assertions.assertEquals(queue_size, global_count);
                fillUndoQueue(stateMachine);
                redoFinished.setTrue();
            }
            return stateMachineTask.undoChangeState();
        });

        stateMachine.redoChangeState(); // 初始化
        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertEquals(0, global_count);
    }

    private static class UndoState<E> extends ActionTask<E> {

        final int expected;

        private UndoState(int expected) {
            this.expected = expected;
        }

        @Override
        protected int executeImpl() {
            if (BtreeTestUtil.random.nextBoolean()) {
                return Status.RUNNING; // 随机等待
            }
            if (global_count == expected) {
                global_count--;
                return Status.SUCCESS;
            }
            return Status.ERROR;
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) {

        }
    }

    private static class RedoState<E> extends ActionTask<E> {

        final int expected;

        private RedoState(int expected) {
            this.expected = expected;
        }

        @Override
        protected int executeImpl() {
            if (BtreeTestUtil.random.nextBoolean()) {
                return Status.RUNNING; // 随机等待
            }
            if (global_count == expected) {
                global_count++;
                return Status.SUCCESS;
            }
            return Status.ERROR;
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) {

        }
    }
    // endregion

    // region 传统状态机样式

    @Test
    void testDelayExecute() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        ClassicalState<Blackboard> nextState = new ClassicalState<>();
        taskEntry.getRootStateMachine().changeState(nextState);
        BtreeTestUtil.untilCompleted(taskEntry);

        Assertions.assertTrue(nextState.isSucceeded());
        Assertions.assertTrue(taskEntry.isSucceeded());
    }

    /** 传统状态机下的状态；期望enter和execute分开执行 */
    private static class ClassicalState<E> extends LeafTask<E> {
        @Override
        protected void beforeEnter() {
            super.beforeEnter();
            setDisableEnterExecute(true);
        }

        @Override
        protected void execute() {
            if (getRunFrames() != 1) {
                throw new IllegalStateException();
            }
            setSuccess();
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) {

        }
    }
    // endregion

    // region changeState

    /**
     * {@link ChangeStateTask}先更新为完成，然后再调用的{@link StateMachineTask#changeState(Task)}，
     * 因此完成应该处于成功状态
     */
    @Test
    void testChangeStateTask() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        ChangeStateTask<Blackboard> stateTask = new ChangeStateTask<>(new Success<>());
        taskEntry.getRootStateMachine().changeState(stateTask);

        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertTrue(stateTask.isSucceeded(), "ChangeState task is cancelled? code: " + stateTask.getStatus());
    }

    @Test
    void testDelay_currentCompleted() {
        final int runFrames = 10;
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StateMachineTask<Blackboard> rootStateMachine = taskEntry.getRootStateMachine();
        rootStateMachine.setListener((stateMachineTask, curState, nextState) -> {
            if (curState != null && nextState != null) {
                Assertions.assertEquals(runFrames, curState.getRunFrames());
            }
        });
        rootStateMachine.changeState(new WaitFrame<>(runFrames));
        taskEntry.update(0); // 启动任务树，使行为树处于运行状态

        rootStateMachine.changeState(new WaitFrame<>(1), ChangeStateArgs.PLAIN_WHEN_COMPLETED);
        BtreeTestUtil.untilCompleted(taskEntry);
    }

    @Test
    void testDelay_nextFrame() {
        final int runFrames = 10;
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StateMachineTask<Blackboard> rootStateMachine = taskEntry.getRootStateMachine();
        rootStateMachine.setListener((stateMachineTask, curState, nextState) -> {
            if (curState != null && nextState != null) {
                Assertions.assertEquals(1, curState.getRunFrames());
            }
        });
        rootStateMachine.changeState(new WaitFrame<>(runFrames));
        taskEntry.update(0); // 启动任务树，使行为树处于运行状态

        rootStateMachine.changeState(new WaitFrame<>(1), ChangeStateArgs.PLAIN_NEXT_FRAME);
        BtreeTestUtil.untilCompleted(taskEntry);
    }

    @Test
    void testDelay_specialFrame() {
        final int runFrames = 10;
        final int spFrame = 5;
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StateMachineTask<Blackboard> rootStateMachine = taskEntry.getRootStateMachine();
        rootStateMachine.setListener((stateMachineTask, curState, nextState) -> {
            if (curState != null && nextState != null) {
                Assertions.assertEquals(spFrame, curState.getRunFrames());
            }
        });
        rootStateMachine.changeState(new WaitFrame<>(runFrames));
        taskEntry.update(0); // 启动任务树，使行为树处于运行状态

        rootStateMachine.changeState(new WaitFrame<>(1),
                ChangeStateArgs.PLAIN_NEXT_FRAME.withFrame(spFrame)); // 在给定帧切换
        BtreeTestUtil.untilCompleted(taskEntry);
    }

    // endregion

}