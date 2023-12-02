package cn.wjybxx.common.btree.branch.join;

import cn.wjybxx.common.btree.Status;
import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.btree.branch.Join;
import cn.wjybxx.common.btree.branch.JoinPolicy;
import cn.wjybxx.common.btree.branch.SelectorN;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

/**
 * {@link SelectorN}
 *
 * @author wjybxx
 * date - 2023/12/2
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class JoinSelectorN<E> implements JoinPolicy<E> {

    private int required = 1;

    @Override
    public void resetForRestart() {

    }

    @Override
    public void beforeEnter(Join<E> join) {

    }

    @Override
    public void onChildCompleted(Join<E> join, Task<E> child) {
        if (join.getSucceededCount() >= required) {
            join.setSuccess();
            return;
        }
        if (join.isAllChildCompleted()) {
            join.setFailed(Status.ERROR);
        }
    }

    @Override
    public void onEvent(Join<E> join, Object event) {

    }

    public int getRequired() {
        return required;
    }

    public void setRequired(int required) {
        this.required = required;
    }

}