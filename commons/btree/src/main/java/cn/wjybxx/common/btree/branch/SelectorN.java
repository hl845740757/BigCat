package cn.wjybxx.common.btree.branch;

import cn.wjybxx.common.btree.Status;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.btree.leaf.Success;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

import javax.annotation.Nullable;
import java.util.List;

/**
 * 多选Selector。
 * 如果{@link #required}小于等于0，则等同于{@link Success}
 * 如果{@link #required}等于1，则等同于{@link Selector}；
 * 如果{@link #required}等于{@code children.size}，则在所有child成功之后成功 -- 不会提前失败。
 * 如果{@link #required}大于{@code children.size}，则在所有child运行完成之后失败 -- 不会提前失败。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class SelectorN<E> extends SingleRunningChildBranch<E> {

    private int required = 1;
    private transient int count;

    public SelectorN() {
    }

    public SelectorN(List<Task<E>> children) {
        super(children);
    }

    public SelectorN(Task<E> first, @Nullable Task<E> second) {
        super(first, second);
    }

    @Override
    public void resetForRestart() {
        super.resetForRestart();
        count = 0;
    }

    @Override
    protected void beforeEnter() {
        super.beforeEnter();
        count = 0;
    }

    @Override
    protected void enter(int reentryId) {
        super.enter(reentryId);
        if (required < 1) {
            setSuccess();
        }
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        runningChild = null;
        if (child.isCancelled()) {
            setCancelled();
            return;
        }
        if (child.isSucceeded() && ++count >= required) {
            setSuccess();
        } else if (isAllChildCompleted()) {
            setFailed(Status.ERROR);
        } else if (!isExecuting()) {
            template_execute();
        }
    }

    public int getRequired() {
        return required;
    }

    public void setRequired(int required) {
        this.required = required;
    }
}
