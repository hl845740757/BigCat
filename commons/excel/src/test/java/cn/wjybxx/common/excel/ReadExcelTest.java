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

package cn.wjybxx.common.excel;

import cn.wjybxx.common.config.Sheet;
import cn.wjybxx.common.config.SheetCodec;
import cn.wjybxx.common.dson.DocClassId;
import cn.wjybxx.common.dson.TypeArgInfo;
import cn.wjybxx.common.dson.codec.ConvertOptions;
import cn.wjybxx.common.dson.document.DefaultDocumentConverter;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.io.File;
import java.util.List;
import java.util.Map;
import java.util.Set;

/**
 * @author wjybxx
 * date - 2023/4/17
 */
public class ReadExcelTest {

    @Test
    void name() {
        Map<String, Sheet> sheetMap = ExcelUtils.readExcel(new File("D:\\github-mine\\BigCat\\doc\\test.xlsx"),
                ExcelReaderOptions.newBuilder().build());

        Sheet skillSheet = sheetMap.get("Skill");

        DefaultDocumentConverter converter = DefaultDocumentConverter.newInstance(Set.of(),
                List.of(new SheetCodec()),
                Map.of(Sheet.class, new DocClassId("Sheet")),
                ConvertOptions.DEFAULT);

        Sheet clonedObject = converter.cloneObject(skillSheet, TypeArgInfo.of(Sheet.class));
        Assertions.assertEquals(skillSheet, clonedObject);

//        System.out.println(sheetMap);
    }
}