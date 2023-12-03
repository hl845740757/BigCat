package cn.wjybxx.common.btree;

import cn.wjybxx.common.btree.branch.Selector;
import cn.wjybxx.common.btree.branch.Sequence;
import cn.wjybxx.common.btree.branch.Switch;
import cn.wjybxx.common.btree.leaf.Failure;
import cn.wjybxx.common.btree.leaf.SimpleRandom;
import cn.wjybxx.common.btree.leaf.Success;
import cn.wjybxx.common.btree.leaf.WaitFrame;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2023/11/26
 */
public class SingleRunningTest1 {

    @Test
    void selectorTest() {
        TaskEntry<Blackboard> entry = BtreeTestUtil.newTaskEntry();
        entry.setRootTask(new Selector<>());
        entry.getRootTask()
                .addChild(new Failure<>())
                .addChild(new Success<>())
                .addChild(new Failure<>());

        BtreeTestUtil.untilCompleted(entry);
        Assertions.assertTrue(entry.isSucceeded(), "Task is unsuccessful, status " + entry.getStatus());
    }

    @Test
    void sequenceTest() {
        TaskEntry<Blackboard> entry = BtreeTestUtil.newTaskEntry();
        entry.setRootTask(new Sequence<>());
        entry.getRootTask()
                .addChild(new WaitFrame<>(10))
                .addChild(new Failure<>())
                .addChild(new Success<>());

        BtreeTestUtil.untilCompleted(entry);
        Assertions.assertTrue(entry.isFailed(), "Task is unfailed, status " + entry.getStatus());
    }

    @Test
    void switchTest() {
        TaskEntry<Blackboard> entry = BtreeTestUtil.newTaskEntry();
        entry.setRootTask(new Switch<>());
        entry.getRootTask()
                .addChild(new WaitFrame<Blackboard>().setGuard(new SimpleRandom<>(0.3f)))
                .addChild(new Success<Blackboard>().setGuard(new SimpleRandom<>(0.4f)))
                .addChild(new Failure<Blackboard>().setGuard(new SimpleRandom<>(0.5f)));

        BtreeTestUtil.untilCompleted(entry);

        Task<Blackboard> runChild = entry.getRootTask().childStream()
                .filter(e -> e.getGuard().isSucceeded())
                .findFirst()
                .orElse(null);
        if (runChild == null) {
            Assertions.assertTrue(entry.isFailed());
        } else {
            Assertions.assertEquals(entry.getStatus(), runChild.getStatus());
        }
    }
}