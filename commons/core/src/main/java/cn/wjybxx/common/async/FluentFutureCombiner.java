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

import javax.annotation.concurrent.NotThreadSafe;
import java.util.Collection;

/**
 * {@link FluentFuture}聚合器。
 * <p>
 * 在调用任意选择方法之前，用户可以通过{@link #add(FluentFuture)}和{@link #addAll(FluentFuture[])}方法添加任意数量的{@link FluentFuture}，
 * 调用任意的选择方法后，当前combiner无法继续选择（理论上可以做到支持，但暂时还无需求）。
 *
 * @author wjybxx
 * date 2023/4/3
 */
@NotThreadSafe
public interface FluentFutureCombiner {

    /**
     * 添加一个要监听的future
     *
     * @return this
     */
    FluentFutureCombiner add(FluentFuture<?> future);

    default FluentFutureCombiner addAll(FluentFuture<?>... futures) {
        for (FluentFuture<?> future : futures) {
            this.add(future);
        }
        return this;
    }

    default FluentFutureCombiner addAll(Collection<? extends FluentFuture<?>> futures) {
        for (FluentFuture<?> future : futures) {
            this.add(future);
        }
        return this;
    }

    /**
     * 获取监听的future数量
     * 注意：future计数是不去重的，一个future反复添加会反复计数
     */
    int futureCount();

    // region 选择方法 - 终结方法

    /**
     * 返回的promise在任意future进入完成状态时进入完成状态
     * 返回的promise与首个future的结果相同
     */
    FluentPromise<Object> anyOf();

    /**
     * 成功N个触发成功
     * 如果触发失败，只随机记录一个Future的异常信息，而不记录所有的异常信息
     * <p>
     * 1.如果require等于【0】，则必定会成功。
     * 2.如果require大于监听的future数量，必定会失败。
     * 3.如果require小于监听的future数量，当成功任务数达到期望时触发成功。
     * <p>
     * 如果lazy为false，则满足成功/失败条件时立即触发完成；
     * 如果lazy为true，则等待所有任务完成之后才触发成功或失败。
     *
     * @param successRequire 期望成成功的任务数
     * @param lazy           是否在所有任务都进入完成状态之后才触发成功或失败
     */
    FluentPromise<Object> selectN(int successRequire, boolean lazy);

    /**
     * 要求所有的future都成功时才进入成功状态；
     * 任意任务失败，最终结果都表现为失败
     *
     * @param lazy 是否在所有任务都进入完成状态之后才触发成功或失败
     */
    default FluentPromise<Object> selectAll(boolean lazy) {
        return selectN(futureCount(), lazy);
    }

    /**
     * 要求所有的future都成功时才进入成功状态
     * 一旦有任务失败则立即失败
     */
    default FluentPromise<Object> selectAll() {
        return selectN(futureCount(), false);
    }

    // endregion

}