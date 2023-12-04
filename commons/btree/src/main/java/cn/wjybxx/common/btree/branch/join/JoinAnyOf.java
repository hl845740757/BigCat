package cn.wjybxx.common.btree.branch.join;

import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.btree.branch.Join;
import cn.wjybxx.common.btree.branch.JoinPolicy;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.ClassImpl;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * 默认的AnyOf，不特殊处理取消
 * 相当于并发编程中的anyOf
 *
 * @author wjybxx
 * date - 2023/12/2
 */
@ClassImpl(singleton = "getInstance")
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class JoinAnyOf<E> implements JoinPolicy<E> {

    private static final JoinAnyOf<?> INSTANCE = new JoinAnyOf<>();

    @SuppressWarnings("unchecked")
    public static <E> JoinAnyOf<E> getInstance() {
        return (JoinAnyOf<E>) INSTANCE;
    }

    @Override
    public void resetForRestart() {

    }

    @Override
    public void beforeEnter(Join<E> join) {

    }

    @Override
    public void onChildEmpty(Join<E> join) {
        // 不能成功，失败也不能
        if (join.isExecuteTriggeredByEnter()) {
            Task.logger.info("JonAnyOf: children is empty");
        }
    }

    @Override
    public void onChildCompleted(Join<E> join, Task<E> child) {
        join.setCompleted(child.getStatus(), true);
    }

    @Override
    public void onEvent(Join<E> join, Object event) {

    }
}