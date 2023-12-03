package cn.wjybxx.common.btree.decorator;

import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * 重复运行子节点，直到该任务失败
 * （超类做了死循环避免）
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class UntilFail<E> extends LoopDecorator<E> {

    public UntilFail() {
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