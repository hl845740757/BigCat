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
using System.Linq;

#if UNITY_2021_3_OR_NEWER
using UnityEngine;
using System.Runtime.CompilerServices;
#endif

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace System
{
/// <summary>
/// 系统类扩展
///
/// 1.主要给Unity用
/// 2.特意命名System命名空间
/// </summary>
public static class SystemExtensions
{
#if UNITY_2021_3_OR_NEWER
    public static void EnsureCapacity<T>(this List<T> list, int capacity) {
        if (list.Capacity >= capacity) {
            return;
        }
        if (capacity <= 4) {
            list.Capacity = 4;
            return;
        }
        int newCapacity = list.Capacity + list.Capacity / 2;
        list.Capacity = Math.Max(newCapacity, capacity);
    }

    /// <summary>
    /// 检测obj的有效性
    /// (用于Lua项目)
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CheckObject(UnityEngine.Object obj) {
        return obj;
    }
#endif

#if UNITY_EDITOR

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GUIContent Reset(this GUIContent content) {
        content.text = "";
        content.tooltip = "";
        content.image = null;
        return content;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GUIContent WithText(this GUIContent content, string text) {
        content.text = text;
        return content;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GUIContent WithText(this GUIContent content, string text, string tooltip) {
        content.text = text;
        content.tooltip = tooltip;
        return content;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GUIContent WithTooltip(this GUIContent content, string tooltip) {
        content.tooltip = tooltip;
        return content;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GUIContent WithImage(this GUIContent content, Texture image) {
        content.image = image;
        return content;
    }

    /// <summary>
    /// 将文件路径转换为资产路径
    /// </summary>
    /// <param name="filePath"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConvertToAssetPath(string filePath) {
        if (string.IsNullOrWhiteSpace(filePath)) {
            return filePath;
        }
        if (filePath.StartsWith("Assets")) {
            return filePath;
        }
        return filePath.Replace(Application.dataPath, "Assets");
    }

    /// <summary>
    /// 是否是图片文件(测试文件路径后缀)
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static bool IsImageFile(string filePath) {
        if (string.IsNullOrWhiteSpace(filePath)) return false;
        return filePath.EndsWith(".png") || filePath.EndsWith(".jpg") || filePath.EndsWith(".jpeg");
    }

    /// <summary>
    /// 加载指定目录下的所有资产文件
    ///
    /// 以笨办法加载指定目录下的特定类型资产文件。
    /// </summary>
    /// <param name="folderPath">文件夹</param>
    /// <param name="extensions">文件扩展名</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<T> LoadAllAssetsAtPath<T>(string folderPath, params string[] extensions) {
        List<T> result = new List<T>();
        foreach (string filePath in Directory.GetFiles(folderPath)) {
            bool contains = extensions.Length switch
            {
                0 => !filePath.EndsWith(".meta"),
                1 => filePath.EndsWith(extensions[0]),
                _ => extensions.Any(extension => filePath.EndsWith(extension))
            };
            if (!contains) {
                continue;
            }
            string assetPath = ConvertToAssetPath(filePath);
            if (AssetDatabase.LoadAssetAtPath(assetPath, typeof(T)) is T asset) {
                result.Add(asset);
            }
        }
        return result;
    }
#endif
}
}