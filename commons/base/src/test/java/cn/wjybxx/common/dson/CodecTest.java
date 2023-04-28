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

import cn.wjybxx.common.dson.CodecStructs.MyStruct;
import cn.wjybxx.common.dson.CodecStructs.NestStruct;
import cn.wjybxx.common.dson.binary.DefaultBinaryConverter;
import cn.wjybxx.common.dson.codec.ConvertOptions;
import cn.wjybxx.common.dson.document.DefaultDocumentConverter;
import org.apache.commons.lang3.RandomStringUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.*;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public class CodecTest {

    private MyStruct myStruct;

    @BeforeEach
    void setUp() {
        Random random = new Random();
        NestStruct nestStruct = new NestStruct(random.nextInt(), random.nextLong(),
                random.nextFloat() * 100, random.nextDouble() * 100);

        MyStruct myStruct = new MyStruct(random.nextInt(), random.nextLong(),
                random.nextFloat() * 100, random.nextDouble() * 100,
                random.nextBoolean(),
                RandomStringUtils.random(10),
                new byte[5],
                new HashMap<>(),
                new ArrayList<>(),
                nestStruct);

        random.nextBytes(myStruct.bytes);

        myStruct.list.add(RandomStringUtils.random(5));
        myStruct.list.add(RandomStringUtils.random(7));

        myStruct.map.put(String.valueOf(myStruct.intVal), random.nextFloat() * 100);
        myStruct.map.put(String.valueOf(myStruct.longVal), random.nextDouble() * 100);

        this.myStruct = myStruct;
    }

    /** 基础读写测试 */
    @Test
    void binCodecTest() {
        DefaultBinaryConverter converter = DefaultBinaryConverter.newInstance(Set.of(),
                List.of(new CodecStructs.MyStructCodec()),
                Map.of(MyStruct.class, new BinClassId(1, 1)),
                ConvertOptions.DEFAULT);

        MyStruct clonedObject = converter.cloneObject(myStruct, TypeArgInfo.of(MyStruct.class));
        Assertions.assertEquals(myStruct, clonedObject);
    }

    @Test
    void docCodecTest() {
        DefaultDocumentConverter converter = DefaultDocumentConverter.newInstance(Set.of(),
                List.of(new CodecStructs.MyStructCodec()),
                Map.of(MyStruct.class, new DocClassId("MyStruct")),
                ConvertOptions.DEFAULT);

        MyStruct clonedObject = converter.cloneObject(myStruct, TypeArgInfo.of(MyStruct.class));
        Assertions.assertEquals(myStruct, clonedObject);
    }


}