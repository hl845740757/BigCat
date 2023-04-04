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

package cn.wjybxx.bigcat.common.pb.codec;

/**
 * @author wjybxx
 * date 2023/4/2
 */
public class ProtobufCodecException extends RuntimeException {

    public ProtobufCodecException() {
    }

    public ProtobufCodecException(String message) {
        super(message);
    }

    public ProtobufCodecException(String message, Throwable cause) {
        super(message, cause);
    }

    public ProtobufCodecException(Throwable cause) {
        super(cause);
    }

    public ProtobufCodecException(String message, Throwable cause, boolean enableSuppression, boolean writableStackTrace) {
        super(message, cause, enableSuppression, writableStackTrace);
    }

    static ProtobufCodecException wrap(Exception e) {
        if (e instanceof ProtobufCodecException) {
            return (ProtobufCodecException) e;
        }
        return new ProtobufCodecException(e);
    }

    static ProtobufCodecException incompatible(Class<?> declared, BinaryValueType valueType) {
        return new ProtobufCodecException(String.format("Incompatible data format, declaredType %s, tag %s", declared, valueType));
    }

    static ProtobufCodecException incompatible(Class<?> declared, long typeId) {
        return new ProtobufCodecException(String.format("Incompatible data format, declaredType %s, typeId %s", declared, typeId));
    }

    static ProtobufCodecException incompatible(BinaryValueType expected, BinaryValueType valueType) {
        return new ProtobufCodecException(String.format("Incompatible data format, expected %s, tag %s", expected, valueType));
    }

    static ProtobufCodecException invalidTag(BinaryValueType valueType) {
        return new ProtobufCodecException("InputStream contained an invalid tag " + valueType);
    }

    static ProtobufCodecException recursionLimitExceeded() {
        return new ProtobufCodecException("Object had too many levels of nesting.");
    }

    static ProtobufCodecException unsupportedType(Class<?> type) {
        return new ProtobufCodecException("Unsupported type " + type);
    }

    static ProtobufCodecException contextError() {
        return new ProtobufCodecException("read write state error");
    }
}