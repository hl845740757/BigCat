package cn.wjybxx.common.btree;

/**
 * @author wjybxx
 * date - 2023/12/3
 */
class BtreeTestUtil {

    public static TaskEntry<Blackboard> newTaskEntry() {
        return new TaskEntry<>("Main", null, new Blackboard(), null, TreeLoader.nullLoader());
    }

    public static void untilCompleted(TaskEntry<?> entry) {
        for (int idx = 0; idx < 100; idx++) { // 避免死循环
            entry.update(idx);
            if (entry.isCompleted()) return;
        }
        throw new IllegalStateException();
    }

    public static int completedCount(Task<?> ctrl) {
        return (int) ctrl.childStream()
                .filter(Task::isCompleted)
                .count();
    }

    public static int succeededCount(Task<?> ctrl) {
        return (int) ctrl.childStream()
                .filter(Task::isSucceeded)
                .count();
    }

    public static int failedCount(Task<?> ctrl) {
        return (int) ctrl.childStream()
                .filter(Task::isFailed)
                .count();
    }

}