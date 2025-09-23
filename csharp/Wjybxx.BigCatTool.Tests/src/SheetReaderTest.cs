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
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Wjybxx.BigCat.Fx;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.BigCatTool.Excel;
using Wjybxx.BigCatTool.Generator.Excel;
using Wjybxx.Dson;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCatTool.Tests
{
public class SheetReaderTest
{
    private static IDsonConverter converter;

    [OneTimeSetUp]
    public static void SetUp() {
        string interfaceName = typeof(IDsonCodec).FullName;
        List<Type> codecTypeList = typeof(ConstGeneratorCfg).Assembly.GetTypes()
            .Where(type => type.Name.EndsWith("Codec") && type.GetInterface(interfaceName!) != null)
            .ToList();
        // core包的codec
        codecTypeList.AddRange(typeof(CodeGeneratorCfg).Assembly.GetTypes()
            .Where(type => type.Name.EndsWith("Codec") && type.GetInterface(interfaceName!) != null));

        DsonConverterBuilder builder = new DsonConverterBuilder();
        foreach (Type codecType in codecTypeList) {
            Type encoderType = GetEncoderType(codecType);
            // 添加Codec
            if (codecType.IsGenericType) {
                builder.AddGenericCodec(encoderType, codecType);
                builder.AddTypeMeta(TypeMeta.Of(encoderType, ObjectStyle.Indent, encoderType.GetGenericTypeDefinition().Name));
            } else {
                builder.AddTypeMeta(TypeMeta.Of(encoderType, ObjectStyle.Indent, encoderType.Name));
                builder.AddCodec((IDsonCodec)Activator.CreateInstance(codecType)!);
            }
        }
        converter = builder.Build();
    }

    private static Type GetEncoderType(Type codecType) {
        Type type = codecType.GetInterface(typeof(IDsonCodec<>).Name);
        return type!.GetGenericArguments()[0];
    }

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
        string sstDir = outDir + "/sst";
        new SstGenerator(repository, sstDir).Execute();

        DsonObject<string> cfgObject = Dsons.FromDson(File.ReadAllText(resDir + "/SheetGeneratorCfg.dson")).AsObject();
        // 生成ds文件 -- 先由Dson初始化，再覆盖部分数据
        DataScriptGeneratorCfg dsGeneratorCfg = converter.ReadFromDsonValue<DataScriptGeneratorCfg>(cfgObject["dsGenerator"]);
        dsGeneratorCfg.outPath = outDir + "/tables.ds";
        dsGeneratorCfg.templateFile = resDir + "/SheetCfg.tt";
        new DataScriptGenerator(repository, dsGeneratorCfg, RequireMode.All).Execute();

        // 构建ds仓库
        DSRepository dsRepository = new DSRepository();
        {
            dsRepository.AddFile(DSFileParser.Parse(new FileInfo(dsGeneratorCfg.outPath)));
        }
        dsRepository.Build();
        // 导出Dson
        DsonGeneratorCfg dsonGeneratorCfg = converter.ReadFromDsonValue<DsonGeneratorCfg>(cfgObject["dsonGenerator"]);
        dsonGeneratorCfg.outPath = outDir;
        new DsonGenerator(repository, dsRepository, dsonGeneratorCfg, RequireMode.All).Execute();

        // 生成常量类 -- 先由Dson初始化，再覆盖部分数据
        ConstGeneratorCfg constGeneratorCfg = converter.ReadFromDsonValue<ConstGeneratorCfg>(cfgObject["constGenerator"]);
        constGeneratorCfg.outPath = outDir;
        new ConstGenerator(repository, constGeneratorCfg).Execute();

        // 生成Class -- 先由Dson初始化，再覆盖部分数据
        CodeGeneratorCfg classGeneratorCfg = converter.ReadFromDsonValue<CodeGeneratorCfg>(cfgObject["classGenerator"]);
        classGeneratorCfg.outPath = outDir;
        new ClassGenerator(dsRepository, classGeneratorCfg, new List<string>() { "tables.ds" }).Execute();

        // 测试SSTMgr
        if (!Directory.Exists(sstDir)) {
            Directory.CreateDirectory(sstDir);
        }
        string indexFile = sstDir + "/" + SstGenerator.FILE_LSST_INDEX;
        SstMgr.Init(Directory.GetFiles(sstDir, SstGenerator.FILE_SST_DB + ".*"), indexFile);

        string str1 = SstMgr.GetString(21);
        string str2 = SstMgr.GetString(22);
        Console.WriteLine("str1: " + str1);
        Console.WriteLine("str2: " + str2);
        Console.WriteLine("ReferenceEquals:" + ReferenceEquals(str1, str2));

        // Console.WriteLine(SstMgr.GetString(11)); // 预加载的字符串
        // Console.WriteLine(SstMgr.GetString(201)); // 延迟加载的字符串
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