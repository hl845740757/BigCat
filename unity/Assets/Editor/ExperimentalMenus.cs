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
using UnityEditor;
using UnityEngine;

namespace Editor
{
/// <summary>
/// 验证想法的菜单项
/// </summary>
public static class ExperimentalMenus
{
    /// <summary>
    /// 理论上越早收录进Unicode的字符，越是常用字符，而其码点通常也越小？？？
    /// 实测发现好像不是...更像是按照字典中的偏旁部首序录入到Unicode字符集的。
    /// </summary>
    // [MenuItem("Editor/SortHan7000")]
    public static void SortHan7000() {
        string text = File.ReadAllText("Assets/Editor/Resources/7000+symbols.txt");
        string sortedText = SortByCodePoint(text);
        Debug.Log(sortedText);
    }

    // [MenuItem("Editor/SortHan3000")]
    public static void SortHan3000() {
        string text = File.ReadAllText("Assets/Editor/Resources/3500+symbols.txt");
        string sortedText = SortByCodePoint(text);
        Debug.Log(sortedText);
    }
    
    private static string SortByCodePoint(string text) {
        List<int> codePointArray = new List<int>(text.Length);
        for (int i = 0, length = text.Length; i < length; i++) {
            char c = text[i];
            int unicode;
            if (char.IsSurrogate(c)) {
                unicode = char.ConvertToUtf32(c, text[++i]);
            } else {
                unicode = c;
            }
            codePointArray.Add(unicode);
        }
        // 按unicode排序 -- 再转换字符串
        codePointArray.Sort();
        StringBuilder sb = new StringBuilder(text.Length);
        foreach (int codePoint in codePointArray) {
            if (codePoint < 65536) {
                sb.Append((char)codePoint);
            } else {
                sb.Append(char.ConvertFromUtf32(codePoint));
            }
        }
        string sortedText = sb.ToString();
        return sortedText;
    }
}
}