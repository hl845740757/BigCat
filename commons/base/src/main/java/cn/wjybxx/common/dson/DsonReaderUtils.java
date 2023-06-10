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

package cn.wjybxx.common.dson;

import cn.wjybxx.common.annotation.Internal;
import cn.wjybxx.common.dson.io.Chunk;
import cn.wjybxx.common.dson.io.DsonInput;
import cn.wjybxx.common.dson.io.DsonOutput;
import cn.wjybxx.common.dson.types.ObjectRef;
import com.google.protobuf.MessageLite;
import com.google.protobuf.Parser;

import java.util.List;

/**
 * @author wjybxx
 * date - 2023/5/31
 */
@Internal
public class DsonReaderUtils {

    /** 支持读取为bytes和直接写入bytes的数据类型 */
    private static final List<DsonType> VALUE_BYTES_TYPES = List.of(DsonType.STRING,
            DsonType.BINARY, DsonType.ARRAY, DsonType.OBJECT);

    // region 字节流

    public static void writeBinary(DsonOutput output, DsonBinary binary) {
        output.writeFixed32(1 + binary.getData().length);
        output.writeRawByte(binary.getType());
        output.writeRawBytes(binary.getData());
    }

    public static void writeBinary(DsonOutput output, int type, Chunk chunk) {
        output.writeFixed32(1 + chunk.getLength());
        output.writeRawByte(type);
        output.writeRawBytes(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
    }

    public static DsonBinary readDsonBinary(DsonInput input) {
        int size = input.readFixed32();
        return new DsonBinary(
                input.readRawByte(),
                input.readRawBytes(size - 1));
    }

    public static void writeExtInt32(DsonOutput output, DsonExtInt32 extInt32, WireType wireType) {
        output.writeUint32(extInt32.getType());
        wireType.writeInt32(output, extInt32.getValue());
    }

    public static DsonExtInt32 readDsonExtInt32(DsonInput input, WireType wireType) {
        return new DsonExtInt32(
                input.readUint32(),
                wireType.readInt32(input));
    }

    public static void writeExtInt64(DsonOutput output, DsonExtInt64 extInt64, WireType wireType) {
        output.writeUint32(extInt64.getType());
        wireType.writeInt64(output, extInt64.getValue());
    }

    public static DsonExtInt64 readDsonExtInt64(DsonInput input, WireType wireType) {
        return new DsonExtInt64(
                input.readUint32(),
                wireType.readInt64(input));
    }

    public static void writeExtString(DsonOutput output, DsonExtString extString) {
        output.writeUint32(extString.getType());
        output.writeString(extString.getValue());
    }

    public static DsonExtString readDsonExtString(DsonInput input) {
        return new DsonExtString(
                input.readUint32(),
                input.readString());
    }

    public static void writeRef(DsonOutput output, ObjectRef objectRef) {
        output.writeString(objectRef.hasNamespace() ? objectRef.getNamespace() : "");
        output.writeString(objectRef.hasLocalId() ? objectRef.getLocalId() : "");
        output.writeUint32(objectRef.getType());
        output.writeUint32(objectRef.getPolicy());
    }

    public static ObjectRef readRef(DsonInput input) {
        return new ObjectRef(
                input.readString(),
                input.readString(),
                input.readUint32(),
                input.readUint32());
    }

    public static void writeMessage(DsonOutput output, int binaryType, MessageLite messageLite) {
        int preWritten = output.position();
        output.writeFixed32(0);
        output.writeRawByte(binaryType);
        output.writeMessageNoSize(messageLite);
        output.setFixedInt32(preWritten, output.position() - preWritten - 4);
    }

    public static <T> T readMessage(DsonInput input, int binaryType, Parser<T> parser) {
        int size = input.readFixed32();
        int oldLimit = input.pushLimit(size);
        byte subType = input.readRawByte();
        if (subType != binaryType) {
            throw DsonCodecException.unexpectedSubType(binaryType, subType);
        }
        T value = input.readMessageNoSize(parser);
        input.popLimit(oldLimit);
        return value;
    }

    public static void writeValueBytes(DsonOutput output, DsonType type, byte[] data) {
        if (type == DsonType.STRING) {
            output.writeUint32(data.length);
        } else {
            output.writeFixed32(data.length);
        }
        output.writeRawBytes(data);
    }

    public static byte[] readValueAsBytes(DsonInput input, DsonType dsonType) {
        int size;
        if (dsonType == DsonType.STRING) {
            size = input.readUint32();
        } else {
            size = input.readFixed32();
        }
        return input.readRawBytes(size);
    }

    public static void skipValue(DsonInput input,
                                 DsonContextType contextType, DsonType dsonType, WireType wireType) {
        int skip;
        switch (dsonType) {
            case FLOAT -> skip = 4;
            case DOUBLE -> skip = 8;
            case BOOLEAN -> skip = 1;
            case NULL -> skip = 0;

            case INT32 -> {
                wireType.readInt32(input);
                return;
            }
            case INT64 -> {
                wireType.readInt64(input);
                return;
            }
            case STRING -> {
                skip = input.readUint32();  // string长度
            }
            case EXT_INT32 -> {
                input.readUint32(); // 子类型
                wireType.readInt32(input);
                return;
            }
            case EXT_INT64 -> {
                input.readUint32(); // 子类型
                wireType.readInt64(input);
                return;
            }
            case EXT_STRING -> {
                input.readUint32(); // 子类型
                skip = input.readUint32();
            }
            case REFERENCE -> {
                input.readUint64();
                skip = input.readUint32(); // string长度
                input.skipRawBytes(skip);
                input.readUint32();
                input.readUint32();
                return;
            }
            case BINARY, ARRAY, OBJECT, HEADER -> {
                skip = input.readFixed32();
            }
            default -> {
                throw DsonCodecException.invalidDsonType(contextType, dsonType);
            }
        }
        if (skip > 0) {
            input.skipRawBytes(skip);
        }
    }

    public static void skipToEndOfObject(DsonInput input) {
        int size = input.getBytesUntilLimit();
        if (size > 0) {
            input.skipRawBytes(size);
        }
    }

    public static void checkReadValueAsBytes(DsonType dsonType) {
        if (!VALUE_BYTES_TYPES.contains(dsonType)) {
            throw DsonCodecException.invalidDsonType(VALUE_BYTES_TYPES, dsonType);
        }
    }

    public static void checkWriteValueAsBytes(DsonType dsonType) {
        if (!VALUE_BYTES_TYPES.contains(dsonType)) {
            throw DsonCodecException.invalidDsonType(VALUE_BYTES_TYPES, dsonType);
        }
    }

    public static DsonReaderGuide whatShouldIDo(DsonContextType contextType, DsonReaderState state) {
        if (contextType == DsonContextType.TOP_LEVEL) {
            if (state == DsonReaderState.END_OF_FILE) {
                return DsonReaderGuide.CLOSE;
            }
            if (state == DsonReaderState.VALUE) {
                return DsonReaderGuide.READ_VALUE;
            }
            return DsonReaderGuide.READ_TYPE;
        } else {
            return switch (state) {
                case TYPE -> DsonReaderGuide.READ_TYPE;
                case VALUE -> DsonReaderGuide.READ_VALUE;
                case NAME -> DsonReaderGuide.READ_NAME;
                case WAIT_START_OBJECT ->
                        contextType == DsonContextType.ARRAY ? DsonReaderGuide.START_ARRAY : DsonReaderGuide.START_OBJECT;
                case WAIT_END_OBJECT ->
                        contextType == DsonContextType.ARRAY ? DsonReaderGuide.END_ARRAY : DsonReaderGuide.END_OBJECT;
                case INITIAL, END_OF_FILE -> throw new AssertionError("invalid state " + state);
            };
        }
    }

    // endregion
}