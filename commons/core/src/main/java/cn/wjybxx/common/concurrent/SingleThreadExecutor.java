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

import java.util.concurrent.Executor;

/**
 * 单线程的Executor
 *
 * @author wjybxx
 * date - 2023/11/6
 */
public interface SingleThreadExecutor extends Executor {

    /***
     * 测试当前线程是否是{@link java.util.concurrent.Executor}所在线程。
     * 主要作用:
     * 1. 判断是否可访问线程封闭的数据。
     * 2. 防止死锁。
     * <p>
     * 更多可参考{@link EventLoop#inEventLoop()}
     */
    boolean inEventLoop();

}
