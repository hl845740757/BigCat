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

/**
 * 用于将事件循环和用户循环绑定起来
 *
 * @author wjybxx
 * date 2023/4/10
 */
public interface EventLoopAgent<T> {

    /**
     * 当事件循环启动的时候将调用该方法
     * 如果启动期间抛出任何异常，线程将终止
     *
     * @param eventLoop 绑定的eventLoop，你可以将其保存下来
     */
    void onStart(EventLoop eventLoop) throws Exception;

    /***
     * 收到一个用户自定义事件或任务
     * {@link RingBufferEvent#getType()}大于0的事件
     */
    void onEvent(T event) throws Exception;

    /**
     * 当事件循环等待较长时间或处理完一批事件之后都将调用该方法
     * 注意：该方法的调用时机和频率是不确定的，因此用户应该自行控制内部逻辑频率。
     */
    void update() throws Exception;

    /**
     * 如果当前线程阻塞在中断也无法唤醒的地方，用户需要唤醒线程
     * 该方法是多线程调用的，要小心并发问题
     */
    default void wakeup() {
    }

    /**
     * 当事件循环退出时将调用该方法
     * 退出前进行必要的清理，释放系统资源
     */
    void onShutdown() throws Exception;

}