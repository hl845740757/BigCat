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

namespace Wjybxx.BigCat.Util
{
public static class FileUtil
{
    /// <summary>
    /// 路径是否已存在
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static bool PathExists(string path) {
        return File.Exists(path) || Directory.Exists(path);
    }

    /// <summary>
    /// 删除文件或文件夹
    /// </summary>
    /// <param name="path"></param>
    public static void DeleteFileOrDirectory(string path) {
        if (File.Exists(path)) {
            File.Delete(path);
        } else if (Directory.Exists(path)) {
            Directory.Delete(path, true);
        }
    }

    /// <summary>
    /// 拷贝文件或文件夹
    /// </summary>
    public static void CopyFileOrDirectory(string sourcePath, string destPath, bool overwrite = true) {
        if (File.Exists(sourcePath)) {
            CopyFile(sourcePath, destPath, overwrite);
        } else if (Directory.Exists(destPath)) {
            CopyDirectory(destPath, sourcePath, overwrite);
        } else {
            throw new FileNotFoundException(sourcePath);
        }
    }

    /// <summary>
    /// 拷贝文件
    /// </summary>
    public static void CopyFile(string sourcePath, string destPath, bool overwrite = true) {
        if (!File.Exists(sourcePath)) {
            throw new FileNotFoundException(sourcePath);
        }
        CreateFileDirectory(destPath);
        File.Copy(sourcePath, destPath, overwrite);
    }

    /// <summary>
    /// 拷贝文件夹
    /// </summary>
    /// <param name="sourceDir">原目录</param>
    /// <param name="destinationDir">目标目录</param>
    /// <param name="overwrite">是否覆盖已存在的文件</param>
    /// <param name="recursive">是否递归</param>
    /// <exception cref="DirectoryNotFoundException">如果原文件夹不存在</exception>
    public static void CopyDirectory(string sourceDir, string destinationDir, bool overwrite, bool recursive = true) {
        DirectoryInfo? srcDirInfo = new DirectoryInfo(sourceDir);
        if (!srcDirInfo.Exists) {
            throw new DirectoryNotFoundException($"Source directory not found: {srcDirInfo.FullName}");
        }
        DirectoryInfo destDirInfo = new DirectoryInfo(destinationDir);
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
                string destinationSubDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, destinationSubDir, overwrite);
            }
        }
    }

    /// <summary>
    /// 清理文件夹（保留空文件夹）
    /// </summary>
    /// <param name="dirName">要清理的文件夹</param>
    /// <param name="retainSubDir">是否保留子文件夹</param>
    public static void ClearDirectory(string dirName, bool retainSubDir = false) {
        DirectoryInfo directoryInfo = new DirectoryInfo(dirName);
        if (!directoryInfo.Exists) {
            return;
        }
        foreach (FileInfo file in directoryInfo.GetFiles()) {
            file.Delete();
        }
        foreach (DirectoryInfo subDir in directoryInfo.GetDirectories()) {
            if (retainSubDir) {
                ClearDirectory(subDir.FullName, true);
            } else {
                subDir.Delete(true);
            }
        }
    }

    /// <summary>
    /// 删除文件夹
    /// </summary>
    /// <param name="dirName"></param>
    public static void DelectDirectory(string dirName) {
        DirectoryInfo directoryInfo = new DirectoryInfo(dirName);
        if (directoryInfo.Exists) {
            directoryInfo.Delete(true);
        }
    }

    /// <summary>
    /// 创建文件夹
    /// </summary>
    /// <param name="dirName"></param>
    public static void CreateDirectory(string dirName) {
        if (!Directory.Exists(dirName)) {
            Directory.CreateDirectory(dirName);
        }
    }

    /// <summary>
    /// 创建文件所在的目录
    /// (mkdir -p)
    /// </summary>
    /// <param name="filePath">文件路径</param>
    public static void CreateFileDirectory(string filePath) {
        string destDirectory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(destDirectory)) {
            throw new ArgumentException("filePath: " + filePath);
        }
        CreateDirectory(destDirectory);
    }

    /// <summary>
    /// 移动文件
    /// </summary>
    public static void MoveFile(string filePath, string destPath, bool overwrite = true) {
        if (overwrite && File.Exists(filePath)) {
            File.Delete(filePath); // 旧API不支持传overwrite参数
        }
        FileInfo fileInfo = new FileInfo(filePath);
        fileInfo.MoveTo(destPath);
    }

    /// <summary>
    /// 移动文件夹
    /// </summary>
    public static void MoveDirectory(string filePath, string destPath, bool overwrite = false) {
        if (overwrite && Directory.Exists(destPath)) {
            Directory.Delete(destPath, true); // 新旧API都不支持传overwrite参数
        }
        DirectoryInfo directoryInfo = new DirectoryInfo(filePath);
        directoryInfo.MoveTo(destPath);
    }

    /// <summary>
    /// 移动文件或文件夹
    /// </summary>
    public static void MoveFileOrDirectory(string filePath, string destPath, bool overwrite = true) {
        if (File.Exists(filePath)) {
            MoveFile(filePath, destPath, overwrite);
        } else if (Directory.Exists(filePath)) {
            MoveDirectory(filePath, destPath, overwrite);
        } else {
            throw new FileNotFoundException(filePath);
        }
    }

    /// <summary>
    /// 获取文件大小
    /// </summary>
    public static long GetFileSize(string filePath) {
        FileInfo fileInfo = new FileInfo(filePath);
        return fileInfo.Exists ? fileInfo.Length : 0;
    }

    /// <summary>
    /// 获取文件夹内容大小
    /// </summary>
    public static long GetDirectorySize(string path) {
        long length = 0;
        foreach (string fileName in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)) {
            FileInfo fileInfo = new FileInfo(fileName);
            length += fileInfo.Length;
        }
        return length;
    }
}
}