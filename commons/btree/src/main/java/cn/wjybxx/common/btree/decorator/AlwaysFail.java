package cn.wjybxx.common.btree.decorator;

import cn.wjybxx.common.btree.Decorator;
import cn.wjybxx.common.btree.Status;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * 在子节点完成之后固定返回失败
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class AlwaysFail<E> extends Decorator<E> {

    private int failureStatus;

    public AlwaysFail() {
    }

    public AlwaysFail(Task<E> child) {
        super(child);
    }

    @Override
    protected void execute() {
        if (child == null) {
            setFailed(Status.ToFailure(failureStatus));
        } else {
            template_runChild(child);
        }
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        setCompleted(Status.ToFailure(child.getStatus()), true); // 错误码有传播的价值
    }

    public int getFailureStatus() {
        return failureStatus;
    }

    public void setFailureStatus(int failureStatus) {
        this.failureStatus = failureStatus;
    }
}