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

package cn.wjybxx.bigcat.common.codec.binary;

import cn.wjybxx.bigcat.common.codec.EntityConverter;
import cn.wjybxx.bigcat.common.codec.TypeArgInfo;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.ThreadSafe;

/**
 * 二进制转换器
 * 二进制是指将对象序列化为字节数组，以编解码效率和压缩比例为重。
 * <p>
 * 注意：
 * 我们在序列化为二进制的约定中，不会写入字段的名字和number，这导致对象数据升级的话，二进制数据很可能是不兼容的，之所以如此，有3个原因：
 * <p>
 * 1.提供二进制编码的第一个目的是为了快速编解码JavaBean，以避免定义大量的protobuf对象 —— 开发的便捷性。
 * 2.第二个目的是用于服务器间临时消息交互(就像客户端和服务器)，并不是为了做数据持久化的，持久化我们使用文档型编解码。
 * 3.为了更好的支持Java对象的编解码(通常是final字段)，我们要支持用户手写编解码实现，而要支持手写的话，API必须要易用，就需要支持number随机访问，
 * 而要支持根据number读写需要额外的缓存，会导致内存的增加和编解码速度的降低；另外，用户维护number也是较大的成本。
 * <p>
 * Q：如果想持久化二进制数据，且数据升级后能保持兼容怎么办？
 * A：如果你需要跨语言或持久化二进制数据，建议使用protobuf这类解决方案。
 * <p>
 * <h3>实现要求</h3>
 * 1.必须检查基础类型数组的编解码器是否存在于{@link BinaryCodecRegistry}，否则基础类型数组无法精确解析
 * 2.必须是线程安全的，建议实现为不可变对象
 *
 * <h3>一些奇怪的特性</h3>
 * 1.编码必须按照约定编码，但解码可以不完全{@link BinaryReader#remainObjectBytes()}
 * 2.编码的类型不重要，只要内容兼容，你可以读取为其它类型。
 *
 * @author wjybxx
 * date 2023/3/31
 */
@ThreadSafe
public interface BinaryConverter extends EntityConverter<byte[]> {

    /**
     * @param value       要写入的对象
     * @param chunk       二进制块，写入的字节数设置到{@link Chunk}
     * @param typeArgInfo 类型参数信息
     */
    void write(@Nullable Object value, Chunk chunk, TypeArgInfo<?> typeArgInfo);

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