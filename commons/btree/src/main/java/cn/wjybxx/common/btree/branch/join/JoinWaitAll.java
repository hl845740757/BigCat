package cn.wjybxx.common.btree.branch.join;

import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.btree.branch.Join;
import cn.wjybxx.common.btree.branch.JoinPolicy;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.ClassImpl;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * 等待所有任务完成后返回成功
 * 相当于并发编程中的WaitAll
 *
 * @author wjybxx
 * date - 2023/12/2
 */
@ClassImpl(singleton = "getInstance")
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class JoinWaitAll<E> implements JoinPolicy<E> {

    private static final JoinWaitAll<?> INSTANCE = new JoinWaitAll<>();

    @SuppressWarnings("unchecked")
    public static <E> JoinWaitAll<E> getInstance() {
        return (JoinWaitAll<E>) INSTANCE;
    }

    @Override
    public void resetForRestart() {

    }

    @Override
    public void beforeEnter(Join<E> join) {

    }

    @Override
    public void onChildCompleted(Join<E> join, Task<E> child) {
        if (join.isAllChildCompleted()) {
            join.setSuccess();
        }
    }

    @Override
    public void onEvent(Join<E> join, Object event) throws Exception {

    }
}