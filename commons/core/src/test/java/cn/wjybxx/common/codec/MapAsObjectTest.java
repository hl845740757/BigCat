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

import cn.wjybxx.common.OptionalBool;
import cn.wjybxx.common.codec.document.DefaultDocumentConverter;
import cn.wjybxx.common.codec.document.DocumentConverter;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

/**
 * 测试Map按照Object类型编码
 *
 * @author wjybxx
 * date - 2023/9/13
 */
public class MapAsObjectTest {

    @Test
    void test() {
        ConvertOptions options = ConvertOptions.newBuilder().setEncodeMapAsObject(OptionalBool.TRUE).build();
        DocumentConverter converter = DefaultDocumentConverter.newInstance(
                List.of(),
                TypeMetaRegistries.fromMetas(),
                options);

        Map<String, Object> map = new LinkedHashMap<>();
        map.put("one", "1");
        map.put("two", 2.0); // 默认解码是double

        String dson = converter.writeAsDson(map);
//        System.out.println(dson);

        @SuppressWarnings("unchecked") LinkedHashMap<String, Object> copied = converter.readFromDson(dson, TypeArgInfo.STRING_LINKED_HASHMAP);
        Assertions.assertEquals(map, copied);
    }
}
