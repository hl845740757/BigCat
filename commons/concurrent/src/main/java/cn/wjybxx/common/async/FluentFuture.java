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

import javax.annotation.Nonnull;
import javax.annotation.concurrent.NotThreadSafe;
import java.util.concurrent.*;
import java.util.function.BiConsumer;
import java.util.function.BiFunction;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * 可参考{@link CompletableFuture}
 * 只不过该接口是单线程的，非线程安全的，且不支持阻塞的。
 * <p>
 * Q：为什么不实现JDK的{@link Future}接口？
 * A：因为{@link Future}要求是线程安全的，继承接口是不安全的。
 * <p>
 * Q：为什么接口比{@link CompletableFuture}少？
 * A：一部分我们转移到了{@link FluentFutureCombiner}，另一部分我们转移到了{@link SameThreads}的适配方法，这可以让我们的接口变得干净。
 *
 * @author wjybxx
 * date 2023/4/3
 */
@NotThreadSafe
public interface FluentFuture<V> {

    // ---------------------------------------- 查询状态和获取结果  ----------------------------------------------------

    /**
     * 查询关联的计算是否已完成
     */
    boolean isDone();

    /**
     * @return 如果future已进入完成状态，且是成功完成，则返回true。
     */
    boolean isSucceeded();

    /**
     * 如果future以任何形式的异常完成，则返回true。
     * 包括被取消，以及显式调用{@link FluentPromise#setCompleteExceptionally(Throwable)}和{@link FluentPromise#completeExceptionally(Throwable)}操作。
     */
    boolean isFailed();

    /**
     * 获取关联的计算结果 -- 非阻塞。
     * 如果对应的计算失败，则抛出对应的异常。
     * 如果计算成功，则返回计算结果。
     * 如果计算尚未完成，则返回null。
     * <p>
     * 如果future关联的task没有返回值(操作完成返回null)，对于这种情况，你可以使用{@link #isSucceeded()}作为判断任务是否成功执行的更好选择。
     *
     * @return task的结果
     * @throws CancellationException 如果任务被取消了，则抛出该异常
     * @throws CompletionException   如果在计算过程中出现了其它异常导致任务失败，则抛出该异常。
     */
    default V getNow() {
        return getNow(null);
    }

    /**
     * 尝试获取计算结果 -- 非阻塞
     * 如果对应的计算失败，则抛出对应的异常。
     * 如果计算成功，则返回计算结果。
     * 如果计算尚未完成，则返回给定值。
     */
    V getNow(V valueIfAbsent);

    /**
     * 获取导致计算失败的原因。
     * 如果关联的任务已失败，则返回对应的失败原因，否则返回null。
     */
    Throwable cause();

    /**
     * 如果{@code Future}已进入完成状态，则立即执行给定动作，否则什么也不做。
     * 该设计其实是一个访问者，即我们知道Future的结果包含正常结果和异常结果，但是如何存储的我们并不知晓，我们进行访问的时候，它可以告诉我们。
     * 它的主要目的是减少读开销，它其实是尝试同时调用{@link #cause()}和{@link #getNow()}。
     *
     * @param action 用于接收当前的执行结果
     * @return 如果执行了给定动作则返回true(即future已完成的情况下返回true)
     */
    boolean acceptNow(@Nonnull BiConsumer<? super V, ? super Throwable> action);

    /**
     * 取消任务
     *
     * @return 取消成功或任务已经被取消，则返回true
     */
    boolean cancel();

    /**
     * @return 如果认为被取消则返回true
     */
    boolean isCancelled();

    // ---------------------------------------- 管道支持  ----------------------------------------------------

    /**
     * 该方法表示在当前{@code Future}与返回的{@code Future}中插入一个异步操作，构建异步管道。
     * 这是本类的核心API，该方法非常强大，一定要学习。
     * 该方法对应我们日常流中使用的{@link java.util.stream.Stream#flatMap(Function)}操作。
     * <p>
     * 该方法返回一个新的{@code Future}，它的最终结果与指定的{@code Function}返回的{@code Future}结果相同。
     * 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
     * 如果当前{@code Future}执行成功，则当前{@code Future}的执行结果将作为指定操作的执行参数。
     * <p>
     * {@link CompletionStage#thenCompose(Function)}
     */
    <U> FluentFuture<U> thenComposeApply(Function<? super V, ? extends FluentFuture<U>> fn);

    /**
     * 它表示能从从特定的异常中恢复，并异步返回一个正常结果。
     * <p>
     * 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
     * 如果当前{@code Future}正常完成，则给定的动作不会执行，且返回的{@code Future}使用相同的结果值进入完成状态。
     * 如果当前{@code Future}执行失败，则其异常信息将作为指定操作的执行参数，返回的{@code Future}的结果取决于指定操作的执行结果。
     */
    <X extends Throwable>
    FluentFuture<V> thenComposeCatching(Class<X> exceptionType, Function<? super X, ? extends FluentFuture<V>> fallback);

    /**
     * 它表示既能接收任务的正常结果，也可以接收任务异常结果，并异步返回一个运算结果。
     */
    <U> FluentFuture<U> thenComposeHandle(BiFunction<? super V, Throwable, ? extends FluentFuture<U>> fn);

    // ---------------------------------------- 链式回调  ----------------------------------------------------

    /**
     * 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
     * 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
     * 如果当前{@code Future}执行成功，则当前{@code Future}的执行结果将作为指定操作的执行参数，返回的{@code Future}的结果取决于指定操作的执行结果。
     * <p>
     * {@link CompletionStage#thenApply(Function)}
     */
    <U> FluentFuture<U> thenApply(Function<? super V, ? extends U> fn);

    /**
     * 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
     * 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
     * 如果当前{@code Future}执行成功，则当前{@code Future}的执行结果将作为指定操作的执行参数，返回的{@code Future}的结果取决于指定操作的执行结果。
     * <p>
     * {@link CompletionStage#thenAccept(Consumer)}
     */
    FluentFuture<Void> thenAccept(Consumer<? super V> action);

    /**
     * 它表示能从从特定的异常中恢复，并返回一个正常结果。
     * <p>
     * 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
     * 如果当前{@code Future}正常完成，则给定的动作不会执行，且返回的{@code Future}使用相同的结果值进入完成状态。
     * 如果当前{@code Future}执行失败，则其异常信息将作为指定操作的执行参数，返回的{@code Future}的结果取决于指定操作的执行结果。
     * <p>
     * 不得不说JDK的{@link CompletionStage#exceptionally(Function)}这个名字太差劲了，实现的也不够好，因此我们不使用它，
     * 这里选择了Guava中的实现
     *
     * @param exceptionType 能处理的异常类型
     * @param fallback      异常恢复函数
     */
    <X extends Throwable>
    FluentFuture<V> catching(Class<X> exceptionType, Function<? super X, ? extends V> fallback);

    /**
     * 该方法表示既能处理当前计算的正常结果，又能处理当前结算的异常结果(可以将异常转换为新的结果)，并返回一个新的结果。
     * <p>
     * 该方法返回一个新的{@code Future}，无论当前{@code Future}执行成功还是失败，给定的操作都将执行。
     * 如果当前{@code Future}执行成功，而指定的动作出现异常，则返回的{@code Future}以该异常完成。
     * 如果当前{@code Future}执行失败，且指定的动作出现异常，则返回的{@code Future}以新抛出的异常进入完成状态。
     * <p>
     * {@link CompletionStage#handle(BiFunction)}
     */
    <U> FluentFuture<U> thenHandle(BiFunction<? super V, Throwable, ? extends U> fn);

    /**
     * 该方法返回一个新的{@code Future}，无论当前{@code Future}执行成功还是失败，给定的操作都将执行，且返回的{@code Future}始终以相同的结果进入完成状态。
     * 与方法{@link #thenHandle(BiFunction)}不同，此方法不是为转换完成结果而设计的，因此提供的操作不应引发异常。
     * 如果确实出现了异常，则仅仅记录一个日志，不向下传播(这里与JDK实现不同)。
     * <p>
     * {@link CompletionStage#whenComplete(BiConsumer)}
     */
    FluentFuture<V> whenComplete(BiConsumer<? super V, ? super Throwable> action);

    // ---------------------------------------- 特殊API一枚 -------------------------------

    /**
     * 注意：该api主要是为了支持前面的流式API的，不怎么建议用户使用。
     * 该方法不是管道调用，也不产生新的{@link FluentFuture}，开销小一些，适合放在方法链的末尾，进行一些特殊的逻辑。
     *
     * @param action 回调监听器
     * @return this!!!
     */
    FluentFuture<V> addListener(@Nonnull BiConsumer<? super V, ? super Throwable> action);

}