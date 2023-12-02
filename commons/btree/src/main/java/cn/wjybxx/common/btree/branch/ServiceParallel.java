package cn.wjybxx.common.btree.branch;

import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

import javax.annotation.Nonnull;
import java.util.List;

/**
 * 服务并发节点
 * 其中第一个任务为主要任务，其余任务为后台服务。
 * 每次所有任务都会执行一次，但总是根据第一个任务执行结果返回结果。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class ServiceParallel<E> extends Parallel<E> {

    @Override
    protected void execute() {
        final List<Task<E>> children = this.children;
        final Task<E> mainTask = children.get(0);
        template_runChild(mainTask);

        for (int idx = 1; idx < children.size(); idx++) {
            Task<E> child = children.get(idx);
            template_runHook(child);
        }

        if (mainTask.isCompleted()) {
            setCompleted(mainTask.getStatus(), true);
        } else {
            setRunning();
        }
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        assert child == children.get(0);
        if (!isExecuting()) {
            setSuccess();
        }
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) throws Exception {
        children.get(0).onEvent(event);
    }
}