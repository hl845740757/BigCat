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

import cn.wjybxx.common.props.PropertiesUtils;

import javax.annotation.Nonnull;
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
public class LocalPools {

    /** 字节数组不能扩容，因此需要提前规划 */
    private static final int BUFFER_SIZE;
    /** 用以支持递归 */
    private static final int POOL_SIZE;

    private static final ThreadLocal<Queue<byte[]>> LOCAL_BUFFER_QUEUE;
    private static final ThreadLocal<Queue<StringBuilder>> LOCAL_STRING_QUEUE;

    static {
        Properties properties = System.getProperties();
        BUFFER_SIZE = PropertiesUtils.getInt(properties, "cn.wjybxx.common.codec.buffsize", 64 * 1024);
        POOL_SIZE = PropertiesUtils.getInt(properties, "cn.wjybxx.common.codec.poolsize", 4);

        LOCAL_BUFFER_QUEUE = ThreadLocal.withInitial(() -> new ArrayDeque<>(POOL_SIZE));
        LOCAL_STRING_QUEUE = ThreadLocal.withInitial(() -> new ArrayDeque<>(POOL_SIZE));
    }

    public static final BufferPool BUFFER_POOL = new LocalBufferPool();
    public static final StringBuilderPool STRING_BUILDER_POOL = new LocalStringBuilderPool();

    private static class LocalBufferPool implements BufferPool {

        @Override
        public byte[] alloc() {
            final byte[] buffer = LOCAL_BUFFER_QUEUE.get().poll();
            if (buffer != null) {
                return buffer;
            }
            return new byte[BUFFER_SIZE];
        }

        @Override
        public void release(byte[] buffer) {
            Objects.requireNonNull(buffer, "buffer");
            final Queue<byte[]> queue = LOCAL_BUFFER_QUEUE.get();
            if (queue.size() < POOL_SIZE) {
                queue.offer(buffer);
            }
        }
    }

    private static class LocalStringBuilderPool implements StringBuilderPool {

        @Nonnull
        @Override
        public StringBuilder alloc() {
            StringBuilder stringBuilder = LOCAL_STRING_QUEUE.get().poll();
            if (stringBuilder != null) {
                return stringBuilder;
            }
            return new StringBuilder(1024);
        }

        @Override
        public void release(StringBuilder builder) {
            Objects.requireNonNull(builder, "builder");
            Queue<StringBuilder> queue = LOCAL_STRING_QUEUE.get();
            if (queue.size() < POOL_SIZE) {
                builder.setLength(0);
                queue.offer(builder);
            }
        }
    }

}