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

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import java.util.Map;
import java.util.Objects;

/**
 * @author wjybxx
 * date 2023/4/6
 */
public class EventBusUtils {

    private static final Logger logger = LoggerFactory.getLogger(EventBusUtils.class);

    public static final int DEFAULT_EXPECTED_SIZE = 64;
    public static final int RECURSION_LIMIT = 16;

    private EventBusUtils() {

    }

    /**
     * 抛出事件的真正实现
     *
     * @param handlerMap 事件处理器映射
     * @param event      要抛出的事件
     * @param eventKey   事件对应的key
     * @param <T>        事件的类型
     */
    public static <K, T> void postEvent(Map<K, EventHandler<?>> handlerMap, @Nonnull T event, @Nonnull K eventKey) {
        @SuppressWarnings("unchecked") final EventHandler<? super T> handler = (EventHandler<? super T>) handlerMap.get(eventKey);
        if (null == handler) {
            return;
        }
        try {
            handler.onEvent(event);
        } catch (Exception e) {
            final String handlerClassName = handler.getClass().getName();
            final String eventClassName = event.getClass().getName();
            logger.warn("handlerClassName: " + handlerClassName + ", eventClassName: " + eventClassName, e);
        }
    }

    public static <T> void invokeHandlerSafely(@Nonnull T event, @Nonnull EventHandler<? super T> handler) {
        try {
            handler.onEvent(event);
        } catch (Exception e) {
            final String handlerClassName = handler.getClass().getName();
            final String eventClassName = event.getClass().getName();
            logger.warn("handlerClassName: " + handlerClassName + ", eventClassName: " + eventClassName, e);
        }
    }

    /**
     * 添加事件处理器的真正实现
     *
     * @param handlerMap 保存事件处理器的map
     * @param eventKey   关注的事件对应的key
     * @param handler    事件处理器
     * @param <T>        事件的类型
     */
    public static <K, T> void addHandler(Map<K, EventHandler<?>> handlerMap, @Nonnull K eventKey, EventHandler<? super T> handler) {
        Objects.requireNonNull(handler);
        @SuppressWarnings("unchecked") final EventHandler<? super T> existHandler = (EventHandler<? super T>) handlerMap.get(eventKey);
        if (null == existHandler) {
            handlerMap.put(eventKey, handler);
            return;
        }
        if (existHandler instanceof CompositeEventHandler) {
            @SuppressWarnings("unchecked") final CompositeEventHandler<T> compositeEventHandler = (CompositeEventHandler<T>) existHandler;
            compositeEventHandler.add(handler);
        } else {
            handlerMap.put(eventKey, new CompositeEventHandler<>(existHandler, handler));
        }
    }

    public static <K, T> boolean removeHandler(Map<K, EventHandler<?>> handlerMap, @Nonnull K eventKey, EventHandler<? super T> handler) {
        if (handler == null) {
            return false;
        }
        @SuppressWarnings("unchecked") final EventHandler<? super T> existHandler = (EventHandler<? super T>) handlerMap.get(eventKey);
        if (null == existHandler) {
            return false;
        }

        if (existHandler == handler) {
            handlerMap.remove(eventKey);
            return true;
        }
        if (existHandler instanceof CompositeEventHandler<?> compositeEventHandler) {
            final boolean changed = compositeEventHandler.remove(handler);
            if (changed && compositeEventHandler.size() == 0) {
                handlerMap.remove(eventKey);
            }
            return changed;
        } else {
            return false;
        }
    }

    public static <K> boolean hasListener(Map<K, EventHandler<?>> handlerMap, @Nonnull K eventKey, EventHandler<?> handler) {
        if (handler == null) {
            return false;
        }
        final EventHandler<?> existHandler = handlerMap.get(eventKey);
        if (null == existHandler) {
            return false;
        }
        if (existHandler == handler) {
            return true;
        }
        if (existHandler instanceof CompositeEventHandler<?> compositeEventHandler) {
            return compositeEventHandler.contains(handler);
        } else {
            return false;
        }
    }

}