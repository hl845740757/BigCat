package cn.wjybxx.common.btree;

import cn.wjybxx.common.btree.branch.Join;
import cn.wjybxx.common.btree.branch.JoinPolicy;
import cn.wjybxx.common.btree.branch.join.*;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/12/3
 */
public class JoinTest {

    private static TaskEntry<Blackboard> newJoinTree(JoinPolicy<Blackboard> joinPolicy) {
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry();
        Join<Blackboard> join = new Join<>();
        join.setPolicy(joinPolicy);
        taskEntry.setRootTask(join);
        return taskEntry;
    }

    private static int globalCount = 0;
    private static final int childCount = 5;

    @BeforeEach
    void setUp() {
        globalCount = 0;
    }

    // region
    private static class Counter<E> extends ActionTask<E> {

        @Override
        protected int executeImpl() {
            globalCount++;
            return Status.SUCCESS;
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) throws Exception {

        }
    }

    @Test
    void testWaitAll() {
        TaskEntry<Blackboard> taskEntry = newJoinTree(JoinWaitAll.getInstance());

        Task<Blackboard> rootTask = taskEntry.getRootTask();
        for (int i = 0; i < childCount; i++) {
            rootTask.addChild(new Counter<>());
        }

        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertTrue(taskEntry.isSucceeded());
        Assertions.assertEquals(childCount, globalCount);
    }

    /** 测试join多次执行的正确性 */
    @Test
    void testWaitAllMultiLoop() {
        TaskEntry<Blackboard> taskEntry = newJoinTree(JoinWaitAll.getInstance());

        Task<Blackboard> rootTask = taskEntry.getRootTask();
        for (int i = 0; i < childCount; i++) {
            rootTask.addChild(new Counter<>());
        }
        int loop = 3;
        for (int i = 0; i < loop; i++) {
            BtreeTestUtil.untilCompleted(taskEntry);
        }
        Assertions.assertTrue(taskEntry.isSucceeded());
        Assertions.assertEquals(childCount * loop, globalCount);
    }

    @Test
    void testAnyOf() {
        TaskEntry<Blackboard> taskEntry = newJoinTree(JoinAnyOf.getInstance());

        Task<Blackboard> rootTask = taskEntry.getRootTask();
        for (int i = 0; i < childCount; i++) {
            rootTask.addChild(new Counter<>());
        }

        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertTrue(taskEntry.isSucceeded());
        Assertions.assertEquals(1, BtreeTestUtil.completedCount(rootTask));
        Assertions.assertEquals(1, globalCount);
    }

    @Test
    void testMain() {
        TaskEntry<Blackboard> taskEntry = newJoinTree(JoinMain.getInstance());

        Task<Blackboard> rootTask = taskEntry.getRootTask();
        for (int i = 0; i < childCount; i++) {
            rootTask.addChild(new Counter<>());
        }

        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertTrue(taskEntry.isSucceeded());
        Assertions.assertEquals(1, BtreeTestUtil.completedCount(rootTask));
        Assertions.assertEquals(1, globalCount);
    }

    @Test
    void testSelector() {
        TaskEntry<Blackboard> taskEntry = newJoinTree(JoinSelector.getInstance());

        Task<Blackboard> rootTask = taskEntry.getRootTask();
        for (int i = 0; i < childCount; i++) {
            rootTask.addChild(new Counter<>());
        }

        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertTrue(taskEntry.isSucceeded());
        Assertions.assertEquals(1, BtreeTestUtil.completedCount(rootTask));
        Assertions.assertEquals(1, globalCount);
    }

    @Test
    void testSelectorN() {
        final int expected = 3;
        TaskEntry<Blackboard> taskEntry = newJoinTree(new JoinSelectorN<>(expected));

        Task<Blackboard> rootTask = taskEntry.getRootTask();
        for (int i = 0; i < childCount; i++) {
            rootTask.addChild(new Counter<>());
        }

        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertTrue(taskEntry.isSucceeded()); // 成功
        Assertions.assertEquals(expected, BtreeTestUtil.completedCount(rootTask));
        Assertions.assertEquals(expected, globalCount);
    }

    /** 测试选择超过子节点数量的child */
    @Test
    void testSelectorNOver() {
        final int expected = childCount + 1;
        TaskEntry<Blackboard> taskEntry = newJoinTree(new JoinSelectorN<>(expected));

        Task<Blackboard> rootTask = taskEntry.getRootTask();
        for (int i = 0; i < childCount; i++) {
            rootTask.addChild(new Counter<>());
        }

        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertTrue(taskEntry.isFailed()); // 失败
        Assertions.assertEquals(childCount, BtreeTestUtil.completedCount(rootTask));
        Assertions.assertEquals(childCount, globalCount);
    }

    @Test
    void testSequence() {
        TaskEntry<Blackboard> taskEntry = newJoinTree(JoinSequence.getInstance());

        Task<Blackboard> rootTask = taskEntry.getRootTask();
        for (int i = 0; i < childCount; i++) {
            rootTask.addChild(new Counter<>());
        }

        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertTrue(taskEntry.isSucceeded()); // 成功
        Assertions.assertEquals(childCount, BtreeTestUtil.completedCount(rootTask));
        Assertions.assertEquals(childCount, globalCount);
    }

    // endregion
}