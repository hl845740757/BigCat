package cn.wjybxx.common.btree.branch;

import cn.wjybxx.common.btree.Status;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * 主动选择节点
 * 每次运行时都会重新测试节点的运行条件，选择一个新的可运行节点。
 * 如果新选择的运行节点与之前的运行节点不同，则取消之前的任务。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class ActiveSelector<E> extends SingleRunningChildBranch<E> {

    @Override
    protected void execute() {
        Task<E> childToRun = null;
        int childIndex = -1;
        for (int idx = 0; idx < children.size(); idx++) {
            Task<E> child = children.get(idx);
            if (!template_checkGuard(child.getGuard())) {
                child.setGuardFailed(null); // 不接收通知
                continue;
            }
            childToRun = child;
            childIndex = idx;
            break;
        }

        Task<E> runningChild = this.runningChild;
        if (runningChild != null && runningChild != childToRun) {
            runningChild.stop();
            this.runningChild = null;
            this.runningIndex = -1;
        }

        if (childToRun == null) {
            setFailed(Status.ERROR);
            return;
        }

        this.runningChild = childToRun;
        this.runningIndex = childIndex;
        template_runChildDirectly(childToRun);
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        runningChild = null;
        setCompleted(child.getStatus(), true);
    }
}