package cn.wjybxx.common.btree.decorator;

import cn.wjybxx.common.btree.Decorator;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * 在子节点完成之后固定返回成功
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class AlwaysSuccess<E> extends Decorator<E> {

    public AlwaysSuccess() {
    }

    public AlwaysSuccess(Task<E> child) {
        super(child);
    }

    @Override
    protected void execute() {
        if (child == null) {
            setSuccess();
        } else {
            template_runChild(child);
        }
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        setSuccess();
    }
}