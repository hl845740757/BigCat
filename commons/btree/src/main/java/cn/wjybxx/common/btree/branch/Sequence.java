package cn.wjybxx.common.btree.branch;

import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

import javax.annotation.Nullable;
import java.util.List;

/**
 * @author wjybxx
 * date - 2023/11/26
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class Sequence<E> extends SingleRunningChildBranch<E> {

    public Sequence() {
    }

    public Sequence(List<Task<E>> children) {
        super(children);
    }

    public Sequence(Task<E> first, @Nullable Task<E> second) {
        super(first, second);
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        runningChild = null;
        if (child.isCancelled()) {
            setCancelled();
            return;
        }
        if (child.isFailed()) { // 失败码有传递的价值
            setCompleted(child.getStatus(), true);
        } else if (isAllChildCompleted()) {
            setSuccess();
        } else if (!isExecuting()) {
            template_execute();
        }
    }
}
