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

package cn.wjybxx.common.dson;

import java.util.List;

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

    public static DsonCodecException contextError(DsonContextType expected, DsonContextType contextType) {
        return new DsonCodecException(String.format("context error, expected %s, but found %s", expected, contextType));
    }

    public static DsonCodecException contextErrorTopLevel() {
        return new DsonCodecException("context error, current state is TopLevel");
    }

    public static DsonCodecException unexpectedName(int expected, int name) {
        return new DsonCodecException(String.format("The full number of the field does not match, expected %d, but found %d", expected, name));
    }

    public static DsonCodecException dsonTypeMismatch(DsonType expected, DsonType dsonType) {
        return new DsonCodecException(String.format("The dsonType does not match, expected %s, but found %s", expected, dsonType));
    }

    public static DsonCodecException invalidDsonType(List<DsonType> dsonTypeList, DsonType dsonType) {
        return new DsonCodecException(String.format("The dson type is invalid in context, context: %s, type: %s", dsonTypeList, dsonType));
    }

    public static DsonCodecException invalidDsonType(DsonContextType contextType, DsonType dsonType) {
        return new DsonCodecException(String.format("The dson type is invalid in context, context: %s, type: %s", contextType, dsonType));
    }

    public static DsonCodecException unexpectedSubType(byte expected, byte subType) {
        return new DsonCodecException(String.format("Unexpected subType, expected %d, but found %d", expected, subType));
    }

    public static DsonCodecException invalidState(DsonContextType contextType, List<DsonReaderState> expected, DsonReaderState state) {
        return new DsonCodecException(String.format("invalid state, contextType %s, expected %s, but found %s.",
                contextType, expected, state));
    }

    public static DsonCodecException invalidState(DsonContextType contextType, List<DsonWriterState> expected, DsonWriterState state) {
        return new DsonCodecException(String.format("invalid state, contextType %s, expected %s, but found %s.",
                contextType, expected, state));
    }

    public static DsonCodecException bytesRemain(int bytesUntilLimit) {
        return new DsonCodecException("bytes remain " + bytesUntilLimit);
    }
    //

    public static DsonCodecException unsupportedType(Class<?> type) {
        return new DsonCodecException("Unsupported type " + type);
    }

    public static DsonCodecException replaceToNull() {
        return new DsonCodecException("Cant replace an object with Null");
    }

    public static DsonCodecException incompatible(DsonType expected, DsonType dsonType) {
        return new DsonCodecException(String.format("Incompatible data format, expected %s, but found %s", expected, dsonType));
    }

    public static DsonCodecException incompatible(Class<?> declared, DsonType valueType) {
        return new DsonCodecException(String.format("Incompatible data format, declaredType %s, tag %s", declared, valueType));
    }

    public static DsonCodecException incompatible(Class<?> declared, ClassId classId) {
        return new DsonCodecException(String.format("Incompatible data format, declaredType %s, classId %s", declared, classId));
    }
}