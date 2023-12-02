package cn.wjybxx.common.btree.decorator;

import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * 循环子节点直到给定的条件达成
 *
 * @author wjybxx
 * date - 2023/12/1
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class UntilCond<E> extends LoopDecorator<E> {

    /** 循环条件 -- 不能直接使用child的guard，意义不同 */
    private Task<E> cond;

    @Override
    protected void onChildCompleted(Task<E> child) {
        if (template_checkGuard(cond)) {
            setSuccess();
        }
    }

    public Task<E> getCond() {
        return cond;
    }

    public UntilCond<E> setCond(Task<E> cond) {
        this.cond = cond;
        return this;
    }

}
