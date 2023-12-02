package cn.wjybxx.common.btree;

import cn.wjybxx.common.btree.fsm.ChangeStateTask;
import cn.wjybxx.common.btree.fsm.StateMachineTask;
import cn.wjybxx.common.btree.leaf.Success;
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

    private static TaskEntry<Object> newStateMachineTree() {
        TaskEntry<Object> taskEntry = SingleRunningTest1.newTaskEntry();
        taskEntry.setRootTask(new StateMachineTask<>());

        StateMachineTask<Object> stateMachineTask = taskEntry.getRootStateMachine();
        stateMachineTask.setName("RootStateMachine");
        stateMachineTask.setUndoQueueSize(queue_size);
        stateMachineTask.setRedoQueueSize(queue_size);
        stateMachineTask.setNoneChildStatus(Status.SUCCESS);
        return taskEntry;
    }

    @Test
    void testCount() {
        TaskEntry<Object> taskEntry = newStateMachineTree();
        taskEntry.getRootStateMachine().changeState(new StateA<>());
        SingleRunningTest1.untilCompleted(taskEntry);
        Assertions.assertEquals(3, global_count);
    }

    @Test
    void testCountDelay() {
        delayChange = true;
        TaskEntry<Object> taskEntry = newStateMachineTree();
        taskEntry.getRootStateMachine().changeState(new StateA<>());
        SingleRunningTest1.untilCompleted(taskEntry);
        Assertions.assertEquals(3, global_count);
    }

    @Test
    void testRedo() {
        TaskEntry<Object> taskEntry = newStateMachineTree();

        StateMachineTask<Object> stateMachine = taskEntry.getRootStateMachine();
        stateMachine.getRedoQueue().addLast(new RedoState<>(0));
        stateMachine.getRedoQueue().addLast(new RedoState<>(1));
        stateMachine.getRedoQueue().addLast(new RedoState<>(2));
        stateMachine.getRedoQueue().addLast(new RedoState<>(3));
        stateMachine.getRedoQueue().addLast(new RedoState<>(4));

        stateMachine.setStateMachineHandler((stateMachineTask, preState) -> stateMachineTask.redoChangeState());
        stateMachine.redoChangeState(); // redo命令

        SingleRunningTest1.untilCompleted(taskEntry);
        Assertions.assertEquals(queue_size, global_count);
    }

    @Test
    void testUndo() {
        TaskEntry<Object> taskEntry = newStateMachineTree();
        global_count = queue_size;

        StateMachineTask<Object> stateMachine = taskEntry.getRootStateMachine();
        stateMachine.getUndoQueue().addLast(new UndoState<>(1)); // addLast容易写
        stateMachine.getUndoQueue().addLast(new UndoState<>(2));
        stateMachine.getUndoQueue().addLast(new UndoState<>(3));
        stateMachine.getUndoQueue().addLast(new UndoState<>(4));
        stateMachine.getUndoQueue().addLast(new UndoState<>(5));

        stateMachine.setStateMachineHandler((stateMachineTask, preState) -> stateMachineTask.undoChangeState());
        stateMachine.undoChangeState(); // undo命令

        SingleRunningTest1.untilCompleted(taskEntry);
        Assertions.assertEquals(0, global_count);
    }

    @Test
    void testChangeStateTask() {
        TaskEntry<Object> taskEntry = newStateMachineTree();
        ChangeStateTask<Object> stateTask = new ChangeStateTask<>(new Success<>());
        taskEntry.getRootStateMachine().changeState(stateTask);

        SingleRunningTest1.untilCompleted(taskEntry);
        Assertions.assertTrue(stateTask.isSucceeded(), "ChangeState task is cancelled? code: " + stateTask.getStatus());
    }

    private static class UndoState<E> extends ActionTask<E> {

        final int expected;

        private UndoState(int expected) {
            this.expected = expected;
        }

        @Override
        protected int executeImpl() {
            if (global_count == expected) {
                global_count--;
                return Status.SUCCESS;
            }
            return Status.ERROR;
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) throws Exception {

        }
    }

    private static class RedoState<E> extends ActionTask<E> {

        final int expected;

        private RedoState(int expected) {
            this.expected = expected;
        }

        @Override
        protected int executeImpl() {
            if (global_count == expected) {
                global_count++;
                return Status.SUCCESS;
            }
            return Status.ERROR;
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) throws Exception {

        }
    }

    private static class StateA<E> extends ActionTask<E> {

        @Override
        protected int executeImpl() {
            if (global_count++ == 0) {
                int delayMode = delayChange ? StateMachineTask.DELAY_CURRENT_COMPLETED : 0;
                StateMachineTask.findStateMachine(this).changeState(new StateB<>(), delayMode);
            }
            return Status.SUCCESS;
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) throws Exception {

        }
    }

    private static class StateB<E> extends ActionTask<E> {

        @Override
        protected int executeImpl() {
            if (global_count++ == 1) {
                int delayMode = delayChange ? StateMachineTask.DELAY_CURRENT_COMPLETED : 0;
                StateMachineTask.findStateMachine(this).changeState(new StateA<>(), delayMode);
            }
            return Status.SUCCESS;
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) throws Exception {

        }
    }
}