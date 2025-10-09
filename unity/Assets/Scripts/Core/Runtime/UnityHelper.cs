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
using System.Runtime.CompilerServices;

#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif

#if UNITY_EDITOR
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
public static class UnityHelper
{
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

    /// <summary>
    /// 是否是图片文件(测试文件路径后缀)
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static bool IsImageFile(string filePath) {
        if (string.IsNullOrWhiteSpace(filePath)) return false;
        return filePath.EndsWith(".png")
               || filePath.EndsWith(".jpg")
               || filePath.EndsWith(".psd")
               || filePath.EndsWith(".tga");
        // 其它还有tif
    }

    /// <summary>
    /// 是否是音效文件
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static bool IsAudioFile(string filePath) {
        if (string.IsNullOrWhiteSpace(filePath)) return false;
        return filePath.EndsWith(".ogg")
               || filePath.EndsWith(".mp3")
               || filePath.EndsWith(".wav")
               || filePath.EndsWith(".flac");
    }

#if UNITY_EDITOR

    #region asset-path

    /// <summary>
    /// 将文件路径转换为资产路径
    /// </summary>
    /// <param name="filePath"></param>
    public static string ConvertToAssetPath(string filePath) {
        if (string.IsNullOrWhiteSpace(filePath)) {
            return filePath;
        }
        if (filePath.StartsWith("Assets")) {
            return filePath.Replace('\\', '/');
        }
        return filePath.Replace(Application.dataPath, "Assets").Replace('\\', '/');
    }

    /// <summary>
    /// 将资产路径转换为文件路径
    /// </summary>
    /// <param name="assetPath"></param>
    /// <returns></returns>
    public static string ConvertToFilePath(string assetPath) {
        // "assets/sprites/xx" 
        return Application.dataPath + assetPath.Substring(6);
    }

    /// <summary>
    /// 将资产路径转换为文件夹路径
    /// </summary>
    /// <returns></returns>
    public static string GetAssetFolderPath(UnityEngine.Object obj) {
        string assetPath = AssetDatabase.GetAssetPath(obj);
        if (assetPath.LastIndexOf('.') > 0) { // 文件
            return assetPath.Substring(0, assetPath.LastIndexOf('/'));
        }
        return assetPath;
    }

    #endregion

    #region draw

    /// <summary>
    /// 展开状态标识
    /// </summary>
    public const string SYMBOL_FOLD_OUT = "▼";
    /// <summary>
    /// 折叠状态标识
    /// </summary>
    public const string SYMBOL_FOLD_UP = "▶";

    /// <summary>
    /// 收起和展开的符号
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetFoldoutSymbol(bool b) {
        return b ? "▼" : "▶";
    }

    /** 单行绘制Vector2 */
    public static Vector2 DrawVector2(string label, Vector2 value) {
        bool wideMode = EditorGUIUtility.wideMode;
        EditorGUIUtility.wideMode = true; // 强制单行显示
        value = EditorGUILayout.Vector2Field(label, value);
        EditorGUIUtility.wideMode = wideMode;
        return value;
    }

    /** 单行绘制Vector3 */
    public static Vector3 DrawVector3(string label, Vector3 value) {
        bool wideMode = EditorGUIUtility.wideMode;
        EditorGUIUtility.wideMode = true; // 强制单行显示
        value = EditorGUILayout.Vector3Field(label, value);
        EditorGUIUtility.wideMode = wideMode;
        return value;
    }

    /** 绘制分割线 */
    public static void DrawSeparator() {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, Color.gray);
    }

    /** 绘制分割线 */
    public static void DrawSeparator(Color color) {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, color);
    }

    #endregion

    #region GUIContent

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this GUIContent content) {
        return string.IsNullOrEmpty(content.text)
               && string.IsNullOrEmpty(content.tooltip)
               && !content.image;
    }

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
    public static GUIContent WithImage(this GUIContent content, Texture image) {
        content.image = image;
        return content;
    }

    #endregion

#endif
}
}