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
import cn.wjybxx.common.dson.text.DsonScanner;
import cn.wjybxx.common.dson.text.DsonStringBuffer;
import cn.wjybxx.common.dson.text.DsonTextReader;
import org.junit.jupiter.api.Test;

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
            -- }
            --
            -- {@MyStruct\s
            -- \tname : wjybxx,
            -- \tintro: "hello world",
            -- \tref1 : {@ref localId: 10001, guid: 16148b3b4e7b8923d398},
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
    }
}