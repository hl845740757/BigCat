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
 * @author wjybxx
 * date 2023/4/11
 */
public final class EmptyAgent<T> implements EventLoopAgent<T> {

    private static final EmptyAgent<?> INSTANCE = new EmptyAgent<>();

    @SuppressWarnings("unchecked")
    public static <T> EmptyAgent<T> getInstance() {
        return (EmptyAgent<T>) INSTANCE;
    }

    @Override
    public void onStart(EventLoop eventLoop) {

    }

    @Override
    public void onEvent(T event) throws Exception {

    }

    @Override
    public void update() {

    }

    @Override
    public void onShutdown() {

    }

}