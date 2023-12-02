package cn.wjybxx.common.btree.decorator;

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
public class UntilSuccess<E> extends LoopDecorator<E> {

    public UntilSuccess() {
    }

    public UntilSuccess(int maxLoopTimesPerFrame) {
        super(maxLoopTimesPerFrame);
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        if (child.isCancelled()) {
            setCancelled();
            return;
        }
        if (child.isFailed()) {
            setSuccess();
        }
    }
}