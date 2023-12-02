package cn.wjybxx.common.btree;

import javax.annotation.Nonnull;
import java.util.stream.Stream;

/**
 * 装饰任务（最多只有一个子节点）
 *
 * @author wjybxx
 * date - 2023/11/25
 */
public abstract class Decorator<E> extends Task<E> {

    protected Task<E> child;

    public Decorator() {
    }

    public Decorator(Task<E> child) {
        this.child = child;
    }

    @Override
    protected void stopRunningChildren() {
        Task.stop(child);
    }

    @Override
    protected void onChildRunning(Task<E> child) {
        setRunning();
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) throws Exception {
        if (child != null) {
            child.onEvent(event);
        }
    }

    // region child

    @Override
    public final int indexChild(Task<?> task) {
        if (task != null && task == this.child) {
            return 0;
        }
        return -1;
    }

    @Override
    public final Stream<Task<E>> childStream() {
        return Stream.ofNullable(child);
    }

    @Override
    public final int getChildCount() {
        return child == null ? 0 : 1;
    }

    @Override
    public final Task<E> getChild(int index) {
        if (index == 0 && child != null) {
            return child;
        }
        throw new IndexOutOfBoundsException(index);
    }

    @Override
    protected final int addChildImpl(Task<E> task) {
        if (child != null) {
            throw new IllegalStateException("A task entry cannot have more than one child");
        }
        child = task;
        return 0;
    }

    @Override
    protected final Task<E> setChildImpl(int index, Task<E> task) {
        if (index == 0 && child != null) {
            Task<E> r = this.child;
            child = task;
            return r;
        }
        throw new IndexOutOfBoundsException(index);
    }

    @Override
    protected final Task<E> removeChildImpl(int index) {
        if (index == 0 && child != null) {
            Task<E> r = this.child;
            child = null;
            return r;
        }
        throw new IndexOutOfBoundsException(index);
    }
    // endregion

    //region 序列化

    public Task<E> getChild() {
        return child;
    }

    public void setChild(Task<E> child) {
        this.child = child;
    }

    // endregion
}
