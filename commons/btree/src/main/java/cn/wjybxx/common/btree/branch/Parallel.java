package cn.wjybxx.common.btree.branch;

import cn.wjybxx.common.btree.BranchTask;
import cn.wjybxx.common.btree.Task;

/**
 * 并行节点基类
 * 定义该类主要说明一些注意事项，包括：
 * 1.在处理子节点完成事件的时候，避免运行execute方法，否则可能导致其它task单帧内运行多次。
 * 2.如果有缓存数据，务必小心维护。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
public abstract class Parallel<E> extends BranchTask<E> {

    /**
     * 并发节点通常不需要在该事件中将自己更新为运行状态，而是应该在{@link #execute()}方法的末尾更新
     */
    @Override
    protected void onChildRunning(Task<E> child) {

    }

}