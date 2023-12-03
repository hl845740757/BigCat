package cn.wjybxx.common.btree.leaf;

import cn.wjybxx.common.btree.LeafTask;
import cn.wjybxx.common.btree.Status;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/11/26
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class Failure<E> extends LeafTask<E> {

    private int failureStatus;

    @Override
    protected void execute() {
        setFailed(Status.ToFailure(failureStatus));
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) throws Exception {

    }

    public int getFailureStatus() {
        return failureStatus;
    }

    public void setFailureStatus(int failureStatus) {
        this.failureStatus = failureStatus;
    }
}
