package cn.wjybxx.common.btree;

import cn.wjybxx.common.btree.branch.*;
import cn.wjybxx.common.btree.leaf.Failure;
import cn.wjybxx.common.btree.leaf.SimpleRandom;
import cn.wjybxx.common.btree.leaf.Success;
import cn.wjybxx.common.btree.leaf.WaitFrame;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.RepeatedTest;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2023/11/26
 */
public class SingleRunningTest1 {

    /** 测试需要覆盖成功的子节点数量 [0, 5] */
    private static final int childCount = 5;

    @Test
    void selectorTest() {
        Selector<Blackboard> branch = new Selector<>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);
        for (int expcted = 0; expcted <= childCount; expcted++) {
            BtreeTestUtil.initChildren(branch, childCount, expcted);
            BtreeTestUtil.untilCompleted(taskEntry);

            if (expcted > 0) {
                Assertions.assertTrue(taskEntry.isSucceeded(), "Task is unsuccessful, status " + taskEntry.getStatus());
            } else {
                Assertions.assertTrue(taskEntry.isFailed(), "Task is unfailed, status " + taskEntry.getStatus());
            }
        }
    }

    @Test
    void sequenceTest() {
        Sequence<Blackboard> branch = new Sequence<>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);
        for (int expcted = 0; expcted <= childCount; expcted++) {
            BtreeTestUtil.initChildren(branch, childCount, expcted);
            BtreeTestUtil.untilCompleted(taskEntry);

            if (expcted < childCount) {
                Assertions.assertTrue(taskEntry.isFailed(), "Task is unfailed, status " + taskEntry.getStatus());
            } else {
                Assertions.assertTrue(taskEntry.isSucceeded(), "Task is unsuccessful, status " + taskEntry.getStatus());
            }
        }
    }

    @Test
    void selectorNTest() {
        SelectorN<Blackboard> branch = new SelectorN<>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);
        for (int expcted = 0; expcted <= childCount + 1; expcted++) { // 期望成功的数量，需要包含边界外
            branch.setRequired(expcted);
            for (int real = 0; real <= childCount; real++) { // 真正成功的数量
                BtreeTestUtil.initChildren(branch, childCount, real);
                BtreeTestUtil.untilCompleted(taskEntry);

                if (real >= expcted) {
                    Assertions.assertTrue(taskEntry.isSucceeded(), "Task is unsuccessful, status " + taskEntry.getStatus());
                } else {
                    Assertions.assertTrue(taskEntry.isFailed(), "Task is unfailed, status " + taskEntry.getStatus());
                }

                if (expcted >= childCount) { // 所有子节点完成
                    Assertions.assertEquals(childCount, BtreeTestUtil.completedCount(branch));
                }
            }
        }
    }

    /** 由于可能未命中分支，因此需要循环多次 */
    @RepeatedTest(5)
    void switchTest() {
        Switch<Blackboard> branch = new Switch<>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);

        branch.addChild(new WaitFrame<Blackboard>().setGuard(new SimpleRandom<>(0.3f)))
                .addChild(new Success<Blackboard>().setGuard(new SimpleRandom<>(0.4f)))
                .addChild(new Failure<Blackboard>().setGuard(new SimpleRandom<>(0.5f)));
        BtreeTestUtil.untilCompleted(taskEntry);

        Task<Blackboard> runChild = taskEntry.getRootTask().childStream()
                .filter(e -> e.getGuard().isSucceeded())
                .findFirst()
                .orElse(null);
        if (runChild == null) {
            Assertions.assertTrue(taskEntry.isFailed());
        } else {
            Assertions.assertEquals(taskEntry.getStatus(), runChild.getStatus());
        }
    }

    @Test
    void foreachTest() {
        Foreach<Blackboard> branch = new Foreach<>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);

        branch.addChild(new WaitFrame<Blackboard>().setGuard(new SimpleRandom<>(0.3f)))
                .addChild(new Success<Blackboard>().setGuard(new SimpleRandom<>(0.4f)))
                .addChild(new Failure<Blackboard>().setGuard(new SimpleRandom<>(0.5f)));
        BtreeTestUtil.untilCompleted(taskEntry);

        Assertions.assertTrue(taskEntry.isSucceeded());
    }
}