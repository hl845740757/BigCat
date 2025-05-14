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
using System.IO;
using NUnit.Framework;
using Wjybxx.BigCatEditor.DataScript;
using Wjybxx.BigCatEditor.Generator.Protobuf;
using Wjybxx.BigCatEditor.Generator.Rpc;
using Wjybxx.BigCatEditor.Protobuf;
using Wjybxx.Dson;
using Wjybxx.EditorTest;

namespace Wjybxx.BigCat.EditorTest;

/// <summary>
/// 
/// </summary>
public class DSCompilerTest
{
    [Test]
    public void Test() {
        DSRepository repository = new DSRepository();
        // 解析所有pb文件
        string resDir = TestUtil.GetResDirectory();
        foreach (FileInfo fileInfo in new DirectoryInfo(resDir).EnumerateFiles("*.ds")) {
            DSFile file = DSFileParser.Parse(fileInfo);
            repository.AddFile(file);
        }
        // 解决依赖关系
        repository.Build();

        DSNamedTypeElement builtinType = repository.GetBuiltinType(DSKeywords.TYPE_INT32);
        repository.MakeNullableType(builtinType);

        DSInst inst = repository.FindInst("vector3_array");
        Assert.NotNull(inst);
        Console.WriteLine(inst.DsonValue.ToDson());
    }
}