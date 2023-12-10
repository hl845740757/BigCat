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
    private final List<Listener> listeners = new SmallArrayList<>();

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
            listeners.add(new SubTokenListener(child));
        }
        return child;
    }

    /**
     * @return 返回 0 表示已通知，大于0表示注册成功；
     */
    public int addChild(CancelToken child) {
        Objects.requireNonNull(child, "child");
        if (child == this) throw new IllegalArgumentException();
        if (cancelCode > 0) {
            child.cancel(cancelCode);
            return 0;
        } else {
            SubTokenListener listener = new SubTokenListener(child);
            listeners.add(listener);
            return listener.id;
        }
    }

    /** 删除子token */
    public boolean removeChild(CancelToken child) {
        if (child == null) return false;
        return removeByHandle(child);
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
            Listener listener = new ActionListener(action);
            listeners.add(listener);
            return listener.id;
        }
    }

    /**
     * 删除监听器
     * 注意：lambda可能无法正确匹配，因此建议使用{@link #removeById(int)}
     */
    public boolean removeListener(Consumer<? super CancelToken> action) {
        if (action == null) return false;
        return removeByHandle(action);
    }

    /**
     * 为Task定制的接口
     * 1.可减少闭包，也更方便使用
     * 2.如果Task被重入，不会被通知
     *
     * @return listener对应的id；返回 0 表示已通知，大于0表示注册成功；
     */
    public int addListener(Task<?> task) {
        assert task.isRunning();
        Objects.requireNonNull(task, "task");
        if (cancelCode > 0) {
            try {
                task.onCancelRequested(this);
            } catch (Exception | AssertionError e) {
                logListenerException(task, e);
            }
            return 0;
        } else {
            Listener wrapper = new TaskListener(task);
            listeners.add(wrapper);
            return wrapper.id;
        }
    }

    /** 删除task的监听 */
    public boolean removeListener(Task<?> task) {
        if (task == null) return false;
        // task如果被重入，那么前一次注册的监听应当在exit的时候已删除，这里如果出现多个匹配的节点，则证明已出现了错误
        return removeByHandle(task);
    }

    /** 通过分配的监听器id删除监听器 */
    public boolean removeById(int listenerId) {
        if (listenerId <= 0) return false;
        List<Listener> listeners = this.listeners;
        for (int idx = listeners.size() - 1; idx >= 0; idx--) {
            Listener wrapper = listeners.get(idx);
            if (wrapper.id == listenerId) {
                listeners.remove(idx);
                return true;
            }
        }
        return false;
    }

    private boolean removeByHandle(Object handle) {
        // 逆向查找更容易匹配和避免数组拷贝 -- 与Task的启动顺序和停止顺序相关
        List<Listener> listeners = this.listeners;
        for (int idx = listeners.size() - 1; idx >= 0; idx--) {
            Listener wrapper = listeners.get(idx);
            if (handle.equals(wrapper.getHandle())) {
                listeners.remove(idx);
                return true;
            }
        }
        return false;
    }

    // region internal

    private void notifyListeners() {
        List<Listener> listeners = this.listeners;
        if (listeners.isEmpty()) {
            return;
        }
        for (int i = 0; i < listeners.size(); i++) {
            Listener wrapper = listeners.get(i);
            try {
                wrapper.fire(this);
            } catch (Exception | AssertionError e) {
                logListenerException(wrapper.getHandle(), e);
            }
        }
        listeners.clear();
    }

    private void logListenerException(Object action, Throwable e) {
        Task.logger.warn("action caught exception, actionType: " + action.getClass(), e);
    }


    private static abstract class Listener {

        final int id;

        private Listener() {
            this.id = nextId();
        }

        abstract void fire(CancelToken token) throws Exception;

        abstract Object getHandle();

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

    private static class ActionListener extends Listener {

        final Consumer<? super CancelToken> action;

        private ActionListener(Consumer<? super CancelToken> action) {
            this.action = action;
        }

        @Override
        void fire(CancelToken token) throws Exception {
            action.accept(token);
        }

        @Override
        public Object getHandle() {
            return action;
        }
    }

    private static class SubTokenListener extends Listener {

        final CancelToken cancelToken;

        private SubTokenListener(CancelToken cancelToken) {
            this.cancelToken = cancelToken;
        }

        @Override
        void fire(CancelToken token) throws Exception {
            cancelToken.cancel(token.cancelCode);
        }

        @Override
        Object getHandle() {
            return cancelToken;
        }
    }

    private static class TaskListener extends Listener {

        final Task<?> task;
        final int rid;

        public TaskListener(Task<?> task) {
            this.task = task;
            this.rid = task.getReentryId();
        }

        @Override
        void fire(CancelToken token) throws Exception {
            if (!task.isExited(rid)) {
                task.onCancelRequested(token);
            }
        }

        @Override
        Object getHandle() {
            return task;
        }
    }

    // endregion

}