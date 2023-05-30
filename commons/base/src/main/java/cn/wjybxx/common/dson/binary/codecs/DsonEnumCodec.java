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

package cn.wjybxx.common.dson.binary.codecs;

import cn.wjybxx.common.dson.DsonEnum;
import cn.wjybxx.common.dson.TypeArgInfo;
import cn.wjybxx.common.dson.binary.BinaryObjectReader;
import cn.wjybxx.common.dson.binary.BinaryObjectWriter;
import cn.wjybxx.common.dson.binary.BinaryPojoCodecImpl;

import javax.annotation.Nonnull;
import java.util.Objects;
import java.util.function.IntFunction;

/**
 * 在之前的版本中，生成的代码直接继承{@link BinaryPojoCodecImpl}，这让枚举的编解码调整较为麻烦，
 * 因为要调整APT，而让枚举的Codec继承该Codec的话，生成的代码就更为稳定，我们调整编解码方式也更方便。
 *
 * @author wjybxx
 * date - 2023/4/24
 */
public class DsonEnumCodec<T extends DsonEnum> implements BinaryPojoCodecImpl<T> {

    private final Class<T> encoderClass;
    private final IntFunction<T> mapper;

    /**
     * @param forNumber forNumber静态方法的lambda表达式
     */
    public DsonEnumCodec(Class<T> encoderClass, IntFunction<T> forNumber) {
        this.encoderClass = Objects.requireNonNull(encoderClass);
        this.mapper = Objects.requireNonNull(forNumber);
    }

    @Nonnull
    @Override
    public Class<T> getEncoderClass() {
        return encoderClass;
    }

    @Override
    public void writeObject(T instance, BinaryObjectWriter writer, TypeArgInfo<?> typeArgInfo) {
        writer.writeInt(0, instance.getNumber());
    }

    @Override
    public T readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        return mapper.apply(reader.readInt(0));
    }

}