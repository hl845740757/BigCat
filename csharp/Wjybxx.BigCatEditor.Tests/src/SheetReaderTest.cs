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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Wjybxx.BigCat.Fx;
using Wjybxx.BigCatEditor.DataScript;
using Wjybxx.BigCatEditor.Excel;
using Wjybxx.BigCatEditor.Generator.Excel;
using Wjybxx.EditorTest;

namespace Wjybxx.BigCat.EditorTest
{
public class SheetReaderTest
{
    [Test]
    public void Test() {
        // 更改为生成单文件
        string outDir = TestUtil.GetTempDirectory() + "/table";
        if (!Directory.Exists(outDir)) {
            Directory.CreateDirectory(outDir);
        }

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

        SheetRepository repository = new SheetRepository();
        foreach (Sheet sheet in ExcelUtil.Read(fileInfo, readerOptions)) {
            repository.AddSheet(sheet);
        }
        // 字符串转换在生成代码和导出文本之前
        new SstGenerator(repository, outDir).Execute();

        // 生成枚举ds文件
        string? enumDsFilePath = outDir + "/tableEnums.ds";
        {
            FileInfo enumTemplateFile = new FileInfo(resDir + "/SheetEnum.tt");
            List<ConstCfg> enumCfgs = new List<ConstCfg>()
            {
                new ConstCfg("ItemEnum", "Item", "enumName", "itemId", "desc")
            };
            new EnumGenerator(repository, enumTemplateFile, enumCfgs, enumDsFilePath).Execute();
        }

        // 生成ds文件
        string? tableDsFilePath = outDir + "/tables.ds";
        {
            FileInfo templateFile = new FileInfo(resDir + "/SheetCfg.tt");
            new DataScriptGenerator(repository, templateFile, tableDsFilePath, RequireMode.All).Execute();
        }

        // 构建ds仓库
        DSRepository dsRepository = new DSRepository();
        {
            dsRepository.AddFile(DSFileParser.Parse(new FileInfo(enumDsFilePath)));
            dsRepository.AddFile(DSFileParser.Parse(new FileInfo(tableDsFilePath)));
        }
        dsRepository.Build();
        // 导出Dson
        new DsonGenerator(repository, dsRepository, RequireMode.All, outDir, true).Execute();

        // 生成常量类
        {
            List<ConstCfg> constCfgs = new List<ConstCfg>()
            {
                new ConstCfg("ItemConst", "Item", "enumName", "itemId", "desc"),
                new ConstCfg("SkillConst", "SkillParam")
            };
            new ConstantGenerator(repository, "Wjybxx.BigCat.Demo", constCfgs, outDir).Execute();
        }

        // 测试SSTMgr
        List<string> fileList = new List<string>(11);
        fileList.Add(outDir + "/" + SstGenerator.FILE_LSST_INDEX);
        fileList.AddRange(Directory.GetFiles(outDir, SstGenerator.FILE_SST_DB + ".*"));
        SstMgr.Init(fileList);

        Console.WriteLine(SstMgr.GetString(1)); // 预加载的字符串
        Console.WriteLine(SstMgr.GetString(40)); // 延迟加载的字符串
    }

    /// <summary>
    /// 表单名正则表达式
    /// </summary>
    private static readonly Regex sheetNameRegex = new Regex("^[a-zA-Z][a-zA-Z0-9_\\.]*$", RegexOptions.Compiled);
    /// <summary>
    /// 字段名正则表达式
    /// </summary>
    private static readonly Regex fieldNameRegex = new("^[a-zA-Z_][a-zA-Z0-9_]*(?:[#@][KV]?\\d+)?$", RegexOptions.Compiled);

    [Test]
    public void SheetNameTest() {
        Assert.That(sheetNameRegex.IsMatch("Item.Equip"));
        Assert.That(sheetNameRegex.IsMatch("Item_Equip"));
    }

    /// <summary>
    /// 测试字段名检测的正确性
    /// </summary>
    [Test]
    public void FieldNameTest() {
        Assert.That(fieldNameRegex.IsMatch("itemArray#1"));
        Assert.That(fieldNameRegex.IsMatch("itemArray@1"));

        Assert.That(fieldNameRegex.IsMatch("itemDic#1")); // KV在同一个单元格
        Assert.That(fieldNameRegex.IsMatch("itemDic#K1")); // Key在单独单元格
        Assert.That(fieldNameRegex.IsMatch("itemDic#V1")); // Value在单独单元格

        Assert.That(!fieldNameRegex.IsMatch("itemDic#KK1"));
        Assert.That(!fieldNameRegex.IsMatch("itemDic#KV1")); // KV配置非法
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