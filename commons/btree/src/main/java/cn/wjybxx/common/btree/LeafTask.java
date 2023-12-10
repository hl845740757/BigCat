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

import java.util.stream.Stream;

/**
 * 叶子任务（不能有子节点）
 *
 * @author wjybxx
 * date - 2023/11/25
 */
public abstract class LeafTask<E> extends Task<E> {

    @Override
    protected final void onChildRunning(Task<E> child) {
        throw new AssertionError();
    }

    @Override
    protected final void onChildCompleted(Task<E> child) {
        throw new AssertionError();
    }

    // region child

    @Override
    public final int indexChild(Task<?> task) {
        return -1;
    }

    @Override
    public final Stream<Task<E>> childStream() {
        return Stream.empty();
    }

    @Override
    public final int getChildCount() {
        return 0;
    }

    @Override
    public final Task<E> getChild(int index) {
        throw new IndexOutOfBoundsException("A leaf task can not have any child");
    }

    @Override
    protected final int addChildImpl(Task<E> task) {
        throw new IllegalStateException("A leaf task cannot have any children");
    }

    @Override
    protected final Task<E> setChildImpl(int index, Task<E> task) {
        throw new IllegalStateException("A leaf task cannot have any children");
    }

    @Override
    protected final Task<E> removeChildImpl(int index) {
        throw new IndexOutOfBoundsException("A leaf task can not have any child");
    }

    // endregion
}