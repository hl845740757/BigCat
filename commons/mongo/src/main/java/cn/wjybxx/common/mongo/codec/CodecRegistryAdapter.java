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

package cn.wjybxx.common.mongo.codec;

import org.bson.codecs.Codec;
import org.bson.codecs.configuration.CodecConfigurationException;
import org.bson.codecs.configuration.CodecRegistry;

/**
 * 为{@link MongoConverter}提供编解码Mongo集合里对象
 *
 * @author wjybxx
 * date - 2023/4/17
 */
class CodecRegistryAdapter implements CodecRegistry {

    final CodecRegistry defaultCodecRegistry;
    final Codec<?> documentCodec;

    public CodecRegistryAdapter(CodecRegistry defaultCodecRegistry, Codec<?> documentCodec) {
        this.defaultCodecRegistry = defaultCodecRegistry;
        this.documentCodec = documentCodec;
    }

    //Mongo的接口实在糟糕...
    @Override
    public <U> Codec<U> get(Class<U> clazz, CodecRegistry registry) {
        try {
            return get(clazz);
        } catch (CodecConfigurationException ignore) {
            return null;
        }
    }

    @Override
    public <U> Codec<U> get(Class<U> clazz) {
        if (clazz == documentCodec.getEncoderClass()) {
            return (Codec<U>) documentCodec;
        }
        return defaultCodecRegistry.get(clazz);
    }
    //
}