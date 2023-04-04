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

import java.util.IdentityHashMap;
import java.util.List;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public class BinaryCodecRegistries {

    public static BinaryCodecRegistry fromPojoCodecs(List<BinaryPojoCodec<?>> pojoCodecs) {
        final IdentityHashMap<Class<?>, BinaryPojoCodec<?>> identityHashMap = new IdentityHashMap<>(pojoCodecs.size());
        for (BinaryPojoCodec<?> codec : pojoCodecs) {
            if (identityHashMap.put(codec.getEncoderClass(), codec) != null) {
                throw new IllegalArgumentException("the class has multiple codecs :" + codec.getEncoderClass().getName());
            }
        }
        return new DefaultCodecRegistry(identityHashMap);
    }

    /** 事实不可变 */
    private static class DefaultCodecRegistry implements BinaryCodecRegistry {

        private final IdentityHashMap<Class<?>, BinaryPojoCodec<?>> type2CodecMap;

        private DefaultCodecRegistry(IdentityHashMap<Class<?>, BinaryPojoCodec<?>> type2CodecMap) {
            this.type2CodecMap = type2CodecMap;
        }

        @SuppressWarnings("unchecked")
        @Override
        public <T> BinaryPojoCodec<T> get(Class<T> clazz) {
            return (BinaryPojoCodec<T>) type2CodecMap.get(clazz);
        }

    }
}