package cn.wjybxx.common.btree.branch;

import cn.wjybxx.common.btree.Status;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

import javax.annotation.Nullable;
import java.util.List;

/**
 * @author wjybxx
 * date - 2023/11/26
 */

@BinarySerializable
@DocumentSerializable
public class Selector<E> extends SingleRunningChildBranch<E> {

    public Selector() {
    }

    public Selector(List<Task<E>> children) {
        super(children);
    }

    public Selector(Task<E> first, @Nullable Task<E> second) {
        super(first, second);
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        runningChild = null;
        if (child.isCancelled()) {
            setCancelled();
            return;
        }
        if (child.isSucceeded()) {
            setSuccess();
        } else if (isAllChildCompleted()) {
            setFailed(Status.ERROR);
        } else if (!isExecuting()) {
            template_execute();
        }
    }
}