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

import cn.wjybxx.common.dson.TypeArgInfo;
import org.bson.BsonDocument;
import org.bson.BsonReader;
import org.bson.BsonWriter;
import org.bson.codecs.Codec;
import org.bson.codecs.DecoderContext;
import org.bson.codecs.EncoderContext;

/**
 * 我们通过{@link BsonDocument}中间体进行编解码的中转
 * ps:在性能不是很关键的地方，{@link BsonDocument}是很好的中间层。
 *
 * @author wjybxx
 * date - 2023/4/17
 */
class CodecAdapter<T> implements Codec<T> {

    final MongoConverter mongoConverter;
    final TypeArgInfo<T> typeArgInfo;
    final Codec<BsonDocument> bsonDocumentCodec;

    public CodecAdapter(MongoConverter mongoConverter, TypeArgInfo<T> typeArgInfo, Codec<BsonDocument> bsonDocumentCodec) {
        this.mongoConverter = mongoConverter;
        this.typeArgInfo = typeArgInfo;
        this.bsonDocumentCodec = bsonDocumentCodec;
    }

    @Override
    public T decode(BsonReader reader, DecoderContext decoderContext) {
        final BsonDocument bsonDocument = bsonDocumentCodec.decode(reader, decoderContext);
        return mongoConverter.read(bsonDocument, typeArgInfo);
    }

    @Override
    public void encode(BsonWriter writer, T value, EncoderContext encoderContext) {
        final BsonDocument bsonDocument = (BsonDocument) mongoConverter.write(value);
        bsonDocumentCodec.encode(writer, bsonDocument, encoderContext);
    }

    @Override
    public Class<T> getEncoderClass() {
        return typeArgInfo.declaredType;
    }
}