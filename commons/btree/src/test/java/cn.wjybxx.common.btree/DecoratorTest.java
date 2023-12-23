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

import cn.wjybxx.common.btree.decorator.*;
import cn.wjybxx.common.btree.leaf.Failure;
import cn.wjybxx.common.ex.InfiniteLoopException;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.RepeatedTest;
import org.junit.jupiter.api.Test;

import javax.annotation.Nonnull;
import java.util.Random;
import java.util.random.RandomGenerator;

/**
 * 一些特殊的装饰器测试
 *
 * @author wjybxx
 * date - 2023/12/3
 */
public class DecoratorTest {

    private static final RandomGenerator random = new Random();
    private static int failedCount;
    private static int successCount;

    @BeforeEach
    void setUp() {
        failedCount = 0;
        successCount = 0;
    }

    private static class CountRandom<E> extends LeafTask<E> {

        final boolean isGuard;

        private CountRandom() {
            isGuard = false;
        }

        public CountRandom(boolean isGuard) {
            this.isGuard = isGuard;
        }

        @Override
        protected void execute() {
            if (!isGuard && getRunFrames() < 3 && random.nextBoolean()) { // 随机等待
                return;
            }

            if (random.nextBoolean()) {
                successCount++;
                setSuccess();
            } else {
                failedCount++;
                setFailed(Status.ERROR);
            }
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) {

        }
    }

    // region repeat
    private static final int REPEAT_COUNT = 10;

    private static Repeat<Blackboard> newRandomRepeat(int mode) {
        Repeat<Blackboard> repeat = new Repeat<>();
        repeat.setRequired(REPEAT_COUNT);
        repeat.setCountMode(mode);
        repeat.setChild(new CountRandom<>());
        return repeat;
    }

    @Test
    void repeatAlwaysTest() {
        Repeat<Blackboard> repeat = newRandomRepeat(Repeat.MODE_ALWAYS);
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(repeat);
        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertEquals(REPEAT_COUNT, successCount + failedCount);
    }

    @Test
    void repeatSuccessTest() {
        Repeat<Blackboard> repeat = newRandomRepeat(Repeat.MODE_ONLY_SUCCESS);
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(repeat);
        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertEquals(REPEAT_COUNT, successCount);
    }

    @Test
    void repeatFailTest() {
        Repeat<Blackboard> repeat = newRandomRepeat(Repeat.MODE_ONLY_FAILED);
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(repeat);
        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertEquals(REPEAT_COUNT, failedCount);
    }

    // endregion

    // region util

    @RepeatedTest(5)
    void untilSuccessTest() {
        UntilSuccess<Blackboard> decorator = new UntilSuccess<>();
        decorator.setChild(new CountRandom<>());
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(decorator);
        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertEquals(1, successCount);
    }

    @RepeatedTest(5)
    void untilFailedTest() {
        UntilFail<Blackboard> decorator = new UntilFail<>();
        decorator.setChild(new CountRandom<>());
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(decorator);
        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertEquals(1, failedCount);
    }

    @Test
    void untilCondTest() {
        UntilCond<Blackboard> decorator = new UntilCond<>();
        decorator.setChild(new Failure<>()); // 子节点忽略
        decorator.setCond(new CountRandom<>(true)); // 条件成功则成功

        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(decorator);
        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertEquals(1, successCount);
    }

    // endregion

    /** OnlyOnce不重置的情况下，每次都返回之前的状态 */
    @Test
    void onlyOnceTest() {
        OnlyOnce<Blackboard> decorator = new OnlyOnce<>();
        decorator.setChild(new CountRandom<>());
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(decorator);
        BtreeTestUtil.untilCompleted(taskEntry);

        final int status = taskEntry.getStatus();
        for (int i = 0; i < 10; i++) {
            BtreeTestUtil.untilCompleted(taskEntry);
            Assertions.assertEquals(status, taskEntry.getStatus());
        }
        Assertions.assertEquals(1, successCount + failedCount);
    }

    @Test
    void alwaysRunningTest() {
        AlwaysRunning<Blackboard> decorator = new AlwaysRunning<>();
        decorator.setChild(new CountRandom<>());
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(decorator);
        Assertions.assertThrowsExactly(InfiniteLoopException.class, () -> BtreeTestUtil.untilCompleted(taskEntry));

        Assertions.assertTrue(taskEntry.isRunning());
        Assertions.assertEquals(1, successCount + failedCount);
    }
}