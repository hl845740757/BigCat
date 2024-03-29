/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.bigcat.excel;

import cn.wjybxx.bigcat.excel.gen.ConstantGenerator;
import cn.wjybxx.bigcat.excel.gen.EnumGenerator;
import cn.wjybxx.bigcat.excel.gen.SheetEnumValue;
import cn.wjybxx.bigcat.tools.TestUtil;
import org.junit.jupiter.api.Test;

import java.io.IOException;
import java.util.List;

/**
 * @author wjybxx
 * date - 2023/10/15
 */
public class ConstGeneratorTest {

    @Test
    void test() throws IOException {
        String testOutModulePath = TestUtil.bigcatPath + "/framework/testout";
        String javaOut = testOutModulePath + "/src/main/java";
        String javaPackage = "cn.wjybxx.bigcat.temp.excel";
        {
            List<SheetEnumValue> enumValueList = List.of(new SheetEnumValue("ONE", "1", null),
                    new SheetEnumValue("TWO", "2", null),
                    new SheetEnumValue("THREE", "3", null));
            var generator = new ConstantGenerator(javaOut, javaPackage, "String2StringConst",
                    enumValueList, true, false);
            generator.build();
        }
        {
            List<SheetEnumValue> enumValueList = List.of(new SheetEnumValue("ONE", 1, null),
                    new SheetEnumValue("TWO", 2, null),
                    new SheetEnumValue("THREE", 3, null));
            var generator = new ConstantGenerator(javaOut, javaPackage, "String2IntConst",
                    enumValueList, false, false);
            generator.build();
        }
        {
            List<SheetEnumValue> enumValueList = List.of(new SheetEnumValue("ONE", 1, null),
                    new SheetEnumValue("TWO", 2, null),
                    new SheetEnumValue("THREE", 3, null));
            var generator = new EnumGenerator(javaOut, javaPackage, "String2IntEnum",
                    enumValueList);
            generator.build();
        }
    }
}