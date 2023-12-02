package cn.wjybxx.common.btree.branch;

import cn.wjybxx.common.btree.Status;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * @author wjybxx
 * date - 2023/11/26
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class Switch<E> extends SingleRunningChildBranch<E> {

    @Override
    protected void execute() {
        if (runningChild == null && !selectChild()) {
            setFailed(Status.ERROR);
            return;
        }
        template_runChildDirectly(runningChild);
    }

    private boolean selectChild() {
        for (int idx = 0; idx < children.size(); idx++) {
            Task<E> child = children.get(idx);
            if (!template_checkGuard(child.getGuard())) {
                child.setGuardFailed(null); // 不接收通知
                continue;
            }
            this.runningChild = child;
            this.runningIndex = idx;
            return true;
        }
        return false;
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        runningChild = null;
        setCompleted(child.getStatus(), true);
    }
}