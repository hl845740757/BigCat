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

package cn.wjybxx.common.codec;

import cn.wjybxx.dson.internal.InternalUtils;

import java.util.ArrayDeque;
import java.util.Objects;
import java.util.Properties;
import java.util.Queue;

/**
 * 基于ThreadLocal的buffer池
 *
 * @author wjybxx
 * date 2023/3/31
 */
public class LocalBufferPool implements BufferPool {

    /**
     * 单个buffer大小
     */
    private static final int BUFFER_SIZE;
    /**
     * 单线程缓存数量
     * 正常情况下应该只会消耗一个，支持该值是为了支持用户在编码的过程中递归
     */
    private static final int POOL_SIZE;
    /**
     * 线程隔离的Buffer
     */
    private static final ThreadLocal<Queue<byte[]>> LOCAL_BUFFER_QUEUE;

    static {
        Properties properties = System.getProperties();
        BUFFER_SIZE = InternalUtils.getInt(properties, "cn.wjybxx.common.codec.buffsize", 1024 * 1024);
        POOL_SIZE = InternalUtils.getInt(properties, "cn.wjybxx.common.codec.poolsize", 4);
        LOCAL_BUFFER_QUEUE = ThreadLocal.withInitial(() -> new ArrayDeque<>(POOL_SIZE));
    }

    public static final LocalBufferPool INSTANCE = new LocalBufferPool();

    @Override
    public byte[] alloc() {
        final byte[] buffer = LOCAL_BUFFER_QUEUE.get().poll();
        if (buffer == null) {
            return new byte[BUFFER_SIZE];
        } else {
            return buffer;
        }
    }

    @Override
    public void release(byte[] buffer) {
        Objects.requireNonNull(buffer, "buffer");
        final Queue<byte[]> queue = LOCAL_BUFFER_QUEUE.get();
        if (queue.size() >= POOL_SIZE) {
            return;
        }
        queue.offer(buffer);
    }

}