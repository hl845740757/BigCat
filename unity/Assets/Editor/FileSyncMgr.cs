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
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Editor
{
/// <summary>
/// 用户拷贝C#目录下的文件到本地
/// （暂时先不打程序集）
/// </summary>
public class FileSyncMgr
{
     /// <summary>
    /// 需要拷贝的普通程序集
    /// (直接local包)
    /// </summary>
    private static readonly List<string> _projectNames = new()
    {
    };

    /// <summary>
    /// 需要拷贝的文件
    /// (Generated目录统一在程序集的顶层目录)
    /// </summary>
    private static readonly List<string> _includes = new()
    {
        "src", // 源代码
        // "Generated/Wjybxx.Dson.Apt", // Apt生成的解码器
        // "Generated/Wjybxx.BigCat.Apt", // Apt生成的Rpc辅助类
    };

    /// <summary>
    /// 同步本地包
    /// </summary>
    // [MenuItem("Editor/SyncLocalPackages")]
    public static void CreateUpkg() {
        foreach (string projectName in _projectNames) {
            CopyProject(projectName);
        }
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 拷贝单个程序集
    /// </summary>
    /// <param name="projectName">要拷贝的程序集</param>
    private static void CopyProject(string projectName) {
        // dataPath为Assets目录
        DirectoryInfo rootDir = new DirectoryInfo(Application.dataPath)
            .Parent!.Parent!;
        string srcProjectDir = rootDir.FullName + "/csharp/" + projectName;
        string destProjectDir = Application.dataPath + "/ThirdParty/" + projectName;
        // File.Exists只针对文件，不针对文件夹...
        if (!Directory.Exists(destProjectDir)) {
            Directory.CreateDirectory(destProjectDir);
        }
        // 统计需要拷贝的文件的相对路径
        foreach (string srcDir in _includes) {
            DirectoryInfo srcSubDirInfo = new DirectoryInfo(srcProjectDir + "/" + srcDir);
            if (!srcSubDirInfo.Exists) continue;

            // 这里只统计不拷贝，系统的File.Copy可以更高效地批量拷贝
            HashSet<string> srcDirFiles = StatisticFiles(srcSubDirInfo);

            // src直接拷贝的Runtime目录 -- generated会创建对应子目录
            string destSubDir = srcDir == "src"
                ? destProjectDir + "/Runtime"
                : destProjectDir + "/Runtime/" + srcDir;
            CopyDirectory(srcSubDirInfo.FullName, destSubDir, overwrite: true);
            Thread.Sleep(10); // 延迟一下，确保文件刷新

            // 需要删除Generated下的文件
            DirectoryInfo destSubDirInfo = new DirectoryInfo(destSubDir);
            HashSet<string> destDirFiles = StatisticFiles(destSubDirInfo);
            if (srcDir == "src") {
                destDirFiles.RemoveWhere(e => e.StartsWith("Generated/")); // linux, mac ...
                destDirFiles.RemoveWhere(e => e.StartsWith("Generated\\")); // windows
            }

            // 删除多余的文件
            foreach (string destDirFile in destDirFiles) {
                // meta文件在删除源文件的时候一起删除，不单独测试
                if (destDirFile.EndsWith(".meta")) continue;
                // 程序集定义文件
                if (destDirFile.EndsWith(".asmdef")) continue;
                if (destDirFile.StartsWith("AssemblyInfo.")) continue;

                if (!srcDirFiles.Contains(destDirFile)) {
                    string fullName = destSubDirInfo.FullName + "/" + destDirFile;
                    Debug.Log($"delete file: {Path.GetRelativePath(destProjectDir, fullName)}");

                    File.Delete(fullName);
                    File.Delete(fullName + ".meta");
                }
            }
        }
    }

    /** 统计文件夹下的文件 -- 返回的是相对路径 */
    private static HashSet<string> StatisticFiles(DirectoryInfo directoryInfo) {
        HashSet<string> result = new HashSet<string>(100);
        foreach (FileInfo fileInfo in directoryInfo.GetFiles("*", SearchOption.AllDirectories)) {
            string relativePath = Path.GetRelativePath(directoryInfo.FullName, fileInfo.FullName);
            result.Add(relativePath);
        }
        return result;
    }

    private static void CopyDirectory(string sourceDir, string destinationDir, bool overwrite, bool recursive = true) {
        var srcDirInfo = new DirectoryInfo(sourceDir);
        if (!srcDirInfo.Exists) {
            throw new DirectoryNotFoundException($"Source directory not found: {srcDirInfo.FullName}");
        }
        var destDirInfo = new DirectoryInfo(destinationDir);
        if (!destDirInfo.Exists) {
            destDirInfo.Create();
        }
        // 先拷贝直接文件
        foreach (FileInfo file in srcDirInfo.GetFiles()) {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, overwrite);
        }
        // 如果递归，则拷贝子目录
        if (recursive) {
            foreach (DirectoryInfo subDir in srcDirInfo.GetDirectories()) {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, overwrite);
            }
        }
    }
}
}