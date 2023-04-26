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

import org.bson.BsonType;

/**
 * @author wjybxx
 * date - 2023/4/17
 */
public class MongoCodecException extends RuntimeException {

    public MongoCodecException() {
    }

    public MongoCodecException(String message) {
        super(message);
    }

    public MongoCodecException(String message, Throwable cause) {
        super(message, cause);
    }

    public MongoCodecException(Throwable cause) {
        super(cause);
    }

    public MongoCodecException(String message, Throwable cause, boolean enableSuppression, boolean writableStackTrace) {
        super(message, cause, enableSuppression, writableStackTrace);
    }

    static MongoCodecException wrap(Exception e) {
        if (e instanceof MongoCodecException) {
            return (MongoCodecException) e;
        }
        return new MongoCodecException(e);
    }

    static MongoCodecException incompatible(Class<?> declared, BsonType valueType) {
        return new MongoCodecException(String.format("Incompatible data format, declaredType %s, tag %s", declared, valueType));
    }

    static MongoCodecException incompatible(Class<?> declared, String typeName) {
        return new MongoCodecException(String.format("Incompatible data format, declaredType %s, typeName %s", declared, typeName));
    }

    static MongoCodecException incompatible(BsonType expected, BsonType valueType) {
        return new MongoCodecException(String.format("Incompatible data format, expected %s, tag %s", expected, valueType));
    }

    static MongoCodecException recursionLimitExceeded() {
        return new MongoCodecException("Object had too many levels of nesting.");
    }

    static MongoCodecException unsupportedType(Class<?> type) {
        return new MongoCodecException("Unsupported type " + type);
    }

    static MongoCodecException contextError() {
        return new MongoCodecException("read write state error");
    }

    static MongoCodecException nameAbsent() {
        return new MongoCodecException("missing object name");
    }

    static MongoCodecException replaceToNull() {
        return new MongoCodecException("cannot replace an object with Null");
    }

    static MongoCodecException replaceName() {
        return new MongoCodecException("cannot replace the name of an object");
    }
}