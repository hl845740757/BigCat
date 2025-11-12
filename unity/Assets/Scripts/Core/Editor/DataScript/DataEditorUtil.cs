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
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Dson;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
///
/// 1.由于Variable和<see cref="SerializedProperty"/>不能很好的对应，因此我们统一不使用BindProperty。
/// 2.由于我们Variable的Inspector视图是动态创建的，因此我们不设计取消监听逻辑以减少复杂度。
/// </summary>
public static class DataEditorUtil
{
    #region util

    /// <summary>
    /// 拷贝到系统buffer
    /// </summary>
    public static void DoCopy(Variable variable, DataEditor editor) {
        if (DSUtil.IsAtomicType(variable.type)) {
            return;
        }
        GUIUtility.systemCopyBuffer = editor.model.DoCopy(variable);
    }

    /// <summary>
    /// 从系统Buffer粘贴
    /// </summary>
    public static void DoPaste(Variable variable, DataEditor editor) {
        string copyBuffer = GUIUtility.systemCopyBuffer;
        if (string.IsNullOrWhiteSpace(copyBuffer)) {
            return;
        }
        // GUIUtility.systemCopyBuffer = "";
        try {
            editor.model.DoPaste(variable, copyBuffer);
        }
        catch (Exception) {
            Debug.Log("invalid copy buffer: " + copyBuffer);
        }
    }

    /// <summary>
    /// 文本是否可执行粘贴
    /// </summary>
    public static bool IsPastable(string text, DsonType expectedType) {
        if (string.IsNullOrWhiteSpace(text)) {
            return false;
        }
        try {
            return Dsons.FromDson(text).DsonType == expectedType;
        }
        catch (Exception) {
            return false;
        }
    }

    internal static int GetFoldoutDepth(VisualElement element) {
        int foldoutDepth = 0;
        for (VisualElement parent = element.parent; parent != null; parent = parent.parent) {
            if (parent is Foldout) {
                foldoutDepth++;
            }
        }
        return foldoutDepth;
    }

    #endregion

    #region set-label

    /// <summary>
    /// 设置字段的Label
    /// </summary>
    /// <param name="element"></param>
    /// <param name="label"></param>
    public static void SetFieldLabel(VisualElement element, string label) {
        if (element is IVarField field) {
            field.label = label;
        }
    }

    public static void SetFieldLabelMargin<T>(BaseField<T> field, VariableCfg variableCfg) {
        FieldStyleCfg styleCfg = variableCfg.styleCfg;
        if (styleCfg != null && styleCfg.labelMargin != null) {
            field.SetLabelMargin(styleCfg.labelMargin.FloatValue);
        }
    }

    public static void SetVectorFieldMargin(VisualElement field, VisualElement labelElement, VariableCfg variableCfg) {
        FieldStyleCfg styleCfg = variableCfg.styleCfg;
        if (styleCfg == null) return;
        if (styleCfg.labelMargin != null) {
            labelElement.style.marginRight = styleCfg.labelMargin.FloatValue;
        }
        if (styleCfg.xLabelMargin != null && (labelElement = GetVectorFieldLabel(field, 0)) != null) {
            labelElement.style.marginRight = styleCfg.xLabelMargin.FloatValue;
        }
        if (styleCfg.yLabelMargin != null && (labelElement = GetVectorFieldLabel(field, 1)) != null) {
            labelElement.style.marginRight = styleCfg.yLabelMargin.FloatValue;
        }
        if (styleCfg.zLabelMargin != null && (labelElement = GetVectorFieldLabel(field, 2)) != null) {
            labelElement.style.marginRight = styleCfg.zLabelMargin.FloatValue;
        }
        if (styleCfg.wLabelMargin != null && (labelElement = GetVectorFieldLabel(field, 3)) != null) {
            labelElement.style.marginRight = styleCfg.wLabelMargin.FloatValue;
        }
    }

    private static Label GetVectorFieldLabel(VisualElement field, int index) {
        VisualElement values = field.childCount == 1 ? field[0] : field[1];
        if (index >= field.childCount) {
            return null;
        }
        VisualElement inputField = values[index];
        if (inputField is FloatField floatField) {
            return floatField.labelElement;
        }
        IntegerField intField = (IntegerField)inputField;
        return intField.labelElement;
    }

    #endregion

    #region create

    /// <summary>
    /// 根据Variable的信息创建对应的字段变量
    /// </summary>
    /// <param name="variable"></param>
    /// <param name="editor"></param>
    /// <returns></returns>
    public static VisualElement CreateField(Variable variable, DataEditor editor) {
        DSNamedType varType = variable.type;
        // 集合类型的DisplayType作用与其元素 - Map暂时不处理映射
        if (DSUtil.IsCollectionType(varType)) {
            return CreateListField(variable, editor);
        }
        if (DSUtil.IsMapType(varType)) {
            return CreateMapField(variable, editor);
        }
        if (DSUtil.IsNullableType(varType)) {
            return CreateNullableField(variable, editor);
        }
        // 如果字段指定了展示类型，则使用字段指定的展示类型
        VariableCfg variableCfg = variable.cfg;
        if (variableCfg.displayType != DisplayType.Default) {
            return CreateField(variable, variableCfg.displayType, editor);
        }
        // 如果类型指定了展示类型，则使用类型自动的展示类型
        VariableCfg typeCfg = editor.model.GetVariableCfg(varType);
        if (typeCfg.displayType != DisplayType.Default) {
            return CreateField(variable, typeCfg.displayType, editor);
        }
        // 根据字段类型自动推测
        switch (varType.SimpleName) {
            case DSKeywords.TYPE_INT32: {
                if (variableCfg.HasMaskNames) return CreateInt32MaskField(variable, editor);
                if (variableCfg.HasPopNames) return CreateInt32PopupField(variable, editor);
                return CreateInt32Field(variable, editor);
            }
            case DSKeywords.TYPE_INT64: { // 其实可以考虑支持一下Int64的Mask
                if (variableCfg.HasMaskNames) return CreateInt32MaskField(variable, editor);
                if (variableCfg.HasPopNames) return CreateInt32PopupField(variable, editor);
                return CreateInt64Field(variable, editor);
            }
            case DSKeywords.TYPE_STRING: {
                if (variableCfg.HasPopNames) return CreateStringPopupField(variable, editor);
                return CreateStringField(variable, editor);
            }
            case DSKeywords.TYPE_BYTES: {
                variableCfg.isDelayed = true;
                variableCfg.isMultiline = true;
                return CreateStringField(variable, editor);
            }
            case DSKeywords.TYPE_FLOAT: return CreateFloatField(variable, editor);
            case DSKeywords.TYPE_DOUBLE: return CreateDoubleField(variable, editor);
            case DSKeywords.TYPE_BOOL: return CreateBoolField(variable, editor);
            case DSKeywords.TYPE_DATETIME: return CreateDateTimeField(variable, editor);
            case DSKeywords.TYPE_TIMESTAMP: return CreateTimestampField(variable, editor);
            case DSKeywords.TYPE_POINTER: return CreateObjectPathField(variable, editor);
        }
        // 枚举
        if (varType.Kind == DSElementKind.Enum) {
            return variableCfg.HasMaskNames
                ? CreateEnumMaskField(variable, editor)
                : CreateEnumField(variable, editor);
        }
        // 通用Object类型
        return CreateObjectField(variable, editor);
    }

    private static VisualElement CreateField(Variable variable, DisplayType displayType, DataEditor editor) {
        return displayType switch
        {
            DisplayType.List => CreateListField(variable, editor),
            DisplayType.AssetPath => CreateAssetPathField(variable, editor),
            DisplayType.ObjectPath => CreateObjectPathField(variable, editor),
            DisplayType.DateTime => CreateDateTimeField(variable, editor),
            DisplayType.Timestamp => CreateTimestampField(variable, editor),
            DisplayType.Vector2 => CreateVector2Field(variable, editor),
            DisplayType.Vector3 => CreateVector3Field(variable, editor),
            DisplayType.Vector4 => CreateVector4Field(variable, editor),
            DisplayType.Vector2Int => CreateVector2IntField(variable, editor),
            DisplayType.Vector3Int => CreateVector3IntField(variable, editor),
            DisplayType.Color => CreateColorField(variable, editor),
            DisplayType.Color32 => CreateColor32Field(variable, editor),
            DisplayType.Euler32 => CreateEuler32Field(variable, editor),
            DisplayType.MinMaxAABB => CreateAABBField(variable, editor),
            _ => throw new ArgumentException("invalid displayType: " + displayType)
        };
    }

    public static void Bind(VisualElement element, Variable variable, DataEditor editor) {
        if (element is IVarField field) {
            field.Bind(editor, variable);
        }
    }

    public static void Refresh(VisualElement element, bool rebuild) {
        if (element is IVarField field) {
            field.Refresh(rebuild);
        }
    }

    internal static bool IsCacheable(VisualElement element) {
        return element is IVarField;
        // return element switch
        // {
        //     VarObjectField or VarListField => false,
        //     IVarField field => true,
        //     _ => false
        // };
    }

    #endregion

    #region atomic

    public static IntegerField CreateInt32Field(Variable variable, DataEditor editor) {
        VarInt32Field field = new VarInt32Field();
        field.Bind(editor, variable);
        return field;
    }

    public static LongField CreateInt64Field(Variable variable, DataEditor editor) {
        VarInt64Field field = new VarInt64Field();
        field.Bind(editor, variable);
        return field;
    }

    public static FloatField CreateFloatField(Variable variable, DataEditor editor) {
        VarFloatField field = new VarFloatField();
        field.Bind(editor, variable);
        return field;
    }

    public static DoubleField CreateDoubleField(Variable variable, DataEditor editor) {
        VarDoubleField field = new VarDoubleField();
        field.Bind(editor, variable);
        return field;
    }

    public static Toggle CreateBoolField(Variable variable, DataEditor editor) {
        VarBoolField field = new VarBoolField();
        field.Bind(editor, variable);
        return field;
    }

    public static TextField CreateStringField(Variable variable, DataEditor editor) {
        VarStringField field = new VarStringField();
        field.Bind(editor, variable);
        return field;
    }

    public static PopupField<int> CreateInt32PopupField(Variable variable, DataEditor editor) {
        VarInt32PopupField field = new VarInt32PopupField();
        field.Bind(editor, variable);
        return field;
    }

    public static PopupField<string> CreateStringPopupField(Variable variable, DataEditor editor) {
        VarStringPopupField field = new VarStringPopupField();
        field.Bind(editor, variable);
        return field;
    }

    public static MaskField CreateInt32MaskField(Variable variable, DataEditor editor) {
        VarInt32MaskField field = new VarInt32MaskField();
        field.Bind(editor, variable);
        return field;
    }

    public static PopupField<int> CreateEnumField(Variable variable, DataEditor editor) {
        return CreateInt32PopupField(variable, editor);
    }

    public static MaskField CreateEnumMaskField(Variable variable, DataEditor editor) {
        return CreateInt32MaskField(variable, editor);
    }

    public static VarAssetPathField CreateAssetPathField(Variable variable, DataEditor editor) {
        VarAssetPathField field = new VarAssetPathField();
        field.Bind(editor, variable);
        return field;
    }

    #endregion

    #region struct

    public static VarVector2Field CreateVector2Field(Variable variable, DataEditor editor) {
        VarVector2Field field = new VarVector2Field();
        field.Bind(editor, variable);
        return field;
    }

    public static VarVector3Field CreateVector3Field(Variable variable, DataEditor editor) {
        VarVector3Field field = new VarVector3Field();
        field.Bind(editor, variable);
        return field;
    }

    public static VarVector4Field CreateVector4Field(Variable variable, DataEditor editor) {
        VarVector4Field field = new VarVector4Field();
        field.Bind(editor, variable);
        return field;
    }

    public static VarVector2IntField CreateVector2IntField(Variable variable, DataEditor editor) {
        VarVector2IntField field = new VarVector2IntField();
        field.Bind(editor, variable);
        return field;
    }

    public static VarVector3IntField CreateVector3IntField(Variable variable, DataEditor editor) {
        VarVector3IntField field = new VarVector3IntField();
        field.Bind(editor, variable);
        return field;
    }

    public static VarColorField CreateColorField(Variable variable, DataEditor editor) {
        VarColorField field = new VarColorField();
        field.Bind(editor, variable);
        return field;
    }

    public static VarColorField CreateColor32Field(Variable variable, DataEditor editor) {
        VarColorField field = new VarColorField();
        field.Bind(editor, variable);
        return field;
    }

    public static VarDateTimeField CreateDateTimeField(Variable variable, DataEditor editor) {
        VarDateTimeField field = new VarDateTimeField();
        field.Bind(editor, variable);
        return field;
    }

    public static VarTimestampField CreateTimestampField(Variable variable, DataEditor editor) {
        VarTimestampField field = new VarTimestampField();
        field.Bind(editor, variable);
        return field;
    }

    public static VarObjectPathField CreateObjectPathField(Variable variable, DataEditor editor) {
        VarObjectPathField field = new VarObjectPathField();
        field.Bind(editor, variable);
        return field;
    }

    public static VarAABBField CreateAABBField(Variable variable, DataEditor editor) {
        VarAABBField field = new VarAABBField();
        field.Bind(editor, variable);
        return field;
    }

    public static VarEuler32Field CreateEuler32Field(Variable variable, DataEditor editor) {
        VarEuler32Field field = new VarEuler32Field();
        field.Bind(editor, variable);
        return field;
    }

    #endregion

    #region list/map/object

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public static VarListField CreateListField(Variable variable, DataEditor editor) {
        VarListField field = new VarListField();
        field.Bind(editor, variable);
        return field;
    }

    public static VarListField CreateMapField(Variable variable, DataEditor editor) {
        VarListField field = new VarListField();
        field.Bind(editor, variable);
        return field;
    }

    public static VarNullableField CreateNullableField(Variable variable, DataEditor editor) {
        VarNullableField field = new VarNullableField();
        field.Bind(editor, variable);
        return field;
    }

    public static VarObjectField CreateObjectField(Variable variable, DataEditor editor) {
        VarObjectField field = new VarObjectField();
        field.Bind(editor, variable);
        return field;
    }

    #endregion
}
}