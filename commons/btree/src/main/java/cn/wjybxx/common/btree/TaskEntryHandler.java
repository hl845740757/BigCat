package cn.wjybxx.common.btree;

/**
 * 用于处理Entry的完成事件
 *
 * @author wjybxx
 * date - 2023/12/2
 */
public interface TaskEntryHandler<E> {

    void onEntryCompleted(TaskEntry<E> taskEntry);

}