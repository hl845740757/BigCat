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

package cn.wjybxx.bigcat.common.async;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public final class ResultHolder<V> {

    private static final ResultHolder<?> NULL = new ResultHolder<>(null);

    public final V result;

    private ResultHolder(V result) {
        this.result = result;
    }

    @SuppressWarnings("unchecked")
    public static <V> ResultHolder<V> succeeded() {
        return (ResultHolder<V>) NULL;
    }

    public static <V> ResultHolder<V> succeeded(V result) {
        return new ResultHolder<>(result);
    }

    @SuppressWarnings("unchecked")
    public static <V> ResultHolder<V> empty() {
        return (ResultHolder<V>) NULL;
    }

}