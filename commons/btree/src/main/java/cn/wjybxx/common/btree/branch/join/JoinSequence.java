package cn.wjybxx.common.btree.branch.join;

import cn.wjybxx.common.btree.Status;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.btree.branch.Join;
import cn.wjybxx.common.btree.branch.JoinPolicy;
import cn.wjybxx.common.btree.branch.Sequence;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.ClassImpl;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * {@link Sequence}
 * 相当于并发编程中的WhenAll
 *
 * @author wjybxx
 * date - 2023/12/2
 */
@ClassImpl(singleton = "getInstance")
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class JoinSequence<E> implements JoinPolicy<E> {

    private static final JoinSequence<?> INSTANCE = new JoinSequence<>();

    @SuppressWarnings("unchecked")
    public static <E> JoinSequence<E> getInstance() {
        return (JoinSequence<E>) INSTANCE;
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
        if (!child.isSucceeded()) {
            join.setCompleted(child.getStatus(), true);
        } else if (join.isAllChildSucceeded()) {
            join.setSuccess();
        }
    }

    @Override
    public void onEvent(Join<E> join, Object event) {

    }

}