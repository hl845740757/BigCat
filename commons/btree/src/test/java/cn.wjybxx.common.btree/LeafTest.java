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

import cn.wjybxx.common.btree.leaf.WaitFrame;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import javax.annotation.Nonnull;

/**
 * 一些特殊的叶子节点测试
 *
 * @author wjybxx
 * date - 2023/12/3
 */
public class LeafTest {

    @Test
    void waitFrameTest() {
        int expectedFrame = 10;
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(new WaitFrame<>(expectedFrame));
        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertEquals(expectedFrame, taskEntry.getCurFrame());
    }

    /** 测试ctl中记录的上一次执行结果的正确性 */
    @Test
    void testPrevStatus() {
        PrevStatusTask<Blackboard> root = new PrevStatusTask<>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(root);

        int bound = (Status.MAX_PREV_STATUS + 1) * 2;
        for (int idx = 0; idx < bound; idx++) {
            int prevStatus = taskEntry.getStatus();
            BtreeTestUtil.untilCompleted(taskEntry);

            if (prevStatus >= Status.MAX_PREV_STATUS) {
                Assertions.assertEquals(Status.MAX_PREV_STATUS, taskEntry.getPrevStatus());
            } else {
                Assertions.assertEquals(prevStatus, taskEntry.getPrevStatus());
            }
        }
    }

    /** 测试启动前取消 */
    @Test
    void testStillborn() {
        WaitFrame<Blackboard> waitFrame = new WaitFrame<>(10);
        waitFrame.setCancelToken(new CancelToken(1)); // 提前赋值的token不会被覆盖和删除
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(waitFrame);
        BtreeTestUtil.untilCompleted(taskEntry);

        Assertions.assertTrue(waitFrame.isStillborn());
        Assertions.assertEquals(0, waitFrame.getPrevStatus());
    }

    private static class PrevStatusTask<E> extends ActionTask<E> {

        private int next = Status.SUCCESS;

        @Override
        protected int executeImpl() {
            if (next == Status.GUARD_FAILED) {
                next++;
            }
            return next++;
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) {

        }
    }
}