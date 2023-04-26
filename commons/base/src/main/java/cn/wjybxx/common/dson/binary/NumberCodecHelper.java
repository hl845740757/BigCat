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

package cn.wjybxx.common.dson.binary;

import cn.wjybxx.common.dson.*;

/**
 * 1.int扩展之间可以相互转换，当int的扩展不可以直接转换为其它数值类型
 * 2.long扩展之间可以相互转换，但long的扩展不可直接转换为其它数值类型
 * 3.String扩展之间也可以相互转换
 *
 * @author wjybxx
 * date - 2023/4/17
 */
class NumberCodecHelper {

    static DsonType readOrGetDsonType(DsonBinReader reader) {
        if (reader.isAtType()) {
            return reader.readDsonType();
        } else {
            return reader.getCurrentDsonType();
        }
    }

    static int readInt(DsonBinReader reader, int name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case INT32 -> reader.readInt32(name);
            case INT64 -> (int) reader.readInt64(name);
            case FLOAT -> (int) reader.readFloat(name);
            case DOUBLE -> (int) reader.readDouble(name);
            case BOOLEAN -> reader.readBoolean(name) ? 1 : 0;
            case NULL -> {
                reader.readNull(name);
                yield 0;
            }
            case EXT_INT32 -> reader.readExtInt32(name).getValue();
            default -> throw DsonCodecException.incompatible(Integer.class, dsonType);
        };
    }

    static long readLong(DsonBinReader reader, int name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case INT32 -> reader.readInt32(name);
            case INT64 -> reader.readInt64(name);
            case FLOAT -> (long) reader.readFloat(name);
            case DOUBLE -> (long) reader.readDouble(name);
            case BOOLEAN -> reader.readBoolean(name) ? 1 : 0;
            case NULL -> {
                reader.readNull(name);
                yield 0;
            }
            case EXT_INT64 -> reader.readExtInt64(name).getValue();
            default -> throw DsonCodecException.incompatible(Long.class, dsonType);
        };
    }

    static float readFloat(DsonBinReader reader, int name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case INT32 -> reader.readInt32(name);
            case INT64 -> reader.readInt64(name);
            case FLOAT -> reader.readFloat(name);
            case DOUBLE -> (float) reader.readDouble(name);
            case BOOLEAN -> reader.readBoolean(name) ? 1 : 0;
            case NULL -> {
                reader.readNull(name);
                yield 0;
            }
            default -> throw DsonCodecException.incompatible(Float.class, dsonType);
        };
    }

    static double readDouble(DsonBinReader reader, int name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case INT32 -> reader.readInt32(name);
            case INT64 -> reader.readInt64(name);
            case FLOAT -> reader.readFloat(name);
            case DOUBLE -> reader.readDouble(name);
            case BOOLEAN -> reader.readBoolean(name) ? 1 : 0;
            case NULL -> {
                reader.readNull(name);
                yield 0;
            }
            default -> throw DsonCodecException.incompatible(Double.class, dsonType);
        };
    }

    static boolean readBool(DsonBinReader reader, int name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case INT32 -> reader.readInt32(name) != 0;
            case INT64 -> reader.readInt64(name) != 0;
            case FLOAT -> reader.readFloat(name) != 0;
            case DOUBLE -> reader.readDouble(name) != 0;
            case BOOLEAN -> reader.readBoolean(name);
            case NULL -> {
                reader.readNull(name);
                yield false;
            }
            default -> throw DsonCodecException.incompatible(Boolean.class, dsonType);
        };
    }

    static String readString(DsonBinReader reader, int name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case STRING -> reader.readString(name);
            case EXT_STRING -> reader.readExtString(name).getValue();
            case NULL -> {
                reader.readNull(name);
                yield null;
            }
            default -> throw DsonCodecException.incompatible(String.class, dsonType);
        };
    }

    static DsonExtString readExtString(DsonBinReader reader, int name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case STRING -> new DsonExtString((byte) 0, reader.readString(name));
            case EXT_STRING -> reader.readExtString(name);
            case NULL -> {
                reader.readNull(name);
                yield null;
            }
            default -> throw DsonCodecException.incompatible(DsonExtString.class, dsonType);
        };
    }

    static DsonExtInt32 readExtInt32(DsonBinReader reader, int name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case INT32 -> new DsonExtInt32((byte) 0, reader.readInt32(name));
            case EXT_INT32 -> reader.readExtInt32(name);
            case NULL -> {
                reader.readNull(name);
                yield null;
            }
            default -> throw DsonCodecException.incompatible(DsonExtInt64.class, dsonType);
        };
    }

    static DsonExtInt64 readExtInt64(DsonBinReader reader, int name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case INT64 -> new DsonExtInt64((byte) 0, reader.readInt64(name));
            case EXT_INT64 -> reader.readExtInt64(name);
            case NULL -> {
                reader.readNull(name);
                yield null;
            }
            default -> throw DsonCodecException.incompatible(DsonExtInt64.class, dsonType);
        };
    }

    static DsonBinary readBinary(DsonBinReader reader, int name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case BINARY -> reader.readBinary(name);
            case STRING, ARRAY, OBJECT -> new DsonBinary((byte) 0, reader.readValueAsBytes(name));
            case NULL -> {
                reader.readNull(name);
                yield null;
            }
            default -> throw DsonCodecException.incompatible(DsonBinary.class, dsonType);
        };
    }

    //
    static Object readPrimitive(DsonBinReader reader, int name, Class<?> declared) {
        if (declared == int.class) {
            return readInt(reader, name);
        }
        if (declared == long.class) {
            return readLong(reader, name);
        }
        if (declared == float.class) {
            return readFloat(reader, name);
        }
        if (declared == double.class) {
            return readDouble(reader, name);
        }
        if (declared == boolean.class) {
            return readBool(reader, name);
        }
        if (declared == short.class) {
            return (short) readInt(reader, name);
        }
        if (declared == char.class) {
            return (char) readInt(reader, name);
        }
        if (declared == byte.class) {
            return (byte) readInt(reader, name);
        }
        throw new AssertionError();
    }
}