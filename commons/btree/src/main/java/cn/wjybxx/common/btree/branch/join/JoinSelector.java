package cn.wjybxx.common.btree.branch.join;

import cn.wjybxx.common.btree.Status;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.btree.branch.Join;
import cn.wjybxx.common.btree.branch.JoinPolicy;
import cn.wjybxx.common.btree.branch.Selector;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.ClassImpl;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * {@link Selector}
 *
 * @author wjybxx
 * date - 2023/12/2
 */
@ClassImpl(singleton = "getInstance")
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class JoinSelector<E> implements JoinPolicy<E> {

    private static final JoinSelector<?> INSTANCE = new JoinSelector<>();

    @SuppressWarnings("unchecked")
    public static <E> JoinSelector<E> getInstance() {
        return (JoinSelector<E>) INSTANCE;
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
        if (child.isSucceeded()) {
            join.setSuccess();
        } else if (join.isAllChildCompleted()) {
            join.setFailed(Status.ERROR);
        }
    }

    @Override
    public void onEvent(Join<E> join, Object event) {

    }
}