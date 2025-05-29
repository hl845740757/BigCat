#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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
using Wjybxx.Commons.Poet;

namespace Wjybxx.BigCatEditor.Generator.Excel
{
/// <summary>
/// 根据表格数据生成枚举类
///
/// 注意：C#不支持字符串值，因此传入的枚举值必须合法。
/// </summary>
public class EnumGenerator
{
    private static readonly AttributeSpec processorInfo = GeneratorUtil.NewProcessorInfoAnnotation(typeof(EnumGenerator));

    private readonly string outDir;
    private readonly ClassName className;
    private readonly List<ConstValue> enumValues;
    private readonly bool isFlags;
    private readonly bool allowAlias;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="outDir"></param>
    /// <param name="className"></param>
    /// <param name="enumValues"></param>
    /// <param name="isFlags">是否是Flags</param>
    /// <param name="allowAlias">是否允许枚举数重复</param>
    public EnumGenerator(string outDir,
                         ClassName className, List<ConstValue> enumValues,
                         bool isFlags, bool allowAlias = false) {
        this.outDir = outDir;
        this.className = className;
        this.enumValues = enumValues;
        this.isFlags = isFlags;
        this.allowAlias = allowAlias;
    }

    public void Execute() {
        if (enumValues.Any(e => e.kind != ConstKind.Int32)) {
            throw new InvalidOperationException("the kind of enumValue must be int32");
        }
        if (!allowAlias) {
            CheckAlias();
        }
        TypeSpec.Builder typeBuilder = TypeSpec.NewEnumBuilder(className.simpleName)
            .AddModifiers(Modifiers.Public)
            .AddAttribute(processorInfo);
        if (isFlags) {
            typeBuilder.AddAttribute(AttributeSpec.NewBuilder(GeneratorUtil.clsName_Flags).Build());
        }
        foreach (ConstValue enumValue in enumValues) {
            EnumValueSpec.Builder valueBuilder = EnumValueSpec.NewBuilder(enumValue.name, enumValue.IntVal);
            if (!string.IsNullOrWhiteSpace(enumValue.comment)) {
                valueBuilder.AddDocument(enumValue.comment);
            }
            typeBuilder.AddEnumValue(valueBuilder.Build());
        }
        // 增加最大和最小值
        int min = -1;
        int max = -1;
        if (enumValues.Count > 0) {
            min = enumValues.Min().IntVal;
            max = enumValues.Max().IntVal;
        }
        typeBuilder.AddEnumValue(EnumValueSpec.NewBuilder("MIN_VALUE", min)
            .AddDocument("Generated").Build());
        typeBuilder.AddEnumValue(EnumValueSpec.NewBuilder("MAX_VALUE", max)
            .AddDocument("Generated").Build());

        // 生成文件
        GeneratorUtil.WriteToFile(outDir, className, typeBuilder.Build());
    }

    private void CheckAlias() {
        HashSet<int> valueSet = new HashSet<int>(enumValues.Count);
        foreach (var enumValue in enumValues) {
            if (!valueSet.Add(enumValue.IntVal)) {
                throw new InvalidOperationException($"enumValue is duplicate, name: {enumValue.name}, number: {enumValue.IntVal}");
            }
        }
    }
}
}