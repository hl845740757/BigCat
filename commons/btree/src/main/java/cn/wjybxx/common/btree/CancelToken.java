package cn.wjybxx.common.btree;

import cn.wjybxx.common.collect.SmallArrayList;

import java.util.List;
import java.util.Objects;
import java.util.function.Consumer;

/**
 * 取消令牌。
 * 1.行为树的取消必须通过共享对象（共享上下文实现）实现，理论上可存储在黑板中，但会限制黑板的扩展。
 * 2.如果想及时响应取消，可注册监听器
 *
 * @author wjybxx
 * date - 2023/11/25
 */
public final class CancelToken {

    private int cancelCode;
    private final List<ListenerWrapper> listeners = new SmallArrayList<>();

    public CancelToken() {
    }

    public CancelToken(int cancelCode) {
        if (cancelCode < 0) throw new IllegalArgumentException("code: " + cancelCode);
        this.cancelCode = cancelCode;
    }

    /** 是否已收到取消请求 */
    public boolean isCancelling() {
        return cancelCode > 0;
    }

    /** 获取取消码 */
    public int getCancelCode() {
        return cancelCode;
    }

    /** 发出取消命令 */
    public void cancel() {
        cancel(1);
    }

    /**
     * 发出取消命令
     *
     * @param cancelCode 取消码
     * @return 如果之前处于未取消状态，则返回0；否则返回之前的取消码
     */
    public int cancel(int cancelCode) {
        if (cancelCode <= 0) throw new IllegalArgumentException();
        int r = this.cancelCode;
        if (r == 0) {
            this.cancelCode = cancelCode;
            notifyListeners();
        }
        return r;
    }

    /**
     * 创建一个子token
     * 1.子token会在当前token被取消的时候取消
     * 2.如果当前token已取消，则子token也已取消
     */
    public CancelToken newChild() {
        CancelToken child = new CancelToken(cancelCode);
        if (cancelCode <= 0) {
            listeners.add(new ListenerWrapper(child));
        }
        return child;
    }

    /**
     * 清理所有的监听者（含child）
     */
    public void clear() {
        cancelCode = 0;
        listeners.clear();
    }

    /**
     * 添加取消监听器
     *
     * @return listener对应的id；返回 0 表示已通知，大于0表示注册成功；
     */
    public int addListener(Consumer<? super CancelToken> action) {
        Objects.requireNonNull(action, "action");
        if (cancelCode > 0) {
            try {
                action.accept(this);
            } catch (Exception | AssertionError e) {
                logListenerException(action, e);
            }
            return 0;
        } else {
            ListenerWrapper wrapper = new ListenerWrapper(action);
            listeners.add(wrapper);
            return wrapper.id;
        }
    }

    /** 删除监听器 */
    public boolean removeListener(Consumer<? super CancelToken> action) {
        if (action == null) return false;
        // 逆向查找更容易匹配和避免数组拷贝 -- 与Task的启动顺序和停止顺序相关
        List<ListenerWrapper> listeners = this.listeners;
        for (int idx = listeners.size() - 1; idx >= 0; idx--) {
            ListenerWrapper wrapper = listeners.get(idx);
            if (action.equals(wrapper.action)) {
                listeners.remove(idx);
                return true;
            }
        }
        return false;
    }

    /** 通过分配的监听器id删除监听器 */
    public boolean removeListener(int listenerId) {
        if (listenerId <= 0) return false;
        List<ListenerWrapper> listeners = this.listeners;
        for (int idx = listeners.size() - 1; idx >= 0; idx--) {
            ListenerWrapper wrapper = listeners.get(idx);
            if (wrapper.id == listenerId) {
                listeners.remove(idx);
                return true;
            }
        }
        return false;
    }

    // region internal

    private void notifyListeners() {
        List<ListenerWrapper> listeners = this.listeners;
        if (listeners.isEmpty()) {
            return;
        }
        for (int i = 0; i < listeners.size(); i++) {
            ListenerWrapper wrapper = listeners.get(i);
            if (wrapper.action instanceof CancelToken child) {
                child.cancel(cancelCode);
            } else {
                @SuppressWarnings("unchecked") var action = (Consumer<? super CancelToken>) wrapper.action;
                try {
                    action.accept(this);
                } catch (Exception | AssertionError e) {
                    logListenerException(action, e);
                }
            }
        }
    }

    private void logListenerException(Consumer<? super CancelToken> action, Throwable e) {
        Task.logger.warn("action caught exception, actionType: " + action.getClass(), e);
    }

    private static class ListenerWrapper {

        final int id;
        final Object action;

        private ListenerWrapper(Object action) {
            this.id = nextId();
            this.action = action;
        }

        /** 使用静态的int是安全的，同一个token上的监听器id实践中不会重复 */
        private static int idSeq = 0;

        /** 确保分配的id都大于0 */
        private static int nextId() {
            int r = ++idSeq;
            if (r < 0) { // 越界
                r = idSeq = 1;
            }
            return r;
        }
    }
    // endregion

}