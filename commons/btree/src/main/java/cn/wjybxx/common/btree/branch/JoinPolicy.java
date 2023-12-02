package cn.wjybxx.common.btree.branch;

import cn.wjybxx.common.btree.Task;

/**
 * Join的完成策略
 * 1.不要在Policy上缓存Join的child。
 * 2.尽量少的缓存数据
 *
 * @author wjybxx
 * date - 2023/12/2
 */
public interface JoinPolicy<E> {

    /** 重置自身数据 */
    void resetForRestart();

    /** 启动前初始化 */
    void beforeEnter(Join<E> join);

    /**
     * Join在调用该方法前更新了完成计数和成功计数
     *
     * @param child 进入完成状态的child
     */
    void onChildCompleted(Join<E> join, Task<E> child);

    /**
     * @param event 收到的事件
     */
    void onEvent(Join<E> join, Object event) throws Exception;

}