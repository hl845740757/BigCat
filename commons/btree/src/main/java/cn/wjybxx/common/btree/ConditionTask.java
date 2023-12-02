package cn.wjybxx.common.btree;

import javax.annotation.Nonnull;

/**
 * 条件节点
 * (并非所有条件节点都需要继承该类)
 *
 * @author wjybxx
 * date - 2023/11/25
 */
public abstract class ConditionTask<E> extends LeafTask<E> {

    @Override
    protected final void execute() {
        if (test()) {
            setSuccess();
        } else {
            setFailed(Status.ERROR);
        }
    }

    protected abstract boolean test();

    @Override
    public boolean canHandleEvent(@Nonnull Object event) {
        return false;
    }

    /** 条件节点正常情况下不会触发事件 */
    @Override
    protected void onEventImpl(@Nonnull Object event) throws Exception {

    }

}
