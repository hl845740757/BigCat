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

package cn.wjybxx.common.dson.binary;

import cn.wjybxx.common.dson.Converter;
import cn.wjybxx.common.dson.TypeArgInfo;
import cn.wjybxx.common.dson.io.Chunk;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.ThreadSafe;

/**
 * 二进制转换器
 * 二进制是指将对象序列化为字节数组，以编解码效率和压缩比例为重。
 * <p>
 * <h3>实现要求</h3>
 * 1.必须是线程安全的，建议实现为不可变对象
 *
 * @author wjybxx
 * date 2023/3/31
 */
@ThreadSafe
public interface BinaryConverter extends Converter<byte[]> {

    /**
     * @param value       要写入的对象
     * @param chunk       二进制块，写入的字节数设置到{@link Chunk}
     * @param typeArgInfo 类型参数信息
     */
    void write(Object value, Chunk chunk, TypeArgInfo<?> typeArgInfo);

    /**
     * @param chunk       二进制块，读取的字节数设置到{@link Chunk}
     * @param typeArgInfo 类型参数信息
     * @return 解码结果，顶层对象不应该是null
     */
    <U> U read(Chunk chunk, TypeArgInfo<U> typeArgInfo);

    @Nonnull
    @Override
    byte[] write(Object value, @Nonnull TypeArgInfo<?> typeArgInfo);

    @Override
    default <U> U read(byte[] source, TypeArgInfo<U> typeArgInfo) {
        return read(new Chunk(source), typeArgInfo);
    }

    /** @return 写入的字节数 */
    default int write(Object value, byte[] source, TypeArgInfo<?> typeArgInfo) {
        Chunk chunk = new Chunk(source);
        write(value, chunk, typeArgInfo);
        return chunk.getUsed();
    }

    BinaryCodecRegistry codecRegistry();

    TypeIdRegistry typeIdRegistry();

}