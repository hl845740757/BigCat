#region LICENSE
// Copyright 2023 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Wjybxx.BigCatEditor.Excel;
using Wjybxx.BigCatEditor.Generator.Excel;
using Wjybxx.EditorTest;

namespace Wjybxx.BigCat.EditorTest
{
public class SheetReaderTest
{
    [Test]
    public void Test() {
        string resDir = TestUtil.GetResDirectory();
        string filePath = resDir + "/test.xlsx";

        FileInfo fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists) {
            return;
        }
        ExcelReaderOptions readerOptions = new ExcelReaderOptions.Builder()
        {
            SkipRows = 10
        }.Build();

        List<Sheet> sheets = ExcelUtil.Read(fileInfo, readerOptions);
        foreach (Sheet sheet in sheets) {
            
        }
    }
    
    private void test() {
        // Map<String, Sheet> sheetMap = ExcelUtils.readExcel(new File(TestUtil.testResPath + "/test.xlsx"));
        // Sheet skillSheet = sheetMap.get("Skill");
        //
        // ConverterOptions options = ConverterOptions.newBuilder().build();
        // DsonConverter converter = new DsonConverterBuilder()
        //     .addTypeMeta(TypeMeta.of(Sheet.class, ObjectStyle.INDENT, "Sheet"))
        //     .addCodec(new SheetCodec())
        //     .setOptions(options)
        //     .build();
        //
        // String dson = converter.writeAsDson(skillSheet, TypeInfo.OBJECT);
        // System.out.println(dson);
        //
        // TypeInfo sheetTypeInfo = TypeInfo.of(Sheet.class);
        // Assertions.assertEquals(skillSheet, converter.readFromDson(dson, sheetTypeInfo));
        //
        // Sheet clonedObject = converter.cloneObject(skillSheet, sheetTypeInfo, sheetTypeInfo);
        // Assertions.assertEquals(skillSheet, clonedObject);
    }
}
}