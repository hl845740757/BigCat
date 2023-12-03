package cn.wjybxx.common.btree.branch.join;

import cn.wjybxx.common.btree.Status;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.btree.branch.Join;
import cn.wjybxx.common.btree.branch.JoinPolicy;
import cn.wjybxx.common.btree.branch.SimpleParallel;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.ClassImpl;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * Main策略，当第一个任务完成时就完成。
 * 类似{@link SimpleParallel}，但Join在得出结果前不重复运行已完成的子节点
 *
 * @author wjybxx
 * date - 2023/12/2
 */
@ClassImpl(singleton = "getInstance")
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class JoinMain<E> implements JoinPolicy<E> {

    private static final JoinMain<?> INSTANCE = new JoinMain<>();

    @SuppressWarnings("unchecked")
    public static <E> JoinMain<E> getInstance() {
        return (JoinMain<E>) INSTANCE;
    }

    @Override
    public void resetForRestart() {

    }

    @Override
    public void beforeEnter(Join<E> join) {

    }

    @Override
    public void onChildEmpty(Join<E> join) {
        join.setFailed(Status.CHILDLESS);
    }

    @Override
    public void onChildCompleted(Join<E> join, Task<E> child) {
        if (join.isFirstChild(child)) {
            join.setCompleted(child.getStatus(), true);
        }
    }

    @Override
    public void onEvent(Join<E> join, Object event) throws Exception {
        Task<E> firstChild = join.getFirstChild();
        assert firstChild != null;
        firstChild.onEvent(event);
    }
}