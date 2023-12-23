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
import java.util.concurrent.Callable;
import java.util.concurrent.Executor;

/**
 * 用于在当前线程延迟执行任务的Executor。
 * 即：该Executor仍然在当前线程（提交任务的线程）执行提交的任务，只是会延迟执行。
 * PS：放弃了继承{@link Executor}接口，避免用户误用
 * <h3>时序要求</h3>
 * 我们限定逻辑是在当前线程执行的，必须保证先提交的任务先执行。
 *
 * <h3>限制单帧任务数</h3>
 * 由于是在当前线程执行对应的逻辑，因而必须限制单帧执行的任务数，以避免占用过多的资源，同时，限定单帧任务数可避免死循环。
 *
 * <h3>外部驱动</h3>
 * 由于仍然是在当前线程执行，因此需要外部进行驱动，外部需要定时调用{@link #tick()}
 *
 * @author wjybxx
 * date 2023/4/3
 */
@NotThreadSafe
public interface SameThreadExecutor {

    void execute(@Nonnull Runnable command);

    FluentFuture<?> submitRun(@Nonnull Runnable command);

    <V> FluentFuture<V> submitCall(@Nonnull Callable<V> command);

    /**
     * Q：返回值的意义？
     * A：为避免死循环或占用过多cpu，单次tick可能存在一些限制（即：屏障），如果外部确实想处理更多的任务，则可以根据该值判断是否继续运行。
     *
     * @return 如果还有可执行任务则返回true，否则返回false
     */
    boolean tick();

    /**
     * 关闭Executor
     */
    void shutdown();

    /**
     * @return 如果Executor已关闭则返回true
     */
    boolean isShutdown();

}