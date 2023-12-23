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

import javax.annotation.concurrent.ThreadSafe;

/**
 * 事件循环选择器，用于负载均衡和确定选择。
 *
 * @author wjybxx
 * date 2023/4/7
 */
@ThreadSafe
public interface EventLoopChooser {

    /**
     * 按默认规则分配一个{@link EventLoop}
     */
    EventLoop select();

    /**
     * 通过给定键选择一个{@link EventLoop}
     *
     * @apiNote 同一个key的选择结果必须是相同的
     */
    EventLoop select(int key);

}