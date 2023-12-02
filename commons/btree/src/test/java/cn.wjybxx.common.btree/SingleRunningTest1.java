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

import java.util.HashMap;

/**
 * @author wjybxx
 * date - 2023/11/26
 */
public class SingleRunningTest1 {

    // region util

    public static TaskEntry<Object> newTaskEntry() {
        return new TaskEntry<>("Main", null, new HashMap<>(), null, TreeLoader.nullLoader());
    }

    public static void untilCompleted(TaskEntry<Object> entry) {
        for (int idx = 0; idx < 100; idx++) { // 避免死循环
            entry.update(idx);
            if (entry.isCompleted()) break;
        }
    }

    public static int completedCount(Task<?> ctrl) {
        return (int) ctrl.childStream()
                .filter(Task::isCompleted)
                .count();
    }

    public static int succeededCount(Task<?> ctrl) {
        return (int) ctrl.childStream()
                .filter(Task::isSucceeded)
                .count();
    }

    public static int failedCount(Task<?> ctrl) {
        return (int) ctrl.childStream()
                .filter(Task::isFailed)
                .count();
    }
    // endregion

    @Test
    void selectorTest() {
        TaskEntry<Object> entry = newTaskEntry();
        entry.setRootTask(new Selector<>());
        entry.getRootTask()
                .addChild(new Failure<>())
                .addChild(new Success<>())
                .addChild(new Failure<>());

        untilCompleted(entry);
        Assertions.assertTrue(entry.isSucceeded(), "Task is unsuccessful, status " + entry.getStatus());
    }

    @Test
    void sequenceTest() {
        TaskEntry<Object> entry = newTaskEntry();
        entry.setRootTask(new Sequence<>());
        entry.getRootTask()
                .addChild(new WaitFrame<>(10))
                .addChild(new Failure<>())
                .addChild(new Success<>());

        untilCompleted(entry);
        Assertions.assertTrue(entry.isFailed(), "Task is unfailed, status " + entry.getStatus());
    }

    @Test
    void switchTest() {
        TaskEntry<Object> entry = newTaskEntry();
        entry.setRootTask(new Switch<>());
        entry.getRootTask()
                .addChild(new WaitFrame<>().setGuard(new SimpleRandom<>(0.3f)))
                .addChild(new Success<>().setGuard(new SimpleRandom<>(0.4f)))
                .addChild(new Failure<>().setGuard(new SimpleRandom<>(0.5f)));

        untilCompleted(entry);

        Task<Object> runChild = entry.getRootTask().childStream()
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