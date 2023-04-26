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
import cn.wjybxx.common.dson.document.DocumentCodecRegistry;
import cn.wjybxx.common.dson.document.DocumentConverter;
import cn.wjybxx.common.dson.document.TypeKeyMode;
import cn.wjybxx.common.dson.document.TypeNameRegistry;
import org.bson.BsonDocument;
import org.bson.BsonValue;
import org.bson.codecs.Codec;
import org.bson.codecs.configuration.CodecRegistry;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * 基于Mongo的Bson的编解码器
 * 1.输出只有两种：{@link org.bson.BsonArray}和{@link org.bson.BsonDocument}
 * 2.可以通过{@link #createCodecRegistry(Class, CodecRegistry)}创建一个用于解码mongo集合中对象的{@link CodecRegistry。
 *
 * @author wjybxx
 * date - 2023/4/17
 */
public class MongoConverter implements DocumentConverter<BsonValue> {

    private final TypeNameRegistry classNameRegistry;
    private final TypeNameRegistry typeAliasRegistry;
    final DocumentCodecRegistry codecRegistry;
    final int recursionLimit;
    private final TypeKeyMode typeKeyMode;

    // 当前使用的键和注册表
    private final String typeKey;
    private final TypeNameRegistry typeNameRegistry;

    private MongoConverter(TypeNameRegistry classNameRegistry, TypeNameRegistry typeAliasRegistry, DocumentCodecRegistry codecRegistry,
                           int recursionLimit, TypeKeyMode typeKeyMode) {
        this.classNameRegistry = classNameRegistry;
        this.typeAliasRegistry = typeAliasRegistry;
        this.codecRegistry = codecRegistry;
        this.recursionLimit = recursionLimit;
        this.typeKeyMode = typeKeyMode;

        this.typeKey = typeKeyMode.typeKey;
        this.typeNameRegistry = typeKeyMode == TypeKeyMode.CLASS_NAME ? classNameRegistry : typeAliasRegistry;
    }

    @Nonnull
    @Override
    public String getTypeKey() {
        return typeKey;
    }

    @Override
    public DocumentCodecRegistry codecRegistry() {
        return codecRegistry;
    }

    @Override
    public TypeNameRegistry typeNameRegistry() {
        return typeNameRegistry;
    }

    @Nonnull
    @Override
    public BsonValue write(Object value, @Nonnull TypeArgInfo<?> typeArgInfo) {
        return null;
    }

    @Override
    public <U> U read(BsonValue source, TypeArgInfo<U> typeArgInfo) {
        return null;
    }

    /**
     * 创建一个可以编解码Mongo集合里对象的{@link CodecRegistry}
     * 通过这种方式，用户可以脱离对Mongo原生Codec的依赖。
     *
     * @param documentType         mongoDB集合里的对象类型
     * @param defaultCodecRegistry 通常应该是MongoDBClientSetting里的默认Registry
     */
    public CodecRegistry createCodecRegistry(Class<?> documentType, CodecRegistry defaultCodecRegistry) {
        if (codecRegistry.get(documentType) == null) {
            throw new IllegalArgumentException("unsupported document type: " + documentType);
        }
        Codec<BsonDocument> bsonDocumentCodec = defaultCodecRegistry.get(BsonDocument.class);
        CodecAdapter<?> codecAdapter = new CodecAdapter<>(this, TypeArgInfo.of(documentType), bsonDocumentCodec);
        return new CodecRegistryAdapter(defaultCodecRegistry, codecAdapter);
    }

    /** 创建一个使用给定类型键的converter，其它数据共享 */
    public MongoConverter withTypeKeyMode(TypeKeyMode typeKeyMode) {
        Objects.requireNonNull(typeKeyMode);
        if (this.typeKeyMode == typeKeyMode) {
            return this;
        } else {
            return new MongoConverter(classNameRegistry, typeAliasRegistry, codecRegistry, recursionLimit, typeKeyMode);
        }
    }

}