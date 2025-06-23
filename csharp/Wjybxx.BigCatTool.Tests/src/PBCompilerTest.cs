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

using System.IO;
using NUnit.Framework;
using Wjybxx.BigCatTool.Generator.Protobuf;
using Wjybxx.BigCatTool.Protobuf;
using Wjybxx.EditorTest;

namespace Wjybxx.BigCatTool.Tests;

/// <summary>
/// 
/// </summary>
public class PBCompilerTest
{
    [Test]
    public void Test() {
        string tempDir = TestUtil.GetTempDirectory() + "/pb";
        if (!Directory.Exists(tempDir)) {
            Directory.CreateDirectory(tempDir);
        }

        PBRepository repository = new PBRepository();
        // 解析所有pb文件
        string resDir = TestUtil.GetResDirectory();
        foreach (FileInfo fileInfo in new DirectoryInfo(resDir).EnumerateFiles("*.proto")) {
            PBFile pbFile = PBFileParser.Parse(fileInfo);
            repository.AddFile(pbFile);
        }
        // 解决依赖关系
        repository.Build();

        // 生成临时文件
        new PBFileGenerator(repository, resDir, tempDir).Execute();
        // 编译protobuf
        string generatedDir = TestUtil.GetGeneratedDirectory();
        new PBFileCompiler(null, tempDir, generatedDir).Execute();
        // 生成rpc代码
        new ServiceGenerator(repository, generatedDir, null).Execute();
    }
}