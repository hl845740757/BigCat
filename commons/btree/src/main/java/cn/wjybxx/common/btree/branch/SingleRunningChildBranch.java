package cn.wjybxx.common.btree.branch;

import cn.wjybxx.common.btree.BranchTask;
import cn.wjybxx.common.btree.Task;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.List;

/**
 * 非并行分支节点抽象
 * 如果{@link #execute()}方法是有循环体的，那么一定要注意：
 * 只有循环的尾部运行child才是安全的，如果在运行child后还读写其它数据，可能导致bug(小心递归)。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
public abstract class SingleRunningChildBranch<E> extends BranchTask<E> {

    /** 运行中的子节点 */
    protected transient Task<E> runningChild = null;
    /** 运行中的子节点索引 */
    protected transient int runningIndex = -1;

    public SingleRunningChildBranch() {
    }

    public SingleRunningChildBranch(List<Task<E>> children) {
        super(children);
    }

    public SingleRunningChildBranch(Task<E> first, @Nullable Task<E> second) {
        super(first, second);
    }

    /** 允许外部在结束后查询 */
    public final int getRunningIndex() {
        return runningIndex;
    }

    @Override
    public boolean isAllChildCompleted() {
        return runningIndex + 1 >= children.size();
    }

    @Override
    public void resetForRestart() {
        super.resetForRestart();
        runningChild = null;
        runningIndex = -1;
    }

    @Override
    protected void beforeEnter() {
        // 这里不调用super是安全的
        runningChild = null;
        runningIndex = -1;
    }

    @Override
    protected void exit() {
        // index不立即重置，允许返回后查询
        runningChild = null;
    }

    @Override
    protected void stopRunningChildren() {
        Task.stop(runningChild);
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) throws Exception {
        if (runningChild != null) {
            runningChild.onEvent(event);
        }
    }

    @Override
    protected void execute() {
        final int reentryId = getReentryId();
        Task<E> runningChild = this.runningChild;
        for (int i = 0, retryCount = children.size(); i < retryCount; i++) { // 避免死循环
            if (runningChild == null) {
                this.runningChild = runningChild = nextChild();
            }
            template_runChild(runningChild);
            if (checkCancel(reentryId)) { // 得出结果或被取消
                return;
            }
            if (runningChild.isRunning()) { // 子节点未结束
                return;
            }
            runningChild = null;
        }
        throw new IllegalStateException(illegalStateMsg());
    }

    protected Task<E> nextChild() {
        // 避免状态错误的情况下修改了index
        int nextIndex = runningIndex + 1;
        if (nextIndex < children.size()) {
            runningIndex = nextIndex;
            return children.get(nextIndex);
        }
        throw new IllegalStateException(illegalStateMsg());
    }

    protected final String illegalStateMsg() {
        return "numChildren: %d, currentIndex: %d".formatted(children.size(), runningIndex);
    }

    @Override
    protected void onChildRunning(Task<E> child) {
        if (runningChild == null) { // 部分实现可能未在选择child之后就赋值
            runningChild = child;
        }
        setRunning();
    }

    /**
     * 子类的实现模板：
     * <pre>{@code
     *
     *  protected void onChildCompleted(Task child) {
     *      runningChild = null;
     *      // 尝试计算结果
     *      ...
     *      // 如果未得出结果
     *      if (!isExecuting()) {
     *          template_execute();
     *      }
     *  }
     *
     * }</pre>
     */
    @Override
    protected void onChildCompleted(Task<E> child) {
        assert child == runningChild;
        runningChild = null; // 子类可直接重写此句以不调用super
    }

}