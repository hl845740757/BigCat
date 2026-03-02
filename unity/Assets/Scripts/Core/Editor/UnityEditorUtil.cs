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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Wjybxx.BigCat.Core;
using Wjybxx.BigCatTool;
using Wjybxx.BTree;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Codec.Attributes;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;
using Blackboard = Wjybxx.BigCat.Util.Blackboard;

namespace Wjybxx.BigCat.Editor
{
/// <summary>
/// Unity工具类
/// </summary>
public static class UnityEditorUtil
{
    /// <summary>
    /// 列表元素的名字缓存，避免频繁构建字符串
    /// </summary>
    private static readonly string[] elementNameCache = new string[100];

    static UnityEditorUtil() {
        for (int index = 0; index < elementNameCache.Length; index++) {
            elementNameCache[index] = "Ln" + (index + 1);
        }
    }

    /// <summary>
    /// 获取编辑器模式下，数组元素的名字
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public static string GetElementName(int index) {
        return index >= 0 && index < elementNameCache.Length ? elementNameCache[index] : "Ln" + (index + 1);
    }

    /// <summary>
    /// 通过数组元素的名字计算元素的下标
    /// </summary>
    /// <param name="elementName"></param>
    /// <returns></returns>
    public static int GetElementIndex(string elementName) {
        // Element 0
        int spIndex = elementName.LastIndexOf(' ');
        if (spIndex < 0) {
            throw new ArgumentException("Invalid element name: " + elementName);
        }
        return int.Parse(elementName.AsSpan(spIndex + 1));
    }

    /// <summary>
    /// 检测obj的有效性
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CheckObject(UnityEngine.Object obj) {
        return obj;
    }

    public static readonly ImmutableList<string> imageFileExtensions = new string[] { "png", "jpg", "tif" }.ToImmutableList2();
    public static readonly ImmutableList<string> audioFileExtensions = new string[] { "ogg", "wav", "mp3" }.ToImmutableList2();

    public static readonly Encoding UTF8 = new UTF8Encoding(false);
    private static string _lastOpenFolder = "Assets";
    /// <summary>
    /// 上次打开的文件夹路径
    /// </summary>
    public static string lastOpenFolder {
        get => _lastOpenFolder;
        set => _lastOpenFolder = value;
    }
    /// <summary>
    /// Sprite的默认搜索文件夹
    /// </summary>
    /// <returns></returns>
    public static readonly string[] spriteSearchFolders = new[]
    {
        "Assets/GameRes/Sprites",
        "Assets/DNF/Sprites",
        // "Assets/DNF/Animations",
    };

    /// <summary>
    /// 是否是图片文件(测试文件路径后缀)
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static bool IsImageFile(string filePath) {
        if (string.IsNullOrWhiteSpace(filePath)) return false;
        foreach (string extension in imageFileExtensions) {
            if (filePath.EndsWith(extension)) return true;
        }
        return false;
    }

    /// <summary>
    /// 是否是音效文件
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static bool IsAudioFile(string filePath) {
        if (string.IsNullOrWhiteSpace(filePath)) return false;
        foreach (string extension in audioFileExtensions) {
            if (filePath.EndsWith(extension)) return true;
        }
        return false;
    }

    /// <summary>
    /// Ping一下目标资产对象（或文件夹）
    /// </summary>
    /// <param name="assetPath"></param>
    public static void PingObject(string assetPath) {
        if (string.IsNullOrWhiteSpace(assetPath)) {
            return;
        }
        UnityEngine.Object someObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (someObj) {
            EditorGUIUtility.PingObject(someObj);
        }
    }

    /// <summary>
    /// 显示进度条
    /// </summary>
    public static void DisplayProgressBar(string tips, int progressValue, int totalValue) {
        EditorUtility.DisplayProgressBar("进度", $"{tips} : {progressValue} / {totalValue}", (float)progressValue / totalValue);
    }

    #region focus-window

    public static void FocusUnitySceneWindow() {
        EditorWindow.FocusWindowIfItsOpen<SceneView>();
    }

    public static void CloseUnityGameWindow() {
        Type type = Assembly.Load("UnityEditor").GetType("UnityEditor.GameView");
        EditorWindow.FocusWindowIfItsOpen(type);
    }

    public static void FocusUnityGameWindow() {
        Type type = Assembly.Load("UnityEditor").GetType("UnityEditor.GameView");
        EditorWindow.FocusWindowIfItsOpen(type);
    }

    public static void FocusUnityProjectWindow() {
        Type type = Assembly.Load("UnityEditor").GetType("UnityEditor.ProjectBrowser");
        EditorWindow.FocusWindowIfItsOpen(type);
    }

    public static void FocusUnityHierarchyWindow() {
        Type type = Assembly.Load("UnityEditor").GetType("UnityEditor.SceneHierarchyWindow");
        EditorWindow.FocusWindowIfItsOpen(type);
    }

    public static void FocusUnityInspectorWindow() {
        Type type = Assembly.Load("UnityEditor").GetType("UnityEditor.InspectorWindow");
        EditorWindow.FocusWindowIfItsOpen(type);
    }

    public static void FocusUnityConsoleWindow() {
        Type type = Assembly.Load("UnityEditor").GetType("UnityEditor.ConsoleWindow");
        EditorWindow.FocusWindowIfItsOpen(type);
    }

    #endregion

    #region asset-path

    public static string OpenFolderPanel(string title, string folder, string defaultName = "") {
        string openPath = EditorUtility.OpenFolderPanel(title, folder, defaultName);
        if (string.IsNullOrEmpty(openPath)) {
            return null;
        }
        if (!openPath.Contains("/Assets")) {
            Debug.LogWarning("Please select unity assets folder.");
            return null;
        }
        UnityEditorUtil.lastOpenFolder = ConvertToAssetPath(openPath);
        return openPath;
    }

    public static string OpenFilePanel(string title, string directory, string extension = "") {
        string openPath = EditorUtility.OpenFilePanel(title, directory, extension);
        if (string.IsNullOrEmpty(openPath)) {
            return null;
        }
        if (!openPath.Contains("/Assets")) {
            Debug.LogWarning("Please select unity assets file.");
            return null;
        }
        UnityEditorUtil.lastOpenFolder = GetAssetFolderPath(openPath);
        return openPath;
    }

    /// <summary>
    /// 规格化资产路径
    ///
    /// 1.文件扩展名之前的部分转小写，扩展名不转小写。
    /// 2.运行时可通过缓存StringBuilder优化开销。
    /// </summary>
    /// <param name="assetPath"></param>
    /// <returns></returns>
    public static string NormalizeAssetPath(string assetPath) {
        return ToolUtil.NormalizeAssetPath(assetPath);
    }

    /// <summary>
    /// 获取文件扩展名
    /// 注：不包含点号，而<code>Path.GetExtension</code>的返回值包含点号。
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string GetExtension(string path) {
        int index = path.LastIndexOf('.');
        if (index >= 0 && index > path.LastIndexOf('/')) {
            return path.Substring(index + 1);
        }
        return "";
    }

    /// <summary>
    /// 是否是给定
    /// </summary>
    /// <param name="path"></param>
    /// <param name="subPath"></param>
    /// <returns></returns>
    public static bool IsSubPath(string path, string subPath) {
        return subPath.Length > path.Length
               && subPath[path.Length] == '/'
               && subPath.StartsWith(path, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 将文件路径转换为资产路径
    /// </summary>
    /// <param name="filePath">文件路径</param>
    public static string ConvertToAssetPath(string filePath) {
        if (string.IsNullOrWhiteSpace(filePath)) {
            throw new ArgumentNullException(nameof(filePath));
        }
        if (!filePath.StartsWith("Assets")) {
            filePath = filePath.Replace(Application.dataPath, "Assets");
        }
        return filePath.Replace('\\', '/');
    }

    /// <summary>
    /// 将资产路径转换为文件路径
    /// </summary>
    /// <param name="assetPath"></param>
    /// <returns></returns>
    public static string ConvertToFilePath(string assetPath) {
        if (string.IsNullOrEmpty(assetPath)) {
            throw new ArgumentNullException(nameof(assetPath));
        }
        // "assets/sprites/xx" 
        if (assetPath.StartsWith("Assets")) {
            return Application.dataPath + assetPath.Substring(6);
        }
        // "../../" 指向外部文件，当前工作目录就是Unity项目根目录
        return assetPath;
    }

    /// <summary>
    /// 获取资产的路径
    /// </summary>
    /// <param name="assetPath"></param>
    /// <returns></returns>
    public static string GetAssetFolderPath(string assetPath) {
        int lastIndex = assetPath.LastIndexOf('/');
        if (lastIndex == -1 || assetPath.IndexOf('.', lastIndex) < 0) {
            return assetPath;
        }
        return assetPath.Substring(0, lastIndex);
    }

    /// <summary>
    /// 将资产路径转换为文件夹路径
    /// </summary>
    /// <returns></returns>
    public static string GetAssetFolderPath(UnityEngine.Object obj) {
        string assetPath = AssetDatabase.GetAssetPath(obj);
        int idx = assetPath.LastIndexOf('.');
        return idx > 0 ? assetPath.Substring(0, assetPath.LastIndexOf('/')) : assetPath;
    }

    /// <summary>
    /// 获取最后一级文件夹名
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string GetLastDirectoryName(string path) {
        string directoryName = Path.GetDirectoryName(path);
        return string.IsNullOrEmpty(directoryName) ? null : Path.GetFileName(directoryName);
    }

    #endregion

    #region object-path

    /// <summary>
    /// 加载图片
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static Sprite LoadSprite(ObjectPath path) {
        if (path.IsEmpty) {
            return null;
        }
        ObjectPathType type = (ObjectPathType)path.type;
        return type switch
        {
            ObjectPathType.SpriteOfGroup => SpriteGroupLoader.LoadSprite(path),
            _ => null
        };
    }

    public static SpriteGroup LoadSpriteGroup(string groupPath) {
        return SpriteGroupLoader.LoadSpriteGroup(groupPath);
    }

    private static class SpriteGroupLoader
    {
        private static readonly Dictionary<string, SpriteGroup> _nameToSpriteGroup = new(StringComparer.OrdinalIgnoreCase);
        private static double _lastSearchTime;

        public static SpriteGroup LoadSpriteGroup(string groupPath) {
            if (string.IsNullOrWhiteSpace(groupPath)) {
                return null;
            }
            // 替换变量占位符 {sm_body8001} => sm_body8001 
            int idx = groupPath.IndexOf('{');
            if (idx >= 0) {
                int endIdx = groupPath.LastIndexOf('}');
                if (endIdx < idx) {
                    throw new Exception($"Invalid group path: {groupPath}");
                }
                groupPath = groupPath.Substring2(idx + 1, endIdx);
            }
            if (groupPath.LastIndexOf('/') > 0) {
                return AssetDatabase.LoadAssetAtPath<SpriteGroup>(groupPath);
            }
            // name引用
            string assetName = groupPath;
            if (_nameToSpriteGroup.TryGetValue(assetName, out SpriteGroup spriteGroup)) {
                if (spriteGroup && assetName == spriteGroup.name) {
                    return spriteGroup;
                }
                _nameToSpriteGroup.Remove(assetName);
            }
            // 避免频繁检索资源
            if (Time.realtimeSinceStartup - _lastSearchTime < 1) {
                return null;
            }
            _lastSearchTime = Time.realtimeSinceStartup;
            foreach (string guid in AssetDatabase.FindAssets("t:SpriteGroup", spriteSearchFolders)) {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                spriteGroup = AssetDatabase.LoadAssetAtPath<SpriteGroup>(assetPath);
                if (!spriteGroup) {
                    continue;
                }
                string folderName = GetLastDirectoryName(assetPath);
                _nameToSpriteGroup[spriteGroup.name] = spriteGroup;
                _nameToSpriteGroup[$"{folderName}/{spriteGroup.name}"] = spriteGroup;
                //
                _nameToSpriteGroup[$"{nameof(SpriteGroup)}:{spriteGroup.name}"] = spriteGroup;
                _nameToSpriteGroup[$"{nameof(SpriteGroup)}:{folderName}/{spriteGroup.name}"] = spriteGroup;
            }
            _nameToSpriteGroup.TryGetValue(assetName, out spriteGroup);
            return spriteGroup;
        }

        public static Sprite LoadSprite(ObjectPath spritePath) {
            if (spritePath.IsEmpty || spritePath.localId < 0) { // localId为索引
                return null;
            }
            SpriteGroup spriteGroup = LoadSpriteGroup(spritePath.collection);
            if (spriteGroup) {
                return spriteGroup.GetSprite(spritePath);
            }
            return null;
        }
    }

    #endregion

    #region ugui

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

    #region uitoolkit

    private static PropertyInfo _listFoldoutProperty;
    private static PropertyInfo _foldoutToggleProperty;

    public static void SetDisplay(this VisualElement element, bool display) {
        element.style.display = display ? DisplayStyle.Flex : DisplayStyle.None;
    }

    internal static Foldout GetFoldout(this ListView listView) {
        // 也可通过Query查询，但效率差一点，开销也大
        if (_listFoldoutProperty == null) {
            _listFoldoutProperty = typeof(ListView).GetProperty("headerFoldout",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
        return (Foldout)_listFoldoutProperty!.GetValue(listView, null);
    }

    internal static void SetFoldout(this ListView listView, bool value) {
        Foldout foldout = GetFoldout(listView);
        if (foldout != null) {
            foldout.value = value;
        }
    }

    internal static Toggle GetToggle(this Foldout foldout) {
        if (_foldoutToggleProperty == null) {
            _foldoutToggleProperty = typeof(Foldout).GetProperty("toggle",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
        return (Toggle)_foldoutToggleProperty!.GetValue(foldout, null);
    }

    internal static void SetLabelMargin<T>(this BaseField<T> field, float labelMargin) {
        field.labelElement.style.marginRight = labelMargin;
    }

    internal static void SetVectorFieldFlexBasis(VisualElement field, float flexBasis) {
        VisualElement values = field.childCount == 1 ? field[0] : field[1]; // label为空会从层次中删除
        for (int i = 0; i < values.childCount; i++) {
            values[i].style.flexGrow = 1;
            values[i].style.flexShrink = 1;
            values[i].style.flexBasis = flexBasis;
            values[i].style.minWidth = 40;
        }
    }

    internal static void SetVectorFieldReadonly(VisualElement field, bool isReadOnly) {
        VisualElement values = field.childCount == 1 ? field[0] : field[1];
        for (int i = 0; i < values.childCount; i++) {
            if (values[i] is FloatField floatField) {
                floatField.isReadOnly = isReadOnly;
            } else if (values[i] is IntegerField integerField) {
                integerField.isReadOnly = isReadOnly;
            }
        }
    }

    internal static void SetVectorFieldDelayed(VisualElement field, bool isDelayed) {
        VisualElement values = field.childCount == 1 ? field[0] : field[1];
        for (int i = 0; i < values.childCount; i++) {
            if (values[i] is FloatField floatField) {
                floatField.isDelayed = isDelayed;
            } else if (values[i] is IntegerField integerField) {
                integerField.isDelayed = isDelayed;
            }
        }
    }

    internal static T FindUserContextInParent<T>(this VisualElement element) where T : class {
        for (int i = 0; i < 5; i++) {
            element = element.parent;
            if (element == null) return null;
            if (element.userData is T userData) return userData;
        }
        return null;
    }

    internal static void SetMargin(this VisualElement element, int width) {
        element.style.marginLeft = width;
        element.style.marginRight = width;
        element.style.marginTop = width;
        element.style.marginBottom = width;
    }

    internal static void SetBorderWidth(this VisualElement element, int width) {
        element.style.borderLeftWidth = width;
        element.style.borderRightWidth = width;
        element.style.borderTopWidth = width;
        element.style.borderBottomWidth = width;
    }

    internal static void SetBorderColor(this VisualElement element, Color color) {
        element.style.borderLeftColor = color;
        element.style.borderRightColor = color;
        element.style.borderTopColor = color;
        element.style.borderBottomColor = color;
    }

    #endregion

    #region convert

    public static Color32 AsColor32(Integer4 integer4) {
        return new Color32((byte)integer4.v0, (byte)integer4.v1, (byte)integer4.v2, (byte)integer4.v3);
    }

    public static Integer4 AsInteger4(Color32 color) {
        return new Integer4(color.r, color.g, color.b, color.a);
    }

    public static Quaternion AsQuaternion(Vector4 vector4) {
        return new Quaternion(vector4.x, vector4.y, vector4.z, vector4.w);
    }

    public static Vector4 AsVector4(Quaternion quaternion) {
        return new Vector4(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
    }

    /// <summary>
    /// 量化
    /// </summary>
    /// <param name="value">要量化的值</param>
    /// <param name="q">量化步长</param>
    /// <returns></returns>
    public static Vector2 Quantize(this Vector2 value, Vector2 q) {
        return Vector2.Scale(q, new Vector2(
            Mathf.Floor(value.x / q.x),
            Mathf.Floor(value.y / q.y))
        );
    }

    public static Vector3 Quantize(this Vector3 value, Vector3 q) {
        return Vector3.Scale(q, new Vector3(
            Mathf.Floor(value.x / q.x),
            Mathf.Floor(value.y / q.y),
            Mathf.Floor(value.z / q.z))
        );
    }

    /// <summary>
    /// 将浮点数截断为整数
    /// </summary>
    /// <param name="vector"></param>
    internal static void Truncate(ref Vector2 vector) {
        vector.x = (int)vector.x;
        vector.y = (int)vector.y;
    }

    internal static void Truncate(ref Vector3 vector) {
        vector.x = (int)vector.x;
        vector.y = (int)vector.y;
        vector.z = (int)vector.z;
    }

    #endregion

    #region SerializedProperty

    public static void WriteProperty(this MinMaxAABB box, SerializedProperty property) {
        using SerializedProperty pValue = property.Copy();
        pValue.Next(true);
        pValue.vector3Value = box.min;

        pValue.Next(false);
        pValue.vector3Value = box.max;
    }

    public static void ReadProperty(ref MinMaxAABB box, SerializedProperty property) {
        using SerializedProperty pValue = property.Copy();
        pValue.Next(true);
        box.min = pValue.vector3Value;

        pValue.Next(false);
        box.max = pValue.vector3Value;
    }

    public static void WriteProperty(this ObjectPath path, SerializedProperty property) {
        using var pValue = property.Copy();
        pValue.Next(true);
        pValue.stringValue = path.collection;

        pValue.Next(false);
        pValue.stringValue = path.localPath;

        pValue.Next(false);
        pValue.intValue = path.localId;

        pValue.Next(false);
        pValue.intValue = path.type;
    }

    public static void ReadProperty(ref ObjectPath path, SerializedProperty property) {
        using var pValue = property.Copy();
        pValue.Next(true);
        path.collection = pValue.stringValue;

        pValue.Next(false);
        path.localPath = pValue.stringValue;

        pValue.Next(false);
        path.localId = pValue.intValue;

        pValue.Next(false);
        path.type = pValue.intValue;
    }

    #endregion

    #region 公共的序列化支持

    private static IDsonConverter _converter;

    /// <summary>
    /// 获取公共的序列化工具
    /// </summary>
    public static IDsonConverter Converter => _converter ?? CreateConverter();

    private static IDsonConverter CreateConverter() {
        ConverterOptions options = new ConverterOptions.Builder
        {
            TextWriterSettings = new DsonTextWriterSettings.Builder()
            {
                NumberStyle = NumberStyle.Simple,
                MaxLengthOfUnquoteString = 32,
            }.Build() as DsonTextWriterSettings
        }.Build();
        DsonConverterBuilder builder = new DsonConverterBuilder() { Options = options };
        TypeCache.TypeCollection codecTypes = TypeCache.GetTypesDerivedFrom<IDsonCodec>();
        for (int i = 0; i < codecTypes.Count; i++) {
            Type codecType = codecTypes[i];
            if (codecType.IsAbstract || codecType.IsInterface) continue;
            if (!IsWjybxxLogicNamespace(codecType.Namespace)) {
                continue;
            }
            builder.AddByCodecType(codecType);
        }
        // 补充元数据（不序列化但被使用到的类型）
        builder.AddTypeMeta(TypeMeta.Of(typeof(Task<>), "Task"));
        builder.AddTypeMeta(TypeMeta.Of(typeof(Blackboard), "Blackboard"));
        // 大量的枚举被使用
        foreach (Type type in TypeCache.GetTypesDerivedFrom<Enum>()) {
            if (type.IsNested) continue;
            if (!IsWjybxxLogicNamespace(type.Namespace)) {
                continue;
            }
            builder.AddTypeMeta(TypeMeta.Of(type, type.Name));
        }
        return builder.Build();
    }

    // TODO 可以通过注解导入
    private static bool IsWjybxxLogicNamespace(string ns) {
        if (string.IsNullOrEmpty(ns)) {
            return false;
        }
        return ns.StartsWith("Wjybxx.")
               && !ns.StartsWith("Wjybxx.Common")
               && !ns.StartsWith("Wjybxx.Dson");
    }

    #endregion
}
}