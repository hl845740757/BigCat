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
        protected void onEventImpl(@Nonnull Object event) throws Exception {

        }
    }
}