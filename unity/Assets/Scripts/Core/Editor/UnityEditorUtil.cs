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
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Wjybxx.BigCat.Core;
using Wjybxx.BigCat.CoreEditor.UIElements;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCat.CoreEditor
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
            elementNameCache[index] = "Element " + index;
        }
    }

    /// <summary>
    /// 获取编辑器模式下，数组元素的名字
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public static string GetElementName(int index) {
        return index >= 0 && index < elementNameCache.Length ? elementNameCache[index] : "Element " + index;
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

    public static readonly ImmutableList<string> imageFileExtensions = new string[] { "png", "jpg", "psd" }.ToImmutableList2();
    public static readonly ImmutableList<string> audioFileExtensions = new string[] { "ogg", "wav", "mp3" }.ToImmutableList2();

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
    /// 获取资产的路径
    /// </summary>
    /// <param name="assetPath"></param>
    /// <returns></returns>
    public static string GetAssetFolderPath(string assetPath) {
        int idx = assetPath.LastIndexOf('.');
        return idx > 0 ? assetPath.Substring(0, assetPath.LastIndexOf('/')) : assetPath;
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

    #endregion

    #region draw

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

    #region check-event

    /// <summary>
    /// 鼠标左键事件
    /// </summary>
    public static bool IsPrimaryClickEvent(Event evt) {
        return evt.type == EventType.MouseDown && evt.button == 0;
    }

    /// <summary>
    /// 鼠标左键事件
    /// </summary>
    public static bool IsPrimaryClickEvent(Event evt, Rect rect) {
        return evt.type == EventType.MouseDown
               && evt.button == 0 && rect.Contains(evt.mousePosition);
    }

    /// <summary>
    /// 鼠标右键事件
    /// </summary>
    public static bool IsContextClickEvent(Event evt, Rect rect) {
        return evt.type == EventType.ContextClick && rect.Contains(evt.mousePosition);
    }

    /// <summary>
    /// 回车键事件
    /// </summary>
    public static bool IsClickEnterEvent(Event evt) {
        return evt.type == EventType.KeyDown
               && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter);
    }

    /// <summary>
    /// Ping一下目标资产对象
    /// </summary>
    public static void CheckPingObjectEvent(string assetPath, Event evt, Rect controlRect) {
        if (string.IsNullOrWhiteSpace(assetPath) || !IsPrimaryClickEvent(evt, controlRect)) {
            return;
        }
        UnityEngine.Object someObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (someObj) {
            EditorGUIUtility.PingObject(someObj);
        }
    }

    /// <summary>
    /// Ping一下目标资产对象
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

    #endregion

    #region object-path

    /// <summary>
    /// 上次打开的文件夹路径
    /// </summary>
    public static string lastOpenFolder = "Assets";
    /// <summary>
    /// Sprite的默认搜索文件夹
    /// </summary>
    /// <returns></returns>
    public static readonly string[] spriteSearchFolders = new[] { "Assets/Resources/Sprites", "Assets/Sprites" };

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
        private static readonly Dictionary<string, SpriteGroup> _nameToSpriteGroup = new();
        private static double _lastSearchTime;

        public static SpriteGroup LoadSpriteGroup(string groupPath) {
            if (string.IsNullOrWhiteSpace(groupPath)) {
                return null;
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
                if (spriteGroup.preferName) {
                    _nameToSpriteGroup[spriteGroup.name] = spriteGroup;
                }
            }
            _nameToSpriteGroup.TryGetValue(assetName, out spriteGroup);
            return spriteGroup;
        }

        public static Sprite LoadSprite(ObjectPath spritePath) {
            if (spritePath.IsEmpty || spritePath.localId < 0) {
                return null;
            }
            SpriteGroup spriteGroup = LoadSpriteGroup(spritePath.collection);
            if (spriteGroup) {
                return spriteGroup.GetSprite((int)spritePath.localId);
            }
            return null;
        }
    }

    #endregion

    #region GUIContent

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
    

    #endregion

    #region uitoolkit

    private static PropertyInfo _listFoldoutProperty;

    public static void SetDisplay(this VisualElement element, bool display) {
        element.style.display = display ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public static Foldout GetFoldout(this ListView listView) {
        // 也可通过Query查询，但效率差一点，开销也大
        if (_listFoldoutProperty == null) {
            _listFoldoutProperty = typeof(ListView).GetProperty("headerFoldout",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
        return (Foldout)_listFoldoutProperty!.GetValue(listView, null);
    }

    public static void SetFoldout(this ListView listView, bool value) {
        Foldout foldout = GetFoldout(listView);
        if (foldout != null) {
            foldout.value = value;
        }
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

    internal static void SetVectorFieldLabel(VisualElement field, IList<string> labels) {
        VisualElement values = field.childCount == 1 ? field[0] : field[1];
        for (int i = 0; i < values.childCount; i++) {
            if (values[i] is FloatField floatField) {
                floatField.label = labels[i];
            } else if (values[i] is IntegerField integerField) {
                integerField.label = labels[i];
            }
        }
    }

    internal static ObjectPathField QueryObjectPathField(this VisualElement container, string name) {
        return (ObjectPathField)container.Q(name)[0];
    }

    internal static AABBField QueryAABBField(this VisualElement container, string name) {
        return (AABBField)container.Q(name)[0];
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

    #endregion

    #region MyRegion

    public static int AsInt32(Color32 color) {
        return color.r | color.g << 8 | color.b << 16 | color.a << 24;
    }

    public static Color32 AsColor32(int rgba) {
        byte r = (byte)(rgba & 0xff);
        byte g = (byte)(rgba >> 8);
        byte b = (byte)(rgba >> 16);
        byte a = (byte)(rgba >> 24);
        return new Color32(r, g, b, a);
    }

    public static Quaternion AsQuaternion(Vector4 vector4) {
        return new Quaternion(vector4.x, vector4.y, vector4.z, vector4.w);
    }

    public static Vector4 AsVector4(Quaternion quaternion) {
        return new Vector4(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
    }

    internal static void Truncate(ref Vector2 vector) {
        vector.x = (int)vector.x;
        vector.y = (int)vector.y;
    }

    internal static void Truncate(ref Vector3 vector) {
        vector.x = (int)vector.x;
        vector.y = (int)vector.y;
        vector.z = (int)vector.z;
    }

    public static void WriteProperty(this MinMaxAABB box, SerializedProperty property) {
        using SerializedProperty pValue = property.Copy();
        pValue.Next(true);
        pValue.vector3Value = box.min;

        pValue.Next(false);
        pValue.vector3Value = box.max;
    }

    public static void ReadProperty(this MinMaxAABB box, SerializedProperty property) {
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
        pValue.longValue = path.localId;

        pValue.Next(false);
        pValue.intValue = path.type;
    }

    public static void ReadProperty(this ObjectPath path, SerializedProperty property) {
        using var pValue = property.Copy();
        pValue.Next(true);
        path.collection = pValue.stringValue;

        pValue.Next(false);
        path.localPath = pValue.stringValue;

        pValue.Next(false);
        path.localId = pValue.longValue;

        pValue.Next(false);
        path.type = pValue.intValue;
    }

    #endregion
}
}