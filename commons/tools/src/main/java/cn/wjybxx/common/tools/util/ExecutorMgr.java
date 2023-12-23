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

package cn.wjybxx.common.tools.util;

import cn.wjybxx.common.MathCommon;
import cn.wjybxx.common.concurrent.DefaultThreadFactory;

import java.util.List;
import java.util.concurrent.*;

/**
 * 工具模块用公共线程池
 *
 * @author wjybxx
 * date - 2023/10/14
 */
public class ExecutorMgr {

    private final ExecutorService executorService;

    public ExecutorMgr() {
        final int poolSize = calPoolSize();
        final ThreadPoolExecutor threadPoolExecutor = new ThreadPoolExecutor(poolSize, poolSize,
                5, TimeUnit.SECONDS,
                new ArrayBlockingQueue<>(poolSize * 64),
                new DefaultThreadFactory("IO_THREAD", true),
                new ThreadPoolExecutor.CallerRunsPolicy()); // 工具模块，调用者线程也执行任务，安全
        threadPoolExecutor.allowCoreThreadTimeOut(true);
        executorService = threadPoolExecutor;
    }

    private static int calPoolSize() {
        final int availableProcessors = Runtime.getRuntime().availableProcessors();
        final int expectedSize = availableProcessors / 2;
        return MathCommon.clamp(expectedSize, 4, 16);
    }

    public ExecutorService getExecutorService() {
        return executorService;
    }

    public void execute(Runnable command) {
        executorService.execute(command);
    }

    public <T> Future<T> submit(Callable<T> task) {
        return executorService.submit(task);
    }

    public <T> Future<T> submit(Runnable task, T result) {
        return executorService.submit(task, result);
    }

    public Future<?> submit(Runnable task) {
        return executorService.submit(task);
    }

    public void shutdown() {
        executorService.shutdown();
    }

    public List<Runnable> shutdownNow() {
        return executorService.shutdownNow();
    }

    public void close() {
        executorService.close();
    }

}