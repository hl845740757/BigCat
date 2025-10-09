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
using UnityEditor;
using UnityEngine;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.Dson.Types;
using Object = UnityEngine.Object;

namespace Wjybxx.BigCat.CoreEditor
{
public static class DataEditorUtil
{
    #region util

    /** 定位资源 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckPingObjectEvent(string assetPath, Event evt, Rect controlRect) {
        UnityHelper.CheckPingObjectEvent(assetPath, evt, controlRect);
    }

    public static bool IsPrimaryClickEvent(Event evt) {
        return evt.type == EventType.MouseDown && evt.button == 0;
    }

    public static bool IsPrimaryClickEvent(Event evt, Rect rect) {
        return evt.type == EventType.MouseDown && evt.button == 0 && rect.Contains(evt.mousePosition);
    }

    public static bool IsContextClickEvent(Event evt, Rect rect) {
        return evt.type == EventType.ContextClick && rect.Contains(evt.mousePosition);
    }

    /// <summary>
    /// 如果返回null则表示当前不可展示
    /// </summary>
    /// <param name="container"></param>
    /// <param name="branchCfgs"></param>
    /// <returns></returns>
    public static BranchFieldCfg FilterBranchFieldCfg(DataVariable container, List<BranchFieldCfg> branchCfgs) {
        for (int index = 0; index < branchCfgs.Count; index++) {
            BranchFieldCfg branchCfg = branchCfgs[index];
            DataVariable ctrlValue = container.values[branchCfg.ctrlIndex];
            Debug.Assert(ctrlValue.defineInfo.SimpleName == branchCfg.ctrl);
            if (ctrlValue.type.SimpleName == DSKeywords.TYPE_STRING) {
                if (ctrlValue.stringValue == branchCfg.value) {
                    return branchCfg;
                }
            } else {
                if (ctrlValue.intValue == branchCfg.intValue) {
                    return branchCfg;
                }
            }
        }
        return null;
    }

    #endregion

    #region atomic

    public class Int32VariableDrawer : DataVariableDrawer
    {
        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            DataDisplayCfg displayCfg = variable.displayCfg;
            if (displayCfg.HasPopNames) {
                variable.intValue = EditorGUILayout.IntPopup(label, variable.intValue, displayCfg.popNames, displayCfg.intPopValues);
                return;
            }
            if (displayCfg.HasMaskNames) {
                variable.intValue = EditorGUILayout.MaskField(label, variable.intValue, displayCfg.maskNames);
                return;
            }
            // 标签字段需要由上层处理
            int intValue = EditorGUILayout.IntField(label, variable.intValue);
            if (displayCfg.min != null) {
                intValue = Math.Max(intValue, displayCfg.min.IntValue);
            }
            if (displayCfg.max != null) {
                intValue = Math.Min(intValue, displayCfg.max.IntValue);
            }
            variable.intValue = intValue;
        }
    }

    public class Int64VariableDrawer : DataVariableDrawer
    {
        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            DataDisplayCfg displayCfg = variable.displayCfg;
            if (displayCfg.HasPopNames) {
                variable.intValue = EditorGUILayout.IntPopup(label, variable.intValue, displayCfg.popNames, displayCfg.intPopValues);
                return;
            }
            if (displayCfg.HasMaskNames) {
                variable.intValue = EditorGUILayout.MaskField(label, variable.intValue, displayCfg.maskNames);
                return;
            }
            long longValue = EditorGUILayout.LongField(label, variable.longValue);
            if (displayCfg.min != null) {
                longValue = Math.Max(longValue, displayCfg.min.LongValue);
            }
            if (displayCfg.max != null) {
                longValue = Math.Min(longValue, displayCfg.max.LongValue);
            }
            variable.longValue = longValue;
        }
    }

    public class FloatVariableDrawer : DataVariableDrawer
    {
        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            DataDisplayCfg displayCfg = variable.displayCfg;
            double doubleValue = EditorGUILayout.FloatField(label, variable.floatValue);
            if (displayCfg.min != null) {
                doubleValue = Math.Max(doubleValue, displayCfg.min.DoubleValue);
            }
            if (displayCfg.max != null) {
                doubleValue = Math.Min(doubleValue, displayCfg.max.DoubleValue);
            }
            variable.doubleValue = doubleValue;
        }
    }

    public class DoubleVariableDrawer : DataVariableDrawer
    {
        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            DataDisplayCfg displayCfg = variable.displayCfg;
            double doubleValue = EditorGUILayout.DoubleField(label, variable.doubleValue);
            if (displayCfg.min != null) {
                doubleValue = Math.Max(doubleValue, displayCfg.min.DoubleValue);
            }
            if (displayCfg.max != null) {
                doubleValue = Math.Min(doubleValue, displayCfg.max.DoubleValue);
            }
            variable.doubleValue = doubleValue;
        }
    }

    public class BoolVariableDrawer : DataVariableDrawer
    {
        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            variable.boolValue = EditorGUILayout.Toggle(label, variable.boolValue);
        }
    }

    public class TextVariableDrawer : DataVariableDrawer
    {
        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            DataDisplayCfg displayCfg = variable.displayCfg;
            if (displayCfg.stringPopValues != null) {
                variable.intValue = EditorGUILayout.Popup(label, variable.intValue, displayCfg.stringPopValues);
                variable.stringValue = displayCfg.stringPopValues[variable.intValue].text;
                return;
            }
            variable.stringValue = EditorGUILayout.TextField(label, variable.stringValue);
        }
    }

    public class TextAreaVariableDrawer : DataVariableDrawer
    {
        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(label);
            EditorGUI.indentLevel++;
            variable.stringValue = EditorGUILayout.TextArea(variable.stringValue);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
    }

    public class AssetPathVariableDrawer : DataVariableDrawer
    {
        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            string newPath = EditorGUILayout.TextField(label, variable.stringValue);
            if (newPath != variable.stringValue) {
                newPath = newPath.Replace('\\', '/');
                variable.stringValue = newPath;
            }
            // 左键点击时ping一下
            CheckPingObjectEvent(variable.stringValue, Event.current, GUILayoutUtility.GetLastRect());
        }
    }


    public class EnumVariableDrawer : DataVariableDrawer
    {
        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            DataDisplayCfg enumCfg = editor.model.GetDisplayCfg(variable.type);
            if (variable.displayCfg.maskNames != null) { // 长度为0
                variable.intValue = EditorGUILayout.MaskField(label, variable.intValue, enumCfg.maskNames);
            } else {
                variable.intValue = EditorGUILayout.IntPopup(label, variable.intValue, enumCfg.popNames, enumCfg.intPopValues);
            }
        }
    }


    public class DateTimeVariableDrawer : DataVariableDrawer
    {
        private readonly GUILayoutOption[] _width150 = new[] { GUILayout.Width(150) };
        private readonly GUILayoutOption[] _width100 = new[] { GUILayout.Width(100) };
        private readonly GUILayoutOption[] _width50 = new[] { GUILayout.Width(50) };

        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            // 暂时也先展示为秒吧，不然实时解析字符串的成本也高 - 以后可以支持两种展示模式
            DataVariable varSeconds = variable.values[0];
            DataVariable varNanos = variable.values[1];
            DateTime dateTime = new ExtDateTime(varSeconds.longValue, varNanos.intValue).ToDateTime();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(label);
            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;
            // 
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("yyyy/MM/dd", _width150);
            int year = EditorGUILayout.IntField(dateTime.Year, _width100);
            int month = Math.Clamp(EditorGUILayout.IntField(dateTime.Month, _width50), 1, 12);
            int daysInMonth = DateTime.DaysInMonth(year, month); // 动态修正Day范围
            int day = Math.Clamp(EditorGUILayout.IntField(dateTime.Day, _width50), 0, daysInMonth);
            EditorGUILayout.EndHorizontal();
            //
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("HH:mm:ss", _width150);
            int hour = Math.Clamp(EditorGUILayout.IntField(dateTime.Hour, _width100), 0, 23);
            int minute = Math.Clamp(EditorGUILayout.IntField(dateTime.Minute, _width50), 0, 59);
            int second = Math.Clamp(EditorGUILayout.IntField(dateTime.Second, _width50), 0, 59);
            EditorGUILayout.EndHorizontal();
            //
            EditorGUI.indentLevel = indentLevel;
            EditorGUILayout.EndVertical();
            //
            dateTime = new DateTime(year, month, day, hour, minute, second);
            ExtDateTime extDateTime = ExtDateTime.OfDateTime(dateTime);
            varSeconds.longValue = extDateTime.Seconds;
            varNanos.intValue = extDateTime.Nanos;
        }
    }

    public class TimestampVariableDrawer : DataVariableDrawer
    {
        private readonly GUILayoutOption[] _width60 = new[] { GUILayout.Width(60) };
        private readonly GUILayoutOption[] _width50 = new[] { GUILayout.Width(50) };

        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            DataVariable varSeconds = variable.values[0];
            DataVariable varNanos = variable.values[1];
            EditorGUILayout.BeginVertical();

            // xxx: ____________(s) ___(ms)
            EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.LabelField(label);
            long seconds = EditorGUILayout.LongField(label, varSeconds.longValue);
            if (seconds != varSeconds.longValue) {
                varSeconds.longValue = seconds;
                OnClickPreview(variable);
            }
            EditorGUILayout.LabelField("(s)", _width50);

            // 纳秒转毫秒显式，一般业务精确到毫秒即可
            int millis = (int)(varNanos.longValue / DatetimeUtil.NanosPerMilli);
            millis = Math.Clamp(EditorGUILayout.IntField(millis, _width60), 0, 999);
            varNanos.longValue = millis * DatetimeUtil.NanosPerMilli;
            EditorGUILayout.LabelField("(ms)", _width50);
            EditorGUILayout.EndHorizontal();

            // 预览
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            variable.stringValue = EditorGUILayout.TextField("preview", variable.stringValue);
            if (string.IsNullOrWhiteSpace(variable.stringValue)) {
                if (GUILayout.Button("Refresh", _width60)) {
                    OnClickPreview(variable);
                }
            } else {
                if (GUILayout.Button("Apply", _width60)) {
                    OnClickApply(variable);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private static void OnClickApply(DataVariable variable) {
            if (string.IsNullOrWhiteSpace(variable.stringValue)) {
                return;
            }
            try {
                DateTime dateTime = DatetimeUtil.ParseDateTime(variable.stringValue);
                long seconds = DatetimeUtil.ToEpochSeconds(dateTime);
                variable.values[0].longValue = seconds;
            }
            catch (Exception ex) {
                Debug.LogException(ex);
            }
        }

        private static void OnClickPreview(DataVariable variable) {
            DateTime dateTime = DatetimeUtil.UnixEpoch.AddSeconds(variable.values[0].longValue);
            variable.stringValue = dateTime.ToString("s");
        }
    }

    public class ObjectPtrVariableDrawer : DataVariableDrawer
    {
        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            variable.isExpanded = EditorGUILayout.Foldout(variable.isExpanded, label);
            if (!variable.isExpanded) {
                return;
            }
            DataVariable varAssetPath = variable.values[0];
            DataVariable varLocalName = variable.values[1];
            DataVariable varLocalId = variable.values[2];
            DataVariable varType = variable.values[3];

            EditorGUILayout.BeginVertical();
            float labelWidth = EditorGUIUtility.labelWidth;
            int indentLevel = EditorGUI.indentLevel;
            EditorGUIUtility.labelWidth = 150;
            EditorGUI.indentLevel++;

            string newPath = EditorGUILayout.TextField("AssetPath", varAssetPath.stringValue);
            if (newPath != varAssetPath.stringValue) {
                newPath = newPath.Replace('\\', '/');
                varAssetPath.stringValue = newPath;
            }
            CheckPingObjectEvent(varAssetPath.stringValue, Event.current, GUILayoutUtility.GetLastRect());
            varLocalName.stringValue = EditorGUILayout.TextField("LocalName", varLocalName.stringValue);
            varLocalId.longValue = EditorGUILayout.LongField("LocalId", varLocalId.longValue);
            varType.intValue = EditorGUILayout.IntField("Type", varType.intValue);

            EditorGUI.indentLevel = indentLevel;
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUILayout.EndVertical();
        }
    }

    #endregion

    #region unity-struct

    public class Vector2VariableDrawer : DataVariableDrawer
    {
        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            DataVariable varX = variable.values[0];
            DataVariable varY = variable.values[1];

            bool wideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true; // 强制单行显示
            Vector2 value = new Vector2(varX.floatValue, varY.floatValue);
            value = EditorGUILayout.Vector2Field(label, value);
            EditorGUIUtility.wideMode = wideMode;

            varX.floatValue = value.x;
            varY.floatValue = value.y;
        }
    }

    public class Vector3VariableDrawer : DataVariableDrawer
    {
        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            DataVariable varX = variable.values[0];
            DataVariable varY = variable.values[1];
            DataVariable varZ = variable.values[2];

            bool wideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true; // 强制单行显示
            Vector3 value = new Vector3(varX.floatValue, varY.floatValue, varZ.floatValue);
            value = EditorGUILayout.Vector3Field(label, value);
            EditorGUIUtility.wideMode = wideMode;

            varX.floatValue = value.x;
            varY.floatValue = value.y;
            varZ.floatValue = value.z;
        }
    }

    public class Vector4VariableDrawer : DataVariableDrawer
    {
        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            DataVariable varX = variable.values[0];
            DataVariable varY = variable.values[1];
            DataVariable varZ = variable.values[2];
            DataVariable varW = variable.values[3];

            bool wideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true; // 强制单行显示
            Vector4 value = new(varX.floatValue, varY.floatValue, varZ.floatValue, varW.floatValue);
            value = EditorGUILayout.Vector4Field(label, value);
            EditorGUIUtility.wideMode = wideMode;

            varX.floatValue = value.x;
            varY.floatValue = value.y;
            varZ.floatValue = value.z;
            varW.floatValue = value.w;
        }
    }

    public class Vector2IntVariableDrawer : DataVariableDrawer
    {
        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            DataVariable varX = variable.values[0];
            DataVariable varY = variable.values[1];

            bool wideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true; // 强制单行显示
            Vector2Int value = new Vector2Int(varX.intValue, varY.intValue);
            value = EditorGUILayout.Vector2IntField(label, value);
            EditorGUIUtility.wideMode = wideMode;

            varX.intValue = value.x;
            varY.intValue = value.y;
        }
    }

    public class Vector3IntVariableDrawer : DataVariableDrawer
    {
        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            DataVariable varX = variable.values[0];
            DataVariable varY = variable.values[1];
            DataVariable varZ = variable.values[2];

            bool wideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true; // 强制单行显示
            Vector3Int value = new Vector3Int(varX.intValue, varY.intValue, varZ.intValue);
            value = EditorGUILayout.Vector3IntField(label, value);
            EditorGUIUtility.wideMode = wideMode;

            varX.intValue = value.x;
            varY.intValue = value.y;
            varZ.intValue = value.z;
        }
    }

    public class ColorVariableDrawer : DataVariableDrawer
    {
        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            DataVariable varR = variable.values[0];
            DataVariable varG = variable.values[1];
            DataVariable varB = variable.values[2];
            DataVariable varA = variable.values[3];

            Color value = new Color(varR.floatValue, varG.floatValue, varB.floatValue, varA.floatValue);
            value = EditorGUILayout.ColorField(label, value);

            varR.floatValue = value.r;
            varG.floatValue = value.g;
            varB.floatValue = value.b;
            varA.floatValue = value.a;

            Rect lastRect = GUILayoutUtility.GetLastRect();
            if (IsContextClickEvent(Event.current, lastRect)) {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("White"), false, OnClickWhite, variable);
                menu.AddItem(new GUIContent("Red"), false, OnClickRed, variable);
                menu.AddItem(new GUIContent("Yellow"), false, OnClickYellow, variable);
                menu.AddItem(new GUIContent("Blue"), false, OnClickBlue, variable);
                menu.ShowAsContext();
            }
        }

        private static void OnClickWhite(object obj) {
            DataVariable var = (DataVariable)obj;
            SetColor(var, Color.white);
        }

        private static void OnClickRed(object obj) {
            DataVariable var = (DataVariable)obj;
            SetColor(var, Color.red);
        }

        private static void OnClickYellow(object obj) {
            DataVariable var = (DataVariable)obj;
            SetColor(var, Color.yellow);
        }

        private static void OnClickBlue(object obj) {
            DataVariable var = (DataVariable)obj;
            SetColor(var, Color.blue);
        }

        private static void SetColor(DataVariable variable, Color color) {
            variable.values[0].floatValue = color.r;
            variable.values[1].floatValue = color.g;
            variable.values[2].floatValue = color.b;
            variable.values[3].floatValue = color.a;
        }
    }

    public class Color32VariableDrawer : DataVariableDrawer
    {
        public override void OnGUI(DataEditor editor, DataVariable variable, GUIContent label) {
            DataVariable varR = variable.values[0];
            DataVariable varG = variable.values[1];
            DataVariable varB = variable.values[2];
            DataVariable varA = variable.values[3];

            Color32 value = new Color32(varR.byteValue, varG.byteValue, varB.byteValue, varA.byteValue);
            value = EditorGUILayout.ColorField(label, value);

            varR.byteValue = value.r;
            varG.byteValue = value.g;
            varB.byteValue = value.b;
            varA.byteValue = value.a;
        }
    }

    #endregion
}
}