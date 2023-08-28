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

package cn.wjybxx.common.concurrent;

import cn.wjybxx.common.ThreadUtils;

import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;
import java.util.concurrent.Callable;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.RunnableFuture;
import java.util.concurrent.locks.LockSupport;

/**
 * {@link CompletableFuture}并不支持{@code uncancellable}，我们只能通过一个额外的状态字段来实现。
 * 理想的情况下，取消应该是异步的，即future只向关联的任务发起一个取消通知，并不保证立即取消，但JDK的接口是同步的。
 * {@link FutureContext}可以实现多任务的取消。
 *
 * @author wjybxx
 * date 2023/4/7
 */
class XFutureTask<V> extends XCompletableFuture<V> implements RunnableFuture<V> {

    private static final int STATE_NEW = 0;
    private static final int STATE_UNCANCELLABLE = 1;
    private static final int STATE_WAIT_DONE = 2;

    private Callable<V> task;
    @SuppressWarnings("unused")
    private volatile int state;

    XFutureTask(FutureContext ctx, Callable<V> task) {
        super(ctx);
        this.task = task;
    }

    XFutureTask(FutureContext ctx, Runnable task, V result) {
        super(ctx);
        this.task = FutureUtils.toCallable(task, result);
    }

    @Override
    public void run() {
        try {
            if (internal_setUncancellable()) {
                V result = task.call();
                internal_doComplete(result);
            }
        } catch (Throwable ex) {
            ThreadUtils.recoveryInterrupted(ex);
            internal_doCompleteExceptionally(ex);
        }
    }

    final Callable<V> getTask() {
        return task;
    }

    private void clean() {
        task = null;
    }

    //

    /** 任务的执行者调用 */
    private boolean cas2WaitDone() {
        // 我们假设 UNCANCELLABLE 的概率更大
        int preValue = (Integer) STATE.compareAndExchange(this, STATE_UNCANCELLABLE, STATE_WAIT_DONE);
        if (preValue == STATE_UNCANCELLABLE) {
            return true;
        }
        if (preValue == STATE_NEW) {
            return STATE.compareAndSet(this, STATE_NEW, STATE_WAIT_DONE);
        }
        return false;
    }

    protected final boolean internal_setUncancellable() {
        return STATE.compareAndSet(this, STATE_NEW, STATE_UNCANCELLABLE);
    }

    @Override
    protected boolean internal_doComplete(V value) {
        clean();
        // 需要竞争更新权限，因为周期性任务的成功和外部取消是存在竞争的
        if (cas2WaitDone()) {
            return super.internal_doComplete(value);
        } else {
            return false;
        }
    }

    @Override
    protected boolean internal_doCompleteExceptionally(Throwable ex) {
        clean();
        // 需要竞争更新权限，因为周期性任务的失败和外部取消是存在竞争的
        if (cas2WaitDone()) {
            return super.internal_doCompleteExceptionally(ex);
        } else {
            return false;
        }
    }

    @Override
    public boolean cancel(boolean mayInterruptIfRunning) {
        if (STATE.compareAndSet(this, STATE_NEW, STATE_WAIT_DONE)) {
            return super.internal_doCancel(mayInterruptIfRunning);
        }
        return false;
    }

    @Override
    public final boolean complete(V value) {
        return false;
    }

    @Override
    public final boolean completeExceptionally(Throwable ex) {
        return false;
    }

    @Override
    public final void obtrudeValue(V value) {
        throw new UnsupportedOperationException();
    }

    @Override
    public final void obtrudeException(Throwable ex) {
        throw new UnsupportedOperationException();
    }

    //
    @Override
    public final int hashCode() {
        return System.identityHashCode(this);
    }

    @Override
    public final boolean equals(Object obj) {
        return this == obj;
    }

    // VarHandle mechanics
    private static final VarHandle STATE;

    static {
        try {
            MethodHandles.Lookup l = MethodHandles.lookup();
            STATE = l.findVarHandle(XFutureTask.class, "state", int.class);
        } catch (ReflectiveOperationException e) {
            throw new ExceptionInInitializerError(e);
        }

        // Reduce the risk of rare disastrous classloading in first call to
        // LockSupport.park: https://bugs.openjdk.java.net/browse/JDK-8074773
        Class<?> ensureLoaded = LockSupport.class;
    }

}