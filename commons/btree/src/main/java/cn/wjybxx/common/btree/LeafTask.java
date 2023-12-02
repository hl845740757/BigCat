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