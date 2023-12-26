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

package cn.wjybxx.bigcat.excel;

import cn.wjybxx.bigcat.config.Sheet;
import cn.wjybxx.bigcat.config.SheetCodec;
import cn.wjybxx.bigcat.tools.TestUtil;
import cn.wjybxx.dson.codec.ConvertOptions;
import cn.wjybxx.dson.codec.TypeArgInfo;
import cn.wjybxx.dson.codec.TypeMeta;
import cn.wjybxx.dson.codec.TypeMetaRegistries;
import cn.wjybxx.dson.codec.document.DefaultDocumentConverter;
import cn.wjybxx.dson.codec.document.DocumentConverter;
import cn.wjybxx.dson.text.DsonMode;
import cn.wjybxx.dson.text.ObjectStyle;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.io.File;
import java.util.List;
import java.util.Map;

/**
 * @author wjybxx
 * date - 2023/4/17
 */
public class ReadExcelTest {

    @Test
    void test() {
        Map<String, Sheet> sheetMap = ExcelUtils.readExcel(new File(TestUtil.docPath + "/test.xlsx"));
        Sheet skillSheet = sheetMap.get("Skill");

        ConvertOptions options = ConvertOptions.newBuilder().build();
        DocumentConverter converter = DefaultDocumentConverter.newInstance(
                List.of(new SheetCodec()),
                TypeMetaRegistries.fromMetas(TypeMeta.of(Sheet.class, ObjectStyle.INDENT, "Sheet")),
                options);

        String dson = converter.writeAsDson(skillSheet, DsonMode.RELAXED, TypeArgInfo.OBJECT);
//        System.out.println(dson);
        Assertions.assertEquals(skillSheet, converter.readFromDson(dson, DsonMode.RELAXED, TypeArgInfo.of(Sheet.class)));

        Sheet clonedObject = converter.cloneObject(skillSheet, TypeArgInfo.of(Sheet.class));
        Assertions.assertEquals(skillSheet, clonedObject);
    }
}