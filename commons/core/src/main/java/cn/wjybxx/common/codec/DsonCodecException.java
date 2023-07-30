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

package cn.wjybxx.common.codec;

import cn.wjybxx.dson.DsonType;

/**
 * @author wjybxx
 * date - 2023/4/22
 */
public class DsonCodecException extends RuntimeException {

    public DsonCodecException() {
    }

    public DsonCodecException(String message) {
        super(message);
    }

    public DsonCodecException(String message, Throwable cause) {
        super(message, cause);
    }

    public DsonCodecException(Throwable cause) {
        super(cause);
    }

    public DsonCodecException(String message, Throwable cause, boolean enableSuppression, boolean writableStackTrace) {
        super(message, cause, enableSuppression, writableStackTrace);
    }

    public static DsonCodecException wrap(Exception e) {
        if (e instanceof DsonCodecException) {
            return (DsonCodecException) e;
        }
        return new DsonCodecException(e);
    }

    //
    public static DsonCodecException recursionLimitExceeded() {
        return new DsonCodecException("Object had too many levels of nesting.");
    }

    public static DsonCodecException unexpectedName(int expected, int name) {
        return new DsonCodecException(String.format("The number of the field does not match, expected %d, but found %d", expected, name));
    }

    public static DsonCodecException unexpectedName(String expected, String name) {
        return new DsonCodecException(String.format("The name of the field does not match, expected %s, but found %s", expected, name));
    }

    public static DsonCodecException unexpectedSubType(int expected, int subType) {
        return new DsonCodecException(String.format("Unexpected subType, expected %d, but found %d", expected, subType));
    }

    public static DsonCodecException unsupportedType(Class<?> type) {
        return new DsonCodecException("Can't find a codec for " + type);
    }

    public static DsonCodecException unsupportedKeyType(Class<?> type) {
        return new DsonCodecException("Can't find a codec for " + type + ", or key is not EnumLite");
    }

    public static DsonCodecException enumAbsent(Class<?> declared, int number) {
        return new DsonCodecException(String.format("EnumLite is absent, declared: %s, number: %d", declared, number));
    }

    public static DsonCodecException incompatible(Class<?> declared, DsonType dsonType) {
        return new DsonCodecException(String.format("Incompatible data format, declaredType %s, dsonType %s", declared, dsonType));
    }

    public static DsonCodecException incompatible(DsonType expected, DsonType dsonType) {
        return new DsonCodecException(String.format("Incompatible data format, expected %s, dsonType %s", expected, dsonType));
    }

    public static <T> DsonCodecException incompatible(Class<?> declared, T classId) {
        return new DsonCodecException(String.format("Incompatible data format, declaredType %s, classId %s", declared, classId));
    }
}