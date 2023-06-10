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

import cn.wjybxx.common.dson.document.DocumentConverterUtils;
import cn.wjybxx.common.dson.text.*;
import org.junit.jupiter.api.Test;

import java.io.StringWriter;
import java.util.ArrayList;
import java.util.List;

/**
 * @author wjybxx
 * date - 2023/6/4
 */
public class DsonTextReaderTest {

    private static final String dosnString = """
            --\s
            -- {@MyStruct\s
            -- \tname : wjybxx,
            -- \tage:28,
            -- \t介绍: 这是一段中文而且非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常长 ,
            -- \tintro: "hello world",
            -- \tref1 : {@ref localId: 10001, namespace: 16148b3b4e7b8923d398},
            -- \tref2 : @ref 17630eb4f916148b,
            -- \tbin : [@bin 0, 35df2e75e6a4be9e6f4571c64cb6d08b]
            -- }
            --
            -- {@MyStruct\s
            -- \tname : wjybxx,
            -- \tintro: "hello world",
            -- \tref1 : {@ref localId: 10001, namespace: 16148b3b4e7b8923d398},
            -- \tref2 : @ref 17630eb4f916148b
            --  }
            --
            -- [
            --  [@bin 1, FFFA],
            --  [@ei 1, 10010],
            --  [@eL 1, 10010],
            --  [@es 1, 10010],
            -- ]
            --
            -- [@{compClsName : ei}
            --  [ 1, 0xFFFA],
            --  [ 2, 10010],
            --  [ 3, 10010],
            --  [ 4, 10010],
            -- ]
            """;

    @Test
    void test() {
        List<DsonValue> topObjects = new ArrayList<>(4);
        try (DsonScanner scanner = new DsonScanner(new DsonStringBuffer(dosnString))) {
            DsonDocReader reader = new DsonTextReader(16, scanner);
            DsonValue dsonValue;

            while ((dsonValue = DocumentConverterUtils.readTopDsonValue(reader)) != null) {
                topObjects.add(dsonValue);
            }
        }
        topObjects.forEach(System.out::println);

        StringWriter stringWriter = new StringWriter();
        DsonTextWriterSettings settings = DsonTextWriterSettings.newBuilder()
                .setSoftLineLength(30)
                .setUnicodeChar(false)
                .build();

        DsonValue topObj = topObjects.get(0);
        try (DsonTextWriter writer = new DsonTextWriter(16, stringWriter, settings)) {
            if (topObj instanceof DsonObject) {
                @SuppressWarnings("unchecked") DsonObject<String> dsonObject = (DsonObject<String>) topObj;
                writer.writeStartObject(ObjectStyle.INDENT);
                dsonObject.forEach((name, value) -> {
                    switch (value.getDsonType()) {
                        case INT32 -> writer.writeInt64(name, value.asInt32().getValue(), WireType.VARINT, true);
                        case INT64 -> writer.writeInt64(name, value.asInt64().getValue(), WireType.VARINT, true);
                        case FLOAT -> writer.writeFloat(name, value.asFloat().getValue(), true);
                        case DOUBLE -> writer.writeDouble(name, value.asDouble().getValue());
                        case BOOLEAN -> writer.writeBoolean(name, value.asBoolean().getValue());
                        case NULL -> writer.writeNull(name);
                        case STRING -> {
                            if (name.equals("介绍")){
                                writer.writeString(name, value.asString().getValue(), StringStyle.TEXT);
                            } else {
                                writer.writeString(name, value.asString().getValue(), StringStyle.AUTO);
                            }
                        }
                        case BINARY -> writer.writeBinary(name, value.asBinary());
                        case EXT_INT32 -> writer.writeExtInt32(name, value.asExtInt32(), WireType.VARINT);
                        case EXT_INT64 -> writer.writeExtInt64(name, value.asExtInt64(), WireType.VARINT);
                        case EXT_STRING -> writer.writeExtString(name, value.asExtString(), StringStyle.AUTO);
                        case REFERENCE -> writer.writeRef(name, value.asReference().getValue());
                    }
                });
                writer.writeEndObject();
                writer.flush();
            }
            System.out.println(stringWriter.toString());
        }

    }
}