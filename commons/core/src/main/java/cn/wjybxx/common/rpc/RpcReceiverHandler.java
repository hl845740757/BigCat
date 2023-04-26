/*
 * Copyright 2023 wjybxx
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

package cn.wjybxx.common.rpc;

import cn.wjybxx.common.concurrent.WatchableEventQueue;

/**
 * rpc接收处理器
 *
 * @author wjybxx
 * date 2023/4/5
 */
public interface RpcReceiverHandler {

    /**
     * 监听rpc结果
     * 该方法应将watcher适配为事件队列的watcher，并注册到事件队列中
     */
    void watch(WatchableEventQueue.Watcher<? super RpcResponse> watcher);

    void cancelWatch(WatchableEventQueue.Watcher<? super RpcResponse> watcher);

}