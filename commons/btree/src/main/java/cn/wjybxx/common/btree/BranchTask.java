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
package cn.wjybxx.common.btree;

import cn.wjybxx.common.CollectionUtils;
import cn.wjybxx.common.annotation.VisibleForTesting;

import javax.annotation.Nullable;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.Objects;
import java.util.stream.Stream;

/**
 * 分支任务（可能有多个子节点）
 *
 * @author wjybxx
 * date - 2023/11/25
 */
public abstract class BranchTask<E> extends Task<E> {

    protected List<Task<E>> children;

    public BranchTask() {
        this.children = new ArrayList<>(4);
    }

    public BranchTask(List<Task<E>> children) {
        this.children = Objects.requireNonNull(children);
    }

    public BranchTask(Task<E> first, @Nullable Task<E> second) {
        Objects.requireNonNull(first);
        this.children = new ArrayList<>(2);
        this.children.add(first);
        if (second != null) {
            this.children.add(second);
        }
    }

    // region

    public final boolean isFirstChild(Task<?> child) {
        if (this.children.isEmpty()) {
            return false;
        }
        return this.children.get(0) == child;
    }

    public final boolean isLastChild(Task<?> child) {
        if (children.isEmpty()) {
            return false;
        }
        return children.getLast() == child;
    }

    /** 主要为MainPolicy提供帮助 */
    @Nullable
    public final Task<E> getFirstChild() {
        final int size = children.size();
        if (size > 0) {
            return children.get(0);
        }
        return null;
    }

    @Nullable
    public final Task<E> getLastChild() {
        final int size = children.size();
        if (size > 0) {
            return children.get(size - 1);
        }
        return null;
    }

    public boolean isAllChildCompleted() {
        // 在判断是否全部完成这件事上，逆序遍历有优势
        for (int i = 0, size = children.size(); i < size; i++) {
            Task<?> child = children.get(i);
            if (child.isRunning()) {
                return false;
            }
        }
        return true;
    }
    // endregion

    // region child

    /** 用于避免测试的子节点过于规律 */
    @VisibleForTesting
    public final void shuffleChild() {
        Collections.shuffle(children);
    }

    @Override
    public final void removeAllChild() {
        children.forEach(Task::unsetControl);
        children.clear();
    }

    @Override
    public final int indexChild(Task<?> task) {
        return CollectionUtils.indexOfRef(children, task, 0);
    }

    @Override
    public final Stream<Task<E>> childStream() {
        return children.stream();
    }

    @Override
    public final int getChildCount() {
        return children.size();
    }

    @Override
    public final Task<E> getChild(int index) {
        return children.get(index);
    }

    @Override
    protected final int addChildImpl(Task<E> task) {
        children.add(task);
        return children.size() - 1;
    }

    @Override
    protected final Task<E> setChildImpl(int index, Task<E> task) {
        return children.set(index, task);
    }

    @Override
    protected final Task<E> removeChildImpl(int index) {
        return children.remove(index);
    }

    // endregion

    public List<Task<E>> getChildren() {
        return children;
    }

    public void setChildren(List<Task<E>> children) {
        if (children == null) {
            this.children.clear();
        } else {
            this.children = children;
        }
    }
}
