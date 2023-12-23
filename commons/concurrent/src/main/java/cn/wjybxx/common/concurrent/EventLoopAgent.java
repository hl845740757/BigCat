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
 * Agent是EventLoop的内部代理，是EventLoop的内部策略实现，通常不暴露给EventLoop外部
 * <p>
 * 1.Agent将事件循环和用户循环绑定起来
 * 2.这里的方法只应由EventLoop调用，其它地方不应该调用
 *
 * @author wjybxx
 * date 2023/4/10
 */
public interface EventLoopAgent {

    /**
     * 在构造EventLoop的过程中将调用该方法注入实例
     * 注意：此时EventLoop可能尚未完全初始化！
     *
     * @param eventLoop 绑定的事件循环
     */
    void inject(EventLoop eventLoop);

    /**
     * 当事件循环启动的时候将调用该方法
     * 注意：该方法抛出任何异常，都将导致事件循环线程终止！
     */
    void onStart() throws Exception;

    /***
     * 收到一个用户自定义事件或任务
     * {@link RingBufferEvent#getType()}大于0的事件
     */
    void onEvent(RingBufferEvent event) throws Exception;

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