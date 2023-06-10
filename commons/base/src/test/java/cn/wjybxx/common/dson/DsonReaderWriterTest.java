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

import cn.wjybxx.common.dson.binary.BinaryConverterUtils;
import cn.wjybxx.common.dson.document.DocumentConverterUtils;
import cn.wjybxx.common.dson.io.DsonInput;
import cn.wjybxx.common.dson.io.DsonInputs;
import cn.wjybxx.common.dson.io.DsonOutput;
import cn.wjybxx.common.dson.io.DsonOutputs;
import cn.wjybxx.common.dson.text.ObjectStyle;
import cn.wjybxx.common.dson.text.StringStyle;
import org.apache.commons.lang3.RandomUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.util.ArrayList;
import java.util.List;

/**
 * @author wjybxx
 * date - 2023/6/3
 */
public class DsonReaderWriterTest {

    @Test
    void testDoc() throws InterruptedException {
        final byte[] buffer = new byte[4096];
        final int loop = 3;

        List<DsonObject<String>> srcList = new ArrayList<>(loop);
        List<DsonObject<String>> copiedList = new ArrayList<>(loop);

        int totalBytesWritten;
        try (DsonOutput dsonOutput = DsonOutputs.newInstance(buffer)) {
            DsonDocWriter writer = new DefaultDsonDocWriter(16, dsonOutput);
            for (int i = 0; i < loop; i++) {
                DsonObject<String> obj1 = new MutableDsonObject<>(6);
                obj1.append("name", new DsonString("wjybxx"))
                        .append("age", new DsonInt32(RandomUtils.nextInt(28, 32)))
                        .append("intro", new DsonString("www.wjybxx.cn"))
                        .append("time", new DsonInt64(System.currentTimeMillis()));
                srcList.add(obj1);

                writer.writeStartObject(ObjectStyle.INDENT);
                obj1.forEach((name, value) -> {
                    switch (value.getDsonType()) {
                        case INT32 -> writer.writeInt32(name, value.asInt32().getValue(), WireType.VARINT, false);
                        case INT64 -> writer.writeInt64(name, value.asInt64().getValue(), WireType.VARINT, false);
                        case STRING -> writer.writeString(name, value.asString().getValue(), StringStyle.AUTO);
                    }
                });
                writer.writeEndObject();
                Thread.sleep(50); // 让数值有所不同
            }
            totalBytesWritten = dsonOutput.position();
        }

        try (DsonInput dsonInput = DsonInputs.newInstance(buffer, 0, totalBytesWritten)) {
            DsonDocReader reader = new DefaultDsonDocReader(16, dsonInput);
            DsonValue dsonValue;
            while ((dsonValue = DocumentConverterUtils.readTopDsonValue(reader)) != null) {
                copiedList.add(dsonValue.asObject());
            }
        }

        Assertions.assertEquals(srcList, copiedList);
    }

    @Test
    void testBin() throws InterruptedException {
        final byte[] buffer = new byte[4096];
        final int loop = 3;

        List<DsonObject<FieldNumber>> srcList = new ArrayList<>(loop);
        List<DsonObject<FieldNumber>> copiedList = new ArrayList<>(loop);

        int totalBytesWritten;
        try (DsonOutput dsonOutput = DsonOutputs.newInstance(buffer)) {
            DsonBinWriter writer = new DefaultDsonBinWriter(16, dsonOutput);
            for (int i = 0; i < loop; i++) {
                DsonObject<FieldNumber> obj1 = new MutableDsonObject<>(6);
                obj1.append(FieldNumber.of(0, 0), new DsonString("wjybxx"))
                        .append(FieldNumber.of(0, 1), new DsonInt32(RandomUtils.nextInt(28, 32)))
                        .append(FieldNumber.of(0, 2), new DsonString("www.wjybxx.cn"))
                        .append(FieldNumber.of(0, 3), new DsonInt64(System.currentTimeMillis()));
                srcList.add(obj1);

                writer.writeStartObject(ObjectStyle.INDENT);
                obj1.forEach((name, value) -> {
                    switch (value.getDsonType()) {
                        case INT32 ->
                                writer.writeInt32(name.getFullNumber(), value.asInt32().getValue(), WireType.VARINT, false);
                        case INT64 ->
                                writer.writeInt64(name.getFullNumber(), value.asInt64().getValue(), WireType.VARINT, false);
                        case STRING -> writer.writeString(name.getFullNumber(), value.asString().getValue(), StringStyle.AUTO);
                    }
                });
                writer.writeEndObject();
                Thread.sleep(50); // 让数值有所不同
            }
            totalBytesWritten = dsonOutput.position();
        }

        try (DsonInput dsonInput = DsonInputs.newInstance(buffer, 0, totalBytesWritten)) {
            DsonBinReader reader = new DefaultDsonBinReader(16, dsonInput);
            DsonValue dsonValue;
            while ((dsonValue = BinaryConverterUtils.readTopDsonValue(reader)) != null) {
                copiedList.add(dsonValue.asBinObject());
            }
        }

        Assertions.assertEquals(srcList, copiedList);
    }
}