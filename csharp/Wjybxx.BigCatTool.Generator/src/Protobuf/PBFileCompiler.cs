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
using System.Diagnostics;
using System.IO;

namespace Wjybxx.BigCatTool.Generator.Protobuf
{
/// <summary>
/// 编译protobuf文件生成c#代码
/// </summary>
public class PBFileCompiler
{
    /// <summary>
    /// protoc文件路径
    /// 
    /// 如果不指定，则直接使用protoc命令；如果指定路径，则使用给定路径的protoc编译。
    /// 如果指定路径，以Windows为例，需要指定到'protoc.exe'。
    /// </summary>
    private readonly string? protocPath;
    private readonly string protoDir;
    private readonly string csharpOutDir;

    /// <summary>
    ///
    /// </summary>
    /// <param name="protocPath">protoc文件路径</param>
    /// <param name="protoDir">协议文件目录</param>
    /// <param name="csharpOutDir">csharp文件输出目录</param>
    public PBFileCompiler(string? protocPath, string protoDir, string csharpOutDir) {
        this.protocPath = protocPath;
        this.protoDir = protoDir;
        this.csharpOutDir = csharpOutDir;
    }

    public void Execute() {
        // 尾参数为要编译的proto文件，可通过*匹配所有proto文件
        string normalizedProtoDir = Path.GetFullPath(protoDir);
        string normalizedOutDir = Path.GetFullPath(csharpOutDir);
        string normalizedSearchPattern = Path.GetFullPath(protoDir + "/*.proto");

        Process process = new Process();
        process.StartInfo.FileName = protocPath ?? "protoc"; // 在环境变量查找命令的时候，不需要指定exe
        process.StartInfo.Arguments = $"--proto_path={normalizedProtoDir} --csharp_out={normalizedOutDir} {normalizedSearchPattern}";
        process.StartInfo.UseShellExecute = false; // 必须设为false才能重定向输出
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        process.Start();
        process.WaitForExit();
        if (process.ExitCode == 0) {
            string output = process.StandardOutput.ReadToEnd();
            Console.WriteLine(output);
        } else {
            string error = process.StandardError.ReadToEnd();
            throw new IOException(error, process.ExitCode);
        }
    }
}
}