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

using System.Collections.Generic;
using System.IO;
using System.Text;
using Wjybxx.BigCatEditor.Core;
using Wjybxx.BigCatEditor.Protobuf;
using Range = Wjybxx.BigCatEditor.Protobuf.Range;

namespace Wjybxx.BigCatEditor.Generator.Protobuf
{
/// <summary>
/// 生成注释掉Service的PB文件
///
/// Q：为什么不再追加额外的数据和空白行填充了？
/// A：import和options这些数据都是极少的，用户每个文件配置一下并不费劲；
/// 此外，生成的临时文件和真实文件除了Service被注释，其它一模一样的话，体验会更好；
/// </summary>
public class PBFileGenerator
{
    /// <summary>
    /// 解析后的文件信息
    /// -- 用于判断那些行需要被注释
    /// </summary>
    private readonly PBRepository repository;
    /// <summary>
    /// 原始proto文件夹
    /// -- 不支持子目录，通常也没有必要
    /// </summary>
    private readonly string srcDir;
    /// <summary>
    /// 临时proto文件的文件夹
    /// </summary>
    private readonly string destDir;

    public PBFileGenerator(PBRepository repository, string srcDir, string destDir) {
        this.srcDir = srcDir;
        this.destDir = destDir;
        this.repository = repository;
    }

    public void Execute() {
        DirectoryInfo srcDirInfo = new DirectoryInfo(srcDir);
        DirectoryInfo destDirInfo = new DirectoryInfo(destDir);
        if (!destDirInfo.Exists) {
            destDirInfo.Create();
        } else {
            foreach (FileInfo fileInfo in destDirInfo.EnumerateFiles("*.proto")) {
                fileInfo.Delete();
            }
        }
        // 未被解析的proto文件将被跳过
        foreach (FileInfo fileInfo in srcDirInfo.GetFiles("*.proto", SearchOption.TopDirectoryOnly)) {
            string fileSimpleName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            string destFilePath = $"{destDirInfo.FullName}/{fileInfo.Name}";

            PBFile pbFile = repository.GetFile(fileSimpleName);
            if (pbFile == null) {
                continue;
            }
            List<PBService> services = pbFile.GetServices();
            if (services.Count == 0) {
                File.Copy(fileInfo.FullName, destFilePath);
                continue;
            }

            string[] allLines = File.ReadAllLines(fileInfo.FullName, Encoding.UTF8);
            string[] outLines = new string[allLines.Length];
            Range nextRange = FindNextRange(services, 0);
            for (int idx = 0; idx < allLines.Length; idx++) {
                int ln = idx + 1;
                if (ln > nextRange.end) {
                    nextRange = FindNextRange(services, ln);
                }
                string line = allLines[idx];
                if (ln >= nextRange.start && ln <= nextRange.end && HasContent(line)) {
                    outLines[idx] = "//" + line;
                } else {
                    outLines[idx] = line;
                }
            }
            File.AppendAllLines(destFilePath, outLines);
        }
    }

    private static bool HasContent(string line) {
        int idx = Util.IndexOfNonWhitespace(line);
        return idx >= 0 && line[idx] != '/';
    }

    private static Range FindNextRange(List<PBService> services, int ln) {
        foreach (PBService service in services) {
            if (ln < service.StartLine) {
                return new Range(service.StartLine, service.EndLine);
            }
        }
        return new Range(int.MaxValue, int.MaxValue);
    }
}
}