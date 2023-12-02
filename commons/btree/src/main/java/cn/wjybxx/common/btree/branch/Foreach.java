package cn.wjybxx.common.btree.branch;

import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

import javax.annotation.Nullable;
import java.util.List;

/**
 * 迭代所有的子节点最后返回成功
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class Foreach<E> extends SingleRunningChildBranch<E> {

    public Foreach() {
    }

    public Foreach(List<Task<E>> children) {
        super(children);
    }

    public Foreach(Task<E> first, @Nullable Task<E> second) {
        super(first, second);
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        runningChild = null;
        if (child.isCancelled()) {
            setCancelled();
            return;
        }
        if (isAllChildCompleted()) {
            setSuccess();
        } else if (!isExecuting()) {
            template_execute();
        }
    }
}