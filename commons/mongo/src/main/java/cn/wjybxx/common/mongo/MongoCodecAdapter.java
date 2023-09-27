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

import org.bson.BsonReader;
import org.bson.BsonWriter;
import org.bson.codecs.Codec;
import org.bson.codecs.DecoderContext;
import org.bson.codecs.EncoderContext;

/**
 * @author wjybxx
 * date - 2023/4/17
 */
public class MongoCodecAdapter<T> implements Codec<T> {

    private final MongoCodecRegistry codecRegistry;
    private final MongoCodec<T> delegated;

    MongoCodecAdapter(MongoCodecRegistry codecRegistry, MongoCodec<T> delegated) {
        this.codecRegistry = codecRegistry;
        this.delegated = delegated;
    }

    @Override
    public Class<T> getEncoderClass() {
        return delegated.getEncoderClass();
    }

    @Override
    public void encode(BsonWriter writer, T value, EncoderContext encoderContext) {
        delegated.encode(writer, value, codecRegistry, encoderContext);
    }

    @Override
    public T decode(BsonReader reader, DecoderContext decoderContext) {
        return delegated.decode(reader, codecRegistry, decoderContext);
    }

}