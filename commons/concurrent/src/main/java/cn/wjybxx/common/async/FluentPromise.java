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

import cn.wjybxx.common.ex.NoLogRequiredException;

import javax.annotation.concurrent.NotThreadSafe;

/**
 * Q: 为什么在底层自动记录异常日志？
 * A: 实际使用的时候发现，如果靠写业务的时候保证不丢失异常信息，十分危险，如果疏忽将导致异常信息丢失，异常信息十分重要，不可轻易丢失。
 *
 * @author wjybxx
 * date 2023/4/3
 */
@NotThreadSafe
public interface FluentPromise<V> extends FluentFuture<V> {

    /**
     * 尝试将future置为成功完成状态，如果future已进入完成状态，则返回false
     */
    boolean complete(V result);

    /**
     * 将future置为成功完成状态，如果future已进入完成状态，则抛出{@link IllegalStateException}
     */
    void setComplete(V result);

    /**
     * 尝试将future置为失败完成状态，如果future已进入完成状态，则返回false
     *
     * @param logCause 是否记录日志
     *                 注意：即便为true，如果异常是{@link NoLogRequiredException}，那么也不记录日志
     */
    boolean completeExceptionally(Throwable cause, boolean logCause);

    /**
     * 将future置为失败状态，如果future已进入完成状态，则抛出{@link IllegalStateException}
     *
     * @param logCause 是否记录日志
     *                 注意：即便为true，如果异常是{@link NoLogRequiredException}，那么也不记录日志
     */
    void setCompleteExceptionally(Throwable cause, boolean logCause);

    /**
     * 尝试将future置为失败完成状态，如果future已进入完成状态，则返回false
     */
    default boolean completeExceptionally(Throwable cause) {
        return completeExceptionally(cause, true);
    }

    /**
     * 将future置为失败状态，如果future已进入完成状态，则抛出{@link IllegalStateException}
     */
    default void setCompleteExceptionally(Throwable cause) {
        setCompleteExceptionally(cause, true);
    }

}