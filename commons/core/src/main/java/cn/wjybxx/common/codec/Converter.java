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

import cn.wjybxx.dson.io.DsonChunk;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.ThreadSafe;

/**
 * Q：如何解决集合和Map的编解码问题？
 * A：为使用到的集合分配{@link TypeMeta}，并利用Utils类提供的方法创建对应{@code CollectionCodec}和{@code MapCodec}实例。
 * <p>
 * Q：如何解决Protobuf的消息编解码问题？
 * A：为使用到的消息分配{@link TypeMeta}，并利用Utils类提供的方法创建对应的{@code MessageCodec}。
 *
 * @author wjybxx
 * date 2023/3/31
 */
@ThreadSafe
public interface Converter {

    /**
     * 将一个对象写入源
     * 如果对象的运行时类型和{@link TypeArgInfo#declaredType}一致，则会省去编码结果中的类型信息
     */
    @Nonnull
    byte[] write(Object value, @Nonnull TypeArgInfo<?> typeArgInfo);

    /**
     * 从数据源中读取一个对象
     *
     * @param source      数据源
     * @param typeArgInfo 要读取的目标类型信息，部分实现支持投影
     */
    default <U> U read(byte[] source, @Nonnull TypeArgInfo<U> typeArgInfo) {
        return read(new DsonChunk(source), typeArgInfo);
    }

    /**
     * @param value       要写入的对象
     * @param chunk       二进制块，写入的字节数设置到{@link DsonChunk}
     * @param typeArgInfo 类型参数信息
     */
    void write(Object value, DsonChunk chunk, TypeArgInfo<?> typeArgInfo);

    /**
     * @param chunk       二进制块，读取的字节数设置到{@link DsonChunk}
     * @param typeArgInfo 类型参数信息
     * @return 解码结果，顶层对象不应该是null
     */
    <U> U read(DsonChunk chunk, TypeArgInfo<U> typeArgInfo);

    @Nonnull
    default byte[] write(Object value) {
        return write(value, TypeArgInfo.OBJECT);
    }

    default Object read(@Nonnull byte[] source) {
        return read(source, TypeArgInfo.OBJECT);
    }

    /**
     * @param buffer      编码输出
     * @param typeArgInfo 类型参数信息
     * @return 写入的字节数
     */
    default int write(Object value, byte[] buffer, TypeArgInfo<?> typeArgInfo) {
        DsonChunk chunk = new DsonChunk(buffer);
        write(value, chunk, typeArgInfo);
        return chunk.getUsed();
    }

    /**
     * 克隆一个对象。
     * 注意：
     * 1.返回值的类型不一定和原始对象相同，这通常发生在集合对象上。
     * 2.如果Codec存在lazyDecode，也会导致不同
     *
     * @param typeArgInfo 用于确定返回结果类型
     */
    default <U> U cloneObject(Object value, TypeArgInfo<U> typeArgInfo) {
        if (value == null) {
            return null;
        }
        final byte[] out = write(value, typeArgInfo);
        return read(out, typeArgInfo);
    }

}