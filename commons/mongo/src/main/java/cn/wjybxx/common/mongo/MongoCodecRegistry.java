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

package cn.wjybxx.common.mongo;

import org.bson.codecs.Codec;
import org.bson.codecs.configuration.CodecRegistries;
import org.bson.codecs.configuration.CodecRegistry;

import java.lang.reflect.Type;
import java.util.*;

/**
 * 用于集成最终的CodecRegistry
 * 建议将Mongo默认的CodecRegistry也注册进来
 *
 * @author wjybxx
 * date - 2023/4/17
 */
public class MongoCodecRegistry implements CodecRegistry {

    private final CodecRegistry delegated;

    private MongoCodecRegistry(Builder builder) {
        List<MongoCodecAdapter<?>> adapterList = new ArrayList<>(builder.codecMap.size());
        for (MongoCodec<?> mongoCodec : builder.codecMap.values()) {
            adapterList.add(new MongoCodecAdapter<>(this, mongoCodec));
        }
        CodecRegistry customCodecRegistry = CodecRegistries.fromCodecs(adapterList);
        if (builder.moreCodecRegistries.size() > 0) {
            ArrayList<CodecRegistry> codecRegistries = new ArrayList<>(builder.moreCodecRegistries.size() + 1);
            codecRegistries.addAll(builder.moreCodecRegistries);
            codecRegistries.add(customCodecRegistry);
            delegated = CodecRegistries.fromRegistries(codecRegistries);
        } else {
            delegated = customCodecRegistry;
        }
    }

    @Override
    public <T> Codec<T> get(Class<T> clazz, CodecRegistry registry) {
        return delegated.get(clazz, registry);
    }

    @Override
    public <T> Codec<T> get(Class<T> clazz) {
        return delegated.get(clazz);
    }

    @Override
    public <T> Codec<T> get(Class<T> clazz, List<Type> typeArguments) {
        return delegated.get(clazz, typeArguments);
    }

    @Override
    public <T> Codec<T> get(Class<T> clazz, List<Type> typeArguments, CodecRegistry registry) {
        return delegated.get(clazz, typeArguments, registry);
    }

    public static Builder newBuilder() {
        return new Builder();
    }

    public static class Builder {

        private final Map<Class<?>, MongoCodec<?>> codecMap = new IdentityHashMap<>();
        private final List<CodecRegistry> moreCodecRegistries = new ArrayList<>(2);

        public Builder addCodec(MongoCodec<?> mongoCodec) {
            if (codecMap.containsKey(mongoCodec.getEncoderClass())) {
                throw new IllegalArgumentException("duplicate class: " + mongoCodec.getEncoderClass().getName());
            }
            codecMap.put(mongoCodec.getEncoderClass(), mongoCodec);
            return this;
        }

        public Builder addCodecs(MongoCodec<?>... mongoCodecArray) {
            for (MongoCodec<?> mongoCodec : mongoCodecArray) {
                addCodec(mongoCodec);
            }
            return this;
        }

        public Builder addCodecs(Collection<? extends MongoCodec<?>> mongoCodecCollection) {
            for (MongoCodec<?> mongoCodec : mongoCodecCollection) {
                addCodec(mongoCodec);
            }
            return this;
        }

        /** 用于将系统的一些Codec注册进来 */
        public Builder addCodecRegistry(CodecRegistry codecRegistry) {
            moreCodecRegistries.add(Objects.requireNonNull(codecRegistry));
            return this;
        }

        public CodecRegistry build() {
            return new MongoCodecRegistry(this);
        }
    }
}