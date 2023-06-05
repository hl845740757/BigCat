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
import cn.wjybxx.common.dson.text.DsonLinesBuffer;
import cn.wjybxx.common.dson.text.DsonScanner;
import cn.wjybxx.common.dson.text.DsonTextReader;
import cn.wjybxx.common.dson.text.JsonBuffer;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

/**
 * @author wjybxx
 * date - 2023/6/5
 */
public class Json2DsonTest {

    private static final String jsonString = """
            {
             "name":"wjybxx",
             "age" : 28
            }
            """;

    @Test
    void test() {
        DsonValue dsonValue;
        try (DsonTextReader reader = new DsonTextReader(16, new DsonScanner(DsonLinesBuffer.ofJson(jsonString)))) {
            dsonValue = DocumentConverterUtils.readTopDsonValue(reader);
            Assertions.assertInstanceOf(DsonObject.class, dsonValue);
        }

        DsonValue dsonValue2;
        try (DsonTextReader reader = new DsonTextReader(16, new DsonScanner(new JsonBuffer(jsonString)))) {
            dsonValue2 = DocumentConverterUtils.readTopDsonValue(reader);
            Assertions.assertInstanceOf(DsonObject.class, dsonValue);
        }
        Assertions.assertEquals(dsonValue, dsonValue2);
    }
}