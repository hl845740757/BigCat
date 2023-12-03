package cn.wjybxx.common.btree.branch;

import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.btree.branch.join.JoinSequence;
import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.codec.document.DocumentSerializable;

import javax.annotation.Nonnull;
import java.util.List;

/**
 * Join
 * 1.在得出结果之前不会重复执行已完成的任务。
 * 2.默认为子节点分配独立的取消令牌
 *
 * @author wjybxx
 * date - 2023/12/2
 */
@AutoSchema
@BinarySerializable
@DocumentSerializable
public class Join<E> extends Parallel<E> {

    private JoinPolicy<E> policy;

    /** 子节点的重入id -- 判断本轮是否需要执行 */
    protected transient int[] childReentryIds;
    /** 已进入完成状态的子节点 */
    protected transient int completedCount;
    /** 成功完成的子节点 */
    protected transient int succeededCount;

    @Override
    public void resetForRestart() {
        super.resetForRestart();
        completedCount = 0;
        succeededCount = 0;
        policy.resetForRestart();
    }

    @Override
    protected void beforeEnter() {
        if (policy == null) {
            policy = JoinSequence.getInstance();
        }
        completedCount = 0;
        succeededCount = 0;
        // policy的数据重置
        policy.beforeEnter(this);
    }

    @Override
    protected void enter(int reentryId) {
        // 记录子类上下文 -- 由于beforeEnter可能改变子节点信息，因此在enter时处理
        recordContext();
    }

    private void recordContext() {
        List<Task<E>> children = this.children;
        if (childReentryIds == null || childReentryIds.length != children.size()) {
            childReentryIds = new int[children.size()];
        }
        for (int i = 0; i < children.size(); i++) {
            Task<E> child = children.get(i);
            child.setCancelToken(cancelToken.newChild()); // child默认可读取取消
            childReentryIds[i] = child.getReentryId();
        }
    }

    @Override
    protected void execute() {
        final List<Task<E>> children = this.children;
        if (children.isEmpty()) { // 放在这里更利于子类重写该类
            setSuccess();
            return;
        }
        final int[] childReentryIds = this.childReentryIds;
        final int reentryId = getReentryId();

        for (int i = 0; i < children.size(); i++) {
            Task<E> child = children.get(i);
            if (child.isExited(childReentryIds[i])) {
                continue;
            }
            template_runChild(child);
            if (checkCancel(reentryId)) {
                return;
            }
        }

        if (completedCount >= children.size()) { // child全部执行，但没得出结果
            throw new IllegalStateException();
        }
    }

    @Override
    protected void onChildCompleted(Task<E> child) {
        completedCount++;
        if (child.isSucceeded()) {
            succeededCount++;
        }
        policy.onChildCompleted(this, child);
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) throws Exception {
        policy.onEvent(this, event);
    }

    @Override
    public boolean isAllChildCompleted() {
        return completedCount >= children.size();
    }

    public int getCompletedCount() {
        return completedCount;
    }

    public int getSucceededCount() {
        return succeededCount;
    }

    public JoinPolicy<E> getPolicy() {
        return policy;
    }

    public void setPolicy(JoinPolicy<E> policy) {
        this.policy = policy;
    }

}