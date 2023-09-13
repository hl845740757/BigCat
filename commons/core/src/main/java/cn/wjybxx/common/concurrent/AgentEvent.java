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
 * 需要EventLoop转发给{@link EventLoopAgent}的事件，
 * 这些事件被以{@link Runnable}的形式提交给了{@link EventLoop}。
 *
 * @author wjybxx
 * date - 2023/8/31
 */
public interface AgentEvent extends Runnable {

    @Override
    default void run() {
        // do nothing
    }

}