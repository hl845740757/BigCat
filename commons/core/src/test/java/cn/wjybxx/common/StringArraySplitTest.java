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

package cn.wjybxx.common;

import cn.wjybxx.common.config.ArrayStringParseException;
import cn.wjybxx.common.config.DefaultValueParser;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.io.IOException;
import java.util.List;

/**
 * @author wjybxx
 * date 2023/4/15
 */
public class StringArraySplitTest {

    @Test
    void test1() throws IOException {
        String arrayString = "{  a, \"abc\" , , b  }";
        List<String> expected = List.of("a", "\"abc\"", "", "b");

        String[] stringsArray = DefaultValueParser.getInstance().readAsArray("string[]", arrayString, String[].class);
        Assertions.assertEquals(expected, List.of(stringsArray));
    }

    @Test
    void test2() {
        String arrayString = "{  a, \"{abc}\" , { , ?, }, b  }";
        List<String> expected = List.of("a", "\"{abc}\"", "{ , ?, }", "b");

        String[] stringsArray = DefaultValueParser.getInstance().readAsArray("string[]", arrayString, String[].class);
        Assertions.assertEquals(expected, List.of(stringsArray));
    }

    @Test
    void test3() {
        String arrayString = "{ a  b}";
        Assertions.assertThrows(ArrayStringParseException.class, () -> {
            DefaultValueParser.getInstance().readAsArray("string[]", arrayString, String[].class);
        });
    }
}