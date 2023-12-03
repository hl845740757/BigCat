package cn.wjybxx.common.btree;

import cn.wjybxx.common.btree.leaf.WaitFrame;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

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
}