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
using System.Text;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;

namespace Wjybxx.BigCatEditor.Generator.Excel
{
/// <summary>
/// 根据表格数据生成常量类
/// </summary>
public class ConstantGenerator
{
    private readonly string outDir;
    private readonly ClassName className;
    private readonly List<ConstValue> enumValues;

    /// <summary>
    ///
    /// </summary>
    /// <param name="outDir">输出目录</param>
    /// <param name="className">常量名</param>
    /// <param name="enumValues">常量值</param>
    public ConstantGenerator(string outDir,
                             ClassName className, List<ConstValue> enumValues) {
        this.outDir = outDir;
        this.className = className;
        this.enumValues = enumValues;
    }

    public void Execute() {
        TypeSpec.Builder typeBuilder = TypeSpec.NewEnumBuilder(className.simpleName)
            .AddModifiers(Modifiers.Public);

        List<int> intValues = new List<int>(enumValues.Count);
        foreach (ConstValue enumValue in enumValues) {
            TypeName typeName = enumValue.kind switch
            {
                ConstKind.Int32 => TypeName.INT,
                ConstKind.Int64 => TypeName.LONG,
                ConstKind.Float => TypeName.FLOAT,
                ConstKind.Double => TypeName.DOUBLE,
                ConstKind.Bool => TypeName.BOOL,
                ConstKind.String => TypeName.STRING,
                _ => throw new InvalidOperationException(enumValue.ToString())
            };
            var fieldBuilder = FieldSpec.NewBuilder(typeName, enumValue.name)
                .AddModifiers(Modifiers.Public | Modifiers.Const);
            if (!string.IsNullOrWhiteSpace(enumValue.comment)) {
                fieldBuilder.AddDocument(enumValue.comment);
            }
            if (enumValue.kind == ConstKind.String) {
                // 需要双引号
                fieldBuilder.Initializer("$S", enumValue.strValue);
            } else if (enumValue.kind == ConstKind.Bool) {
                // 避免冗余装箱
                fieldBuilder.Initializer(enumValue.BoolVal ? "true" : "false");
            } else if (enumValue.kind == ConstKind.Int32 || enumValue.kind == ConstKind.Int64) {
                // 避免出现小数点
                fieldBuilder.Initializer(enumValue.LongVal.ToString());
            } else {
                fieldBuilder.Initializer("$L", enumValue.numValue);
            }
            typeBuilder.AddField(fieldBuilder.Build());

            if (enumValue.kind == ConstKind.Int32) {
                intValues.Add(enumValue.IntVal);
            }
        }
        // 添加int值信息
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("new int[] {");
            for (int index = 0; index < intValues.Count; index++) {
                if (index > 0) sb.Append(',');
                if (index > 0 && (index % 5) == 0) { // 每5个值换一次行
                    sb.Append('\n');
                }
                sb.Append(intValues[index]);
            }
            sb.Append("}.ToImmutableList2()");

            typeBuilder.AddField(FieldSpec.NewBuilder(typeof(ImmutableList<int>), "INT_VALUES")
                .AddModifiers(Modifiers.Public | Modifiers.Static | Modifiers.ReadOnly)
                .AddDocument("Generated")
                .Initializer(sb.ToString())
                .Build());

            TypeName comparerTypeName = TypeName.Get(typeof(Comparer<int>));
            typeBuilder.AddField(FieldSpec.NewBuilder(typeof(ImmutableList<int>), "SORTED_VALUES")
                .AddModifiers(Modifiers.Public | Modifiers.Static | Modifiers.ReadOnly)
                .AddDocument("Generated")
                .Initializer("INT_VALUES.ToImmutableList2($T.Default)", comparerTypeName)
                .Build());
        }
        // 生成文件
        GeneratorUtil.WriteToFile(outDir, className, typeBuilder.Build());
    }
}
}