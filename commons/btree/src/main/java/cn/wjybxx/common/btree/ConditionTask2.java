package cn.wjybxx.common.btree;

import javax.annotation.Nonnull;

/**
 * 可返回详细错误码的条件节点
 *
 * @author wjybxx
 * date - 2023/11/25
 */
public abstract class ConditionTask2<E> extends LeafTask<E> {

    @Override
    protected final void execute() {
        int status = test();
        if (status == Status.SUCCESS) {
            setSuccess();
        } else {
            setFailed(status);
        }
    }

    protected abstract int test();

    @Override
    public boolean canHandleEvent(@Nonnull Object event) {
        return false;
    }

    /** 条件节点正常情况下不会触发事件 */
    @Override
    protected void onEventImpl(@Nonnull Object event) throws Exception {

    }

}