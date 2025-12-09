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
using System.Text;
using Wjybxx.Commons;

namespace Wjybxx.BigCatTool
{
/// <summary>
///
/// </summary>
public class ToolUtil
{
    #region 字符串

    public static string FirstCharToUpperCase(string str) {
        return ObjectUtil.FirstCharToUpperCase(str);
    }

    public static string FirstCharToLowerCase(string str) {
        return ObjectUtil.FirstCharToLowerCase(str);
    }

    /// <summary>
    /// 索引首个空白字符
    /// </summary>
    public static int IndexOfWhitespace(string cs, int startIndex = 0) {
        return ObjectUtil.IndexOfWhitespace(cs, startIndex);
    }

    /// <summary>
    /// 反向索引首个空白字符
    /// </summary>
    public static int LastIndexOfWhitespace(string cs, int startIndex = -1) {
        return ObjectUtil.LastIndexOfWhitespace(cs, startIndex);
    }

    /// <summary>
    /// 删除空白字符
    /// </summary>
    /// <param name="cs"></param>
    /// <returns></returns>
    public static string DeleteWhitespace(string cs) {
        return ObjectUtil.DeleteWhitespace(cs);
    }

    /// <summary>
    /// 索引首个非空白字符
    /// </summary>
    public static int IndexOfNonWhitespace(string cs, int startIndex = 0) {
        if (startIndex < 0) {
            throw new ArgumentException("startIndex " + startIndex);
        }
        int length = ObjectUtil.Length(cs);
        if (length == 0) {
            return -1;
        }
        for (int i = startIndex; i < length; i++) {
            if (!char.IsWhiteSpace(cs[i])) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 反向索引首个非空白字符
    /// </summary>
    public static int LastIndexOfNonWhitespace(string cs, int startIndex = -1) {
        if (startIndex < -1) {
            throw new ArgumentException("startIndex " + startIndex);
        }
        int length = ObjectUtil.Length(cs);
        if (length == 0) {
            return -1;
        }
        if (startIndex == -1 || startIndex >= length) {
            startIndex = length - 1;
        }
        for (int i = startIndex; i >= 0; i--) {
            if (!char.IsWhiteSpace(cs[i])) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 将字符串拆分为行
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static List<string> GetLines(string str) {
        List<string> stringList = new List<string>();
        using (StringReader stringReader = new StringReader(str)) {
            string line;
            while ((line = stringReader.ReadLine()) != null)
                stringList.Add(line);
        }
        return stringList;
    }

    /// <summary>
    /// 去除字符串的双引号
    /// </summary>
    /// <param name="str">要处理的字符串</param>
    /// <param name="trim">是否去掉两端空白</param>
    /// <returns></returns>
    public static string Unquote(string str, bool trim = false) {
        int length = ObjectUtil.Length(str);
        if (length < 2) {
            return str;
        }
        char firstChar = str[0];
        char lastChar = str[str.Length - 1];
        if (firstChar == '"' && lastChar == '"') {
            if (trim) {
                int start = IndexOfNonWhitespace(str, 0);
                int end = LastIndexOfNonWhitespace(str);
                if (start < 0) {
                    return "";
                }
                return str.Substring2(start, end);
            }
            return str.Substring2(1, str.Length - 1);
        }
        return str;
    }

    /// <summary>
    /// 蛇形字符串转大驼峰
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ToUpperCamel(string str) {
        if (str.IndexOf('_') < 0) {
            return ObjectUtil.FirstCharToUpperCase(str);
        }
        StringBuilder sb = new StringBuilder(str.Length);
        bool nextUpperCase = true;
        foreach (char c in str) {
            if (c == '_' || c == ' ') {
                nextUpperCase = true;
                continue;
            }
            sb.Append(nextUpperCase ? char.ToUpper(c) : c);
            nextUpperCase = false;
        }
        return sb.ToString();
    }

    /// <summary>
    /// 删除特定字符
    /// </summary>
    /// <param name="str"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    public static string DeleteChar(string str, char c) {
        if (str.IndexOf(c) < 0) {
            return str;
        }
        int len = str.Length;
        StringBuilder sb = new StringBuilder(len);
        for (int idx = 0; idx < len; idx++) {
            char c2 = str[idx];
            if (c2 == c) {
                continue;
            }
            sb.Append(c2);
        }
        return sb.ToString();
    }

    #endregion

    #region file

    public static readonly Encoding ENCODING_UTF8 = new UTF8Encoding(false);

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

    /// <summary>
    /// 规格化资产路径(并非适用于任意场景)
    ///
    /// 1.文件扩展名之前的部分转小写，扩展名不转小写。
    /// 2.编辑器数据不应执行规格化，编辑器下可能需要打开目录，规格化以后不可逆。
    /// </summary>
    public static string NormalizeAssetPath(string assetPath) {
        if (string.IsNullOrEmpty(assetPath)) {
            return assetPath;
        }
        assetPath = assetPath.Replace('\\', '/');
        int spIndex = assetPath.LastIndexOf('/');
        if (spIndex >= 0) {
            spIndex = assetPath.IndexOf('.', spIndex);
        } else {
            spIndex = assetPath.IndexOf('.'); // 注意：不可改用LastIndexOf，以支持多级文件扩展名
        }
        if (spIndex < 0) {
            assetPath = assetPath.ToLower();
        } else {
            assetPath = assetPath.Substring(0, spIndex).ToLower() + assetPath.Substring(spIndex);
        }
        return assetPath;
    }

    #endregion
}
}