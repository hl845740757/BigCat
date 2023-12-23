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

package cn.wjybxx.common.async;

import cn.wjybxx.common.concurrent.StacklessCancellationException;
import cn.wjybxx.common.ex.NoLogRequiredException;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import java.util.Objects;
import java.util.concurrent.CancellationException;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.CompletionException;
import java.util.function.BiConsumer;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public abstract class AbstractPromise<V> implements FluentPromise<V> {

    private static final Logger logger = LoggerFactory.getLogger(AbstractPromise.class);

    /** 方便测试 */
    public static final String propKey = "cn.wjybxx.common.async.promise.asjdkorder";
    /** 是否按照{@link CompletableFuture}的广播顺序进行广播 -- 先插入的后广播 */
    private static final boolean asJdkOrder = Boolean.parseBoolean(System.getProperty(propKey, "false"));

    /**
     * 当任务正常完成没有结果时，使用该对象表示
     */
    private static final Object NIL = new Object();

    /**
     * 该值存储{@code Future}的结果，也表示着{@code Future}的状态。
     * <ul>
     * <li>null表示初始状态，非null表示完成状态</li>
     * <li>{@link AltResult}表示任务异常完成</li>
     * <li>{@link #NIL}表示任务正常完成，但结果为null</li>
     * <li>其它非null表示任务正常完成，且结果不为null</li>
     * </ul>
     */
    Object result;

    /**
     * 监听器栈顶
     */
    private Completion stack;

    /**
     * 异常完成结果
     */
    static class AltResult {

        final Throwable cause;

        AltResult(Throwable cause) {
            this.cause = cause;
        }
    }

    // 一定不可外部直接调用
    private boolean internalComplete(Object r) {
        if (result == null) {
            result = r;
            return true;
        } else {
            return false;
        }
    }

    static Object encodeValue(Object value) {
        return value == null ? NIL : value;
    }

    @SuppressWarnings("unchecked")
    final V decodeValue(Object r) {
        return r == NIL ? null : (V) r;
    }

    @Override
    public boolean complete(V result) {
        if (internalComplete(encodeValue(result))) {
            postComplete(this);
            return true;
        }
        return false;
    }

    @Override
    public void setComplete(V result) {
        if (internalComplete(encodeValue(result))) {
            postComplete(this);
            return;
        }
        throw new IllegalStateException("Already complete");
    }

    @Override
    public boolean completeExceptionally(Throwable cause, boolean logCause) {
        Objects.requireNonNull(cause, "cause");
        if (internalComplete(new AltResult(cause))) {
            if (logCause) {
                logCause(cause);
            }
            postComplete(this);
            return true;
        }
        return false;
    }

    @Override
    public void setCompleteExceptionally(Throwable cause, boolean logCause) {
        Objects.requireNonNull(cause, "cause");
        if (internalComplete(new AltResult(cause))) {
            if (logCause) {
                logCause(cause);
            }
            postComplete(this);
            return;
        }
        throw new IllegalStateException("Already complete");
    }

    @Override
    public boolean isDone() {
        return result != null;
    }

    @Override
    public boolean isSucceeded() {
        final Object r = this.result;
        return r != null && !(r instanceof AltResult);
    }

    @Override
    public boolean isFailed() {
        return result instanceof AltResult;
    }

    @Override
    public V getNow() {
        return getNow(null);
    }

    @Override
    public V getNow(V valueIfAbsent) {
        final Object r = this.result;
        if (r == null) {
            return valueIfAbsent;
        }
        if (r instanceof AltResult) {
            return rethrowGetNow(((AltResult) r).cause);
        }
        return decodeValue(r);
    }

    private static <T> T rethrowGetNow(Throwable cause) {
        if (cause instanceof CancellationException) {
            throw (CancellationException) cause;
        }
        if (cause instanceof CompletionException) {
            throw (CompletionException) cause;
        }
        throw new CompletionException(cause);
    }

    @Override
    public Throwable cause() {
        final Object r = this.result;
        return r instanceof AltResult ? ((AltResult) r).cause : null;
    }

    @Override
    public boolean acceptNow(@Nonnull BiConsumer<? super V, ? super Throwable> action) {
        final Object r = this.result;
        if (r == null) {
            return false;
        }

        if (r == NIL) {
            action.accept(null, null);
            return true;
        }

        if (r instanceof AltResult) {
            action.accept(null, ((AltResult) r).cause);
        } else {
            @SuppressWarnings("unchecked") final V value = (V) r;
            action.accept(value, null);
        }
        return true;
    }

    @Override
    public boolean cancel() {
        final boolean cancelled = (result == null) &&
                completeExceptionally(StacklessCancellationException.INSTANCE, false);
        return cancelled || isCancelled();
    }

    @Override
    public boolean isCancelled() {
        final Object r = this.result;
        if (r instanceof AltResult) {
            return ((AltResult) r).cause instanceof CancellationException;
        }
        return false;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////

    /**
     * 推送future进入完成事件
     */
    static void postComplete(AbstractPromise<?> future) {
        Completion next = null;
        outer:
        while (true) {
            // 将当前future上的监听器添加到next前面
            next = asJdkOrder ? future.clearListenersAsJdkOrder(next) : future.clearListeners(next);

            while (next != null) {
                Completion curr = next;
                next = next.next;
                // help gc
                curr.next = null;

                // Completion的tryFire实现不可以抛出异常，否则会导致其它监听器也丢失信号
                future = curr.tryFire(true);

                if (future != null) {
                    // 如果某个Completion使另一个Future进入完成状态，则更新为新的Future，减少递归
                    continue outer;
                }
            }
            break;
        }
    }

    private Completion detachStack() {
        Completion r = this.stack;
        if (r != null) {
            this.stack = null;
        }
        return r;
    }

    /**
     * 清空当前Future上的监听器，并
     * （广播顺序为： 先插入的后广播 ）
     */
    private Completion clearListenersAsJdkOrder(Completion onto) {
        Completion head = detachStack();
        if (head == null) {
            return onto;
        }
        // 将onto插在栈底
        Completion bottom = head;
        while (bottom.next != null) {
            bottom = bottom.next;
        }
        bottom.next = onto;
        // 返回栈顶
        return head;
    }

    /**
     * 清空当前{@code Future}上的监听器，并将当前{@code Future}上的监听器逆序方式插入到{@code onto}前面。
     * （广播顺序为： 先插入的先广播 ）
     * <p>
     * Q: 这步操作是要干什么？<br>
     * A: 由于一个{@link Completion}在执行时可能使另一个{@code Future}进入完成状态，如果不做处理的话，则可能产生一个很深的递归，
     * 从而造成堆栈溢出，也影响性能。该操作就是将可能通知的监听器由树结构展开为链表结构，消除深嵌套的递归。
     * Guava中{@code AbstractFuture}和{@link CompletableFuture}都有类似处理。
     * <pre>
     *      Future1(stack) -> Completion1_1 ->  Completion1_2 -> Completion1_3
     *                              ↓
     *                          Future2(stack) -> Completion2_1 ->  Completion2_2 -> Completion2_3
     *                                                   ↓
     *                                              Future3(stack) -> Completion3_1 ->  Completion3_2 -> Completion3_3
     * </pre>
     * 转换后的结构如下：
     * <pre>
     *      Future1(stack) -> Completion1_1 ->  Completion2_1 ->  Completion2_2 -> Completion2_3 -> Completion1_2 -> Completion1_3
     *                           (已执行)                 ↓
     *                                              Future3(stack) -> Completion3_1 ->  Completion3_2 -> Completion3_3
     * </pre>
     * 1.参考自guava的FluentFuture
     * 2.JDK的CompletableFuture也做了栈优化，但没有做逆序处理，因此广播时是按照出栈顺序广播。。。
     *
     * @return newHead
     */
    private Completion clearListeners(Completion onto) {
        // 1. 将当前栈内元素逆序，因为即使在接口层进行了说明（不提供监听器执行时序保证），但仍然有人依赖于监听器的执行时序(期望先添加的先执行)
        // 2. 将逆序后的元素插入到'onto'前面，即插入到原本要被通知的下一个监听器的前面
        Completion head = detachStack();
        Completion ontoHead = onto;

        while (head != null) {
            Completion tmpHead = head;
            head = head.next;

            tmpHead.next = ontoHead;
            ontoHead = tmpHead; // 最终为栈底
        }

        return ontoHead;
    }

    final void pushCompletionStack(Completion completion) {
        if (isDone()) {
            completion.tryFire(false);
        } else {
            completion.next = stack;
            stack = completion;
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////

    static abstract class Completion {

        Completion next;

        /**
         * 当依赖的某个{@code Future}进入完成状态时，该方法会被调用。
         * <p>
         * 如果tryFire使得另一个{@code Future}进入完成状态，分两种情况：
         * 1. 如果是嵌套模式，则返回新进入完成状态的{@code Future}，而不触发目标{@code Future}的完成事件，由上层代为触发，去除递归。
         * 2. 如果是同步模式，则直接触发目标{@code Future}的完成事件（允许递归）。
         *
         * @apiNote 实现类不可以抛出异常
         */
        abstract AbstractPromise<?> tryFire(boolean nested);

    }

    private static void logCause(Throwable x) {
        if (!(x instanceof NoLogRequiredException)) {
            logger.warn("future completed with exception", x);
        }
    }

    private static AltResult encodeThrowable(Throwable x) {
        return new AltResult((x instanceof CompletionException) ? x :
                new CompletionException(x));
    }

    /** 表示当前future由一个新的值进入完成状态 */
    final void completeValue(V value) {
        internalComplete(encodeValue(value));
    }

    /** 表示当前future由一个新的异常进入完成状态 */
    final void completeThrowable(Throwable x) {
        logCause(x);
        internalComplete(encodeThrowable(x));
    }

    /**
     * 使用依赖项的结果进入完成状态，通常表示当前{@link Completion}只是一个简单的中继。
     * 这里实现和{@link CompletableFuture}不同，这里保留原始结果，不强制将异常转换为{@link CompletionException}。
     * （我个人觉得不封装更好 -- 不封装会丢失中间部分的堆栈）
     */
    final void completeRelay(Object r) {
        internalComplete(r);
    }

    /**
     * 使用依赖项的异常结果进入完成状态，通常表示当前{@link Completion}只是一个简单的中继。
     * 在已知依赖项异常完成的时候可以调用该方法，减少开销。
     * 这里实现和{@link CompletableFuture}不同，这里保留原始结果，不强制将异常转换为{@link CompletionException}。
     */
    final void completeRelayThrowable(AltResult r) {
        internalComplete(r);
    }
}
