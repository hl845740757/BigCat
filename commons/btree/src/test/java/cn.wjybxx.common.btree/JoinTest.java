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

import cn.wjybxx.common.btree.branch.Join;
import cn.wjybxx.common.btree.branch.JoinPolicy;
import cn.wjybxx.common.btree.branch.join.*;
import cn.wjybxx.common.ex.InfiniteLoopException;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import javax.annotation.Nonnull;

/**
 * 测试join时要小心，不要通过{@link BtreeTestUtil#completedCount(Task)}
 * 统计完成的子节点和成功的子节点，这可能统计到上一次的执行结果。
 * 请直接通过{@link Join#getCompletedCount()}和{@link Join#getSucceededCount()}获取。
 *
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
            // 不能过于简单成功，否则无法覆盖所有情况
            if (BtreeTestUtil.random.nextBoolean()) {
                globalCount++;
                setSuccess();
                return Status.SUCCESS;
            }
            return Status.RUNNING;
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) {

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
        Join<Blackboard> rootTask = (Join<Blackboard>) taskEntry.getRootTask();
        // 需要测试子节点数量为0的情况
        for (int i = 0; i <= childCount; i++) {
            rootTask.removeAllChild();
            for (int j = 0; j < i; j++) {
                rootTask.addChild(new Counter<>());
            }
            if (i == 0) {
                Assertions.assertThrowsExactly(InfiniteLoopException.class, () -> {
                    BtreeTestUtil.untilCompleted(taskEntry);
                });
                taskEntry.stop(); // 需要进入完成状态
                Assertions.assertTrue(taskEntry.isCancelled());
            } else {
                globalCount = 0;
                BtreeTestUtil.untilCompleted(taskEntry);
                // ... 这里有上次进入完成状态的子节点，直接遍历子节点进行统计不安全
                Assertions.assertTrue(taskEntry.isSucceeded());
                Assertions.assertEquals(1, rootTask.getCompletedCount());
                Assertions.assertEquals(1, globalCount);
            }
        }
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
        Assertions.assertTrue(rootTask.getChild(0).isSucceeded());
    }

    @Test
    void testSelector() {
        TaskEntry<Blackboard> taskEntry = newJoinTree(JoinSelector.getInstance());
        Join<Blackboard> branch = (Join<Blackboard>) taskEntry.getRootTask();
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
    void testSequence() {
        TaskEntry<Blackboard> taskEntry = newJoinTree(JoinSequence.getInstance());
        Join<Blackboard> branch = (Join<Blackboard>) taskEntry.getRootTask();

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
    void testSelectorN() {
        JoinSelectorN<Blackboard> policy = new JoinSelectorN<>();
        TaskEntry<Blackboard> taskEntry = newJoinTree(policy);
        Join<Blackboard> branch = (Join<Blackboard>) taskEntry.getRootTask();

        for (int expcted = 0; expcted <= childCount + 1; expcted++) { // 期望成功的数量，需要包含边界外
            policy.setRequired(expcted);
            for (int real = 0; real <= childCount; real++) { // 真正成功的数量
                BtreeTestUtil.initChildren(branch, childCount, real);
                BtreeTestUtil.untilCompleted(taskEntry);

                if (real >= expcted) {
                    Assertions.assertTrue(taskEntry.isSucceeded(), "Task is unsuccessful, status " + taskEntry.getStatus());
                } else {
                    Assertions.assertTrue(taskEntry.isFailed(), "Task is unfailed, status " + taskEntry.getStatus());
                }
                if (expcted >= childCount) { // 所有子节点完成
                    Assertions.assertEquals(childCount, branch.getCompletedCount());
                }
            }
        }
    }

    // endregion
}