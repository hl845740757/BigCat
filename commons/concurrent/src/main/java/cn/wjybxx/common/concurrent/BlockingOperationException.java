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
 * 如果一个操作可能导致死锁状态将抛出该异常
 * 通常是因为监听者和执行者在同一个线程，监听者尝试阻塞等待结果。
 *
 * @author wjybxx
 * date 2023/4/5
 */
public class BlockingOperationException extends RuntimeException {

    public BlockingOperationException() {
    }

    public BlockingOperationException(String s) {
        super(s);
    }

    public BlockingOperationException(String message, Throwable cause) {
        super(message, cause);
    }

    public BlockingOperationException(Throwable cause) {
        super(cause);
    }

}