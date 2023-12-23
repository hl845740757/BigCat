/*
 * Copyright 2023 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
package cn.wjybxx.common.btree.branch;

import cn.wjybxx.common.btree.Task;
import cn.wjybxx.common.btree.branch.join.JoinSequence;
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
@BinarySerializable
@DocumentSerializable
public class Join<E> extends Parallel<E> {

    protected JoinPolicy<E> policy;

    /** 子节点的重入id -- 判断本轮是否需要执行 */
    protected transient int[] childPrevReentryIds;
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
        policy.enter(this);
    }

    private void recordContext() {
        List<Task<E>> children = this.children;
        if (childPrevReentryIds == null || childPrevReentryIds.length != children.size()) {
            childPrevReentryIds = new int[children.size()];
        }
        for (int i = 0; i < children.size(); i++) {
            Task<E> child = children.get(i);
            child.setCancelToken(cancelToken.newChild()); // child默认可读取取消
            childPrevReentryIds[i] = child.getReentryId();
        }
    }

    @Override
    protected void execute() {
        final List<Task<E>> children = this.children;
        if (children.isEmpty()) {
            return;
        }
        final int[] childPrevReentryIds = this.childPrevReentryIds;
        final int reentryId = getReentryId();
        for (int i = 0; i < children.size(); i++) {
            final Task<E> child = children.get(i);
            final boolean started = child.isExited(childPrevReentryIds[i]);
            if (started && child.isCompleted()) { // 勿轻易调整
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
        cancelToken.removeChild(child.getCancelToken()); // 删除分配的token
        child.getCancelToken().clear();
        child.setCancelToken(null);

        policy.onChildCompleted(this, child);
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) {
        policy.onEvent(this, event);
    }

    // region
    @Override
    public boolean isAllChildCompleted() {
        return completedCount >= children.size();
    }

    public boolean isAllChildSucceeded() {
        return succeededCount >= children.size();
    }

    public int getCompletedCount() {
        return completedCount;
    }

    public int getSucceededCount() {
        return succeededCount;
    }
    // endregion

    public JoinPolicy<E> getPolicy() {
        return policy;
    }

    public void setPolicy(JoinPolicy<E> policy) {
        this.policy = policy;
    }

}