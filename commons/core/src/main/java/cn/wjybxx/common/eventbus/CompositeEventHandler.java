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

package cn.wjybxx.common.eventbus;

import cn.wjybxx.common.CollectionUtils;
import cn.wjybxx.common.collect.DelayedCompressList;

import javax.annotation.Nonnull;

/**
 * 在事件监听器管理上，你会常见到组合模式
 *
 * @author wjybxx
 * date 2023/4/6
 */
public class CompositeEventHandler<T> implements EventHandler<T> {

    private final DelayedCompressList<EventHandler<? super T>> children;

    public CompositeEventHandler(EventHandler<? super T> first,
                                 EventHandler<? super T> second) {
        children = CollectionUtils.newDelayedCompressList();
        children.add(first);
        children.add(second);
    }

    void add(EventHandler<? super T> handler) {
        children.add(handler);
    }

    boolean remove(EventHandler<?> handler) {
        return children.removeRef(handler);
    }

    int size() {
        return children.size();
    }

    boolean contains(EventHandler<?> handler) {
        return children.containsRef(handler);
    }

    @Override
    public void onEvent(@Nonnull T event) throws Exception {
        // 默认不触发迭代期间新增的事件处理器
        final DelayedCompressList<EventHandler<? super T>> children = this.children;
        children.beginItr();
        try {
            for (int i = 0, size = children.size(); i < size; i++) {
                EventHandler<? super T> handler = children.get(i);
                if (null == handler) {
                    continue;
                }
                EventBusUtils.invokeHandlerSafely(event, handler);
            }
        } finally {
            children.endItr();
        }
    }

}