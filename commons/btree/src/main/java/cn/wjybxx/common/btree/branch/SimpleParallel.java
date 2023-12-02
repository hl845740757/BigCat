package cn.wjybxx.common.btree.branch;

import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

import javax.annotation.Nonnull;
import java.util.List;

/**
 * 简单并发节点。
 * 其中第一个任务为主要任务，其余任务为次要任务。
 * 一旦主要任务完成，则节点进入完成状态。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class SimpleParallel<E> extends Parallel<E> {

    @Override
    protected void execute() {
        final List<Task<E>> children = this.children;
        final Task<E> mainTask = children.get(0);

        final int reentryId = getReentryId();
        template_runChild(mainTask);
        if (checkCancel(reentryId)) { // 得出结果或取消
            return;
        }

        for (int idx = 1; idx < children.size(); idx++) {
            Task<E> child = children.get(idx);
            template_runHook(child);
            if (checkCancel(reentryId)) { // 得出结果或取消
                return;
            }
        }
        setRunning();
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        assert child == children.get(0);
        setCompleted(child.getStatus(), true);
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) throws Exception {
        children.get(0).onEvent(event);
    }
}