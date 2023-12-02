package cn.wjybxx.common.btree.decorator;

import cn.wjybxx.common.btree.Decorator;
import cn.wjybxx.common.btree.Status;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * 每一帧都检查子节点的前置条件，如果前置条件失败，则取消child执行并返回失败
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class AlwaysCheckGuard<E> extends Decorator<E> {

    @Override
    protected void execute() {
        if (template_checkGuard(child.getGuard())) {
            template_runChildDirectly(child);
        } else {
            child.stop();
            setFailed(Status.ERROR);
        }
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        setCompleted(child.getStatus(), true);
    }
}