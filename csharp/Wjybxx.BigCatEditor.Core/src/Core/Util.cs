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
using System.Runtime.CompilerServices;
using System.Text;
using Wjybxx.Commons;

namespace Wjybxx.BigCatEditor.Core
{
/// <summary>
/// 
/// </summary>
public static class Util
{
    public static readonly Encoding ENCODING_UTF8 = new UTF8Encoding(false);

    #region 空白字符

    /// <summary>
    /// 索引首个空白字符
    /// </summary>
    public static int IndexOfWhitespace(string cs, int startIndex = 0) {
        if (startIndex < 0) {
            throw new ArgumentException("startIndex " + startIndex);
        }
        int length = ObjectUtil.Length(cs);
        if (length == 0) {
            return -1;
        }
        for (int i = startIndex; i < length; i++) {
            if (char.IsWhiteSpace(cs[i])) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 反向索引首个空白字符
    /// </summary>
    public static int LastIndexOfWhitespace(string cs, int startIndex = -1) {
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
            if (char.IsWhiteSpace(cs[i])) {
                return i;
            }
        }
        return -1;
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

    #endregion

    /// <summary>
    /// 将字符串拆分为行
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static List<string> GetLines(string str) {
        List<string> stringList = new List<string>();
        using (StringReader stringReader = new StringReader(str)) {
            string str1;
            while ((str1 = stringReader.ReadLine()) != null)
                stringList.Add(str1);
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
    /// 删除字符串中的空白字符
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string DeleteWhitespace(string str) {
        if (IndexOfWhitespace(str) < 0) {
            return str;
        }
        int len = str.Length;
        StringBuilder sb = new StringBuilder(len);
        for (int idx = 0; idx < len; idx++) {
            char c = str[idx];
            if (char.IsWhiteSpace(c)) {
                continue;
            }
            sb.Append(c);
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

    /// <summary>
    /// 从工作目录向上查找指定目录
    /// </summary>
    /// <param name="dirName"></param>
    /// <returns></returns>
    public static string GetDirectory(string dirName) {
        DirectoryInfo directoryInfo = new DirectoryInfo(Environment.CurrentDirectory);
        while (true) {
            if (directoryInfo.Name == dirName) {
                return directoryInfo.FullName;
            }
            directoryInfo = directoryInfo.Parent;
            if (directoryInfo == null) {
                throw new IOException($"dic {dirName} not found");
            }
        }
    }
}
}