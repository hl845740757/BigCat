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

package cn.wjybxx.common.codec.document;

import javax.annotation.Nullable;
import java.util.ArrayList;
import java.util.IdentityHashMap;
import java.util.List;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/4/4
 */
public class DocumentCodecRegistries {

    public static Map<Class<?>, DocumentPojoCodec<?>> newCodecMap(List<DocumentPojoCodec<?>> pojoCodecs) {
        final IdentityHashMap<Class<?>, DocumentPojoCodec<?>> codecMap = new IdentityHashMap<>(pojoCodecs.size());
        for (DocumentPojoCodec<?> codec : pojoCodecs) {
            codecMap.put(codec.getEncoderClass(), codec);
        }
        return codecMap;
    }

    public static DocumentCodecRegistry fromPojoCodecImpl(List<DocumentPojoCodecImpl<?>> pojoCodecs) {
        ArrayList<DocumentPojoCodec<?>> codecList = new ArrayList<>(pojoCodecs.size());
        for (DocumentPojoCodecImpl<?> pojoCodec : pojoCodecs) {
            codecList.add(new DocumentPojoCodec<>(pojoCodec));
        }
        return fromPojoCodecs(codecList);
    }

    public static DocumentCodecRegistry fromPojoCodecs(List<DocumentPojoCodec<?>> pojoCodecs) {
        final IdentityHashMap<Class<?>, DocumentPojoCodec<?>> identityHashMap = new IdentityHashMap<>(pojoCodecs.size());
        for (DocumentPojoCodec<?> codec : pojoCodecs) {
            if (identityHashMap.put(codec.getEncoderClass(), codec) != null) {
                throw new IllegalArgumentException("the class has multiple codecs :" + codec.getEncoderClass().getName());
            }
        }
        return new DefaultCodecRegistry(identityHashMap);
    }

    public static DocumentCodecRegistry fromRegistries(DocumentCodecRegistry... codecRegistry) {
        return new CompositeCodecRegistry(List.of(codecRegistry));
    }

    private static class DefaultCodecRegistry implements DocumentCodecRegistry {

        private final IdentityHashMap<Class<?>, DocumentPojoCodec<?>> type2CodecMap;

        private DefaultCodecRegistry(IdentityHashMap<Class<?>, DocumentPojoCodec<?>> type2CodecMap) {
            this.type2CodecMap = type2CodecMap;
        }

        @SuppressWarnings("unchecked")
        @Override
        public <T> DocumentPojoCodec<T> get(Class<T> clazz) {
            return (DocumentPojoCodec<T>) type2CodecMap.get(clazz);
        }

    }

    private static class CompositeCodecRegistry implements DocumentCodecRegistry {

        private final List<DocumentCodecRegistry> registryList;

        private CompositeCodecRegistry(List<DocumentCodecRegistry> registryList) {
            this.registryList = registryList;
        }

        @Nullable
        @Override
        public <T> DocumentPojoCodec<T> get(Class<T> clazz) {
            List<DocumentCodecRegistry> registryList = this.registryList;
            for (int i = 0; i < registryList.size(); i++) {
                DocumentCodecRegistry registry = registryList.get(i);
                DocumentPojoCodec<T> codec = registry.get(clazz);
                if (codec != null) return codec;
            }
            return null;
        }
    }

}