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

package cn.wjybxx.bigcattools.excel;

import cn.wjybxx.base.TypeInfo;
import cn.wjybxx.bigcattools.common.TestUtil;
import cn.wjybxx.bigcattools.config.Sheet;
import cn.wjybxx.bigcattools.config.SheetCodec;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dsoncodec.ConverterOptions;
import cn.wjybxx.dsoncodec.DsonConverter;
import cn.wjybxx.dsoncodec.DsonConverterBuilder;
import cn.wjybxx.dsoncodec.TypeMeta;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.io.File;
import java.util.Map;

/**
 * @author wjybxx
 * date - 2023/4/17
 */
public class ReadExcelTest {

    @Test
    void test() {
        Map<String, Sheet> sheetMap = ExcelUtils.readExcel(new File(TestUtil.testResPath + "/test.xlsx"));
        Sheet skillSheet = sheetMap.get("Skill");

        ConverterOptions options = ConverterOptions.newBuilder().build();
        DsonConverter converter = new DsonConverterBuilder()
                .addTypeMeta(TypeMeta.of(Sheet.class, ObjectStyle.INDENT, "Sheet"))
                .addCodec(new SheetCodec())
                .setOptions(options)
                .build();

        String dson = converter.writeAsDson(skillSheet, TypeInfo.OBJECT);
        System.out.println(dson);

        TypeInfo sheetTypeInfo = TypeInfo.of(Sheet.class);
        Assertions.assertEquals(skillSheet, converter.readFromDson(dson, sheetTypeInfo));

        Sheet clonedObject = converter.cloneObject(skillSheet, sheetTypeInfo, sheetTypeInfo);
        Assertions.assertEquals(skillSheet, clonedObject);
    }
}