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

package cn.wjybxx.common.codec.binary;

import javax.annotation.Nullable;
import java.util.ArrayList;
import java.util.IdentityHashMap;
import java.util.List;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public class BinaryCodecRegistries {

    // region

    public static Map<Class<?>, BinaryPojoCodec<?>> newCodecMap(List<BinaryPojoCodec<?>> pojoCodecs) {
        final IdentityHashMap<Class<?>, BinaryPojoCodec<?>> codecMap = new IdentityHashMap<>(pojoCodecs.size());
        for (BinaryPojoCodec<?> codec : pojoCodecs) {
            codecMap.put(codec.getEncoderClass(), codec);
        }
        return codecMap;
    }

    public static BinaryCodecRegistry fromPojoCodecImpl(List<BinaryPojoCodecImpl<?>> pojoCodecs) {
        List<BinaryPojoCodec<?>> codecList = new ArrayList<>(pojoCodecs.size());
        for (BinaryPojoCodecImpl<?> pojoCodec : pojoCodecs) {
            codecList.add(new BinaryPojoCodec<>(pojoCodec));
        }
        return fromPojoCodecs(codecList);
    }

    public static BinaryCodecRegistry fromPojoCodecs(List<BinaryPojoCodec<?>> pojoCodecs) {
        final IdentityHashMap<Class<?>, BinaryPojoCodec<?>> identityHashMap = new IdentityHashMap<>(pojoCodecs.size());
        for (BinaryPojoCodec<?> codec : pojoCodecs) {
            if (identityHashMap.put(codec.getEncoderClass(), codec) != null) {
                throw new IllegalArgumentException("the class has multiple codecs :" + codec.getEncoderClass().getName());
            }
        }
        return new DefaultCodecRegistry(identityHashMap);
    }

    public static BinaryCodecRegistry fromRegistries(BinaryCodecRegistry... codecRegistry) {
        return new CompositeCodecRegistry(List.of(codecRegistry));
    }

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

    private static class CompositeCodecRegistry implements BinaryCodecRegistry {

        private final List<BinaryCodecRegistry> registryList;

        private CompositeCodecRegistry(List<BinaryCodecRegistry> registryList) {
            this.registryList = registryList;
        }

        @Nullable
        @Override
        public <T> BinaryPojoCodec<T> get(Class<T> clazz) {
            List<BinaryCodecRegistry> registryList = this.registryList;
            for (int i = 0; i < registryList.size(); i++) {
                BinaryPojoCodec<T> codec = registryList.get(i).get(clazz);
                if (codec != null) return codec;
            }
            return null;
        }
    }
    // endregion
}