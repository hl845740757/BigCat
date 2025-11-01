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
using UnityEngine;
using Wjybxx.BigCatTool.Core;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
/// 变量编辑器相关配置
///
/// 注：由<see cref="DSElement"/>上的注解信息解析获得。
/// </summary>
public sealed class VariableCfg
{
    /// <summary>
    /// 展示类型
    ///
    /// 1.List和Map字段的DisplayType表示为Value的展示类型。
    /// 2.字段没有指定展示类型的情况下，需要在运行时查找字段类型的配置，无法提前处理 - 原始的字段定义可能是泛型。
    /// </summary>
    public DisplayType displayType;
    /// <summary>
    /// 展示名
    /// </summary>
    public string displayName;
    /// <summary>
    /// Tip
    /// </summary>
    public string tooltip;

    /// <summary>
    /// 是否初始化为null
    /// (作用于字段自身)
    /// </summary>
    public bool initNull;
    /// <summary>
    /// 数字字段的最大最小值
    ///
    /// 注：List和Map字段的Pop信息表示Value的信息。
    /// </summary>
    public DsonNumber min, max;
    /// <summary>
    /// 是否延迟响应输入
    ///
    /// 注：List和Map字段的Pop信息表示Value的信息。
    /// </summary>
    public bool isDelayed;
    /// <summary>
    /// 是否是多行文本
    ///
    /// 注：List和Map字段的Pop信息表示Value的信息。
    /// </summary>
    public bool isMultiline;
    /// <summary>
    /// 是否是整数类型AABB
    ///
    /// 注：List和Map字段的Pop信息表示Value的信息。
    /// </summary>
    public bool isInteger;

    /// <summary>
    /// Pop字段的展示名
    ///
    /// 注：List和Map字段的Pop信息表示Value的信息。
    /// </summary>
    public List<string> popNames;
    /// <summary>
    /// int值和枚举值的可选值
    ///
    /// 注：List和Map字段的Pop信息表示Value的信息。
    /// </summary>
    public List<int> intPopValues;
    /// <summary>
    /// 字符串的可选值
    ///
    /// 注：List和Map字段的Pop信息表示Value的信息。
    /// </summary>
    public List<string> stringPopValues;
    /// <summary>
    /// Mask字段时的展示名
    ///
    /// 1.如果是枚举，根据枚举类信息填充
    /// 2.如果是int，则根据用户的配置填充
    ///
    /// 注：List和Map字段的Pop信息表示Value的信息。
    /// </summary>
    public List<string> maskNames;
    /// <summary>
    /// 标签类字段配置
    ///
    /// 注：理论上不同的分支下，所有配置都可能需要独立；暂不处理，否则配置的复杂度太高；真要处理的可以通过多ctrl实现。
    /// </summary>
    public List<FieldBranchCfg> branchCfgs;
    /// <summary>
    /// 多态字段的可用类型
    ///
    /// 注：如果是List或Map字段，表示Value的多态类型。
    /// </summary>
    public List<string> supportedTypes;
    /// <summary>
    /// 类型关联的可用实例，可用于初始化类型
    /// </summary>
    public List<DSInst> supportedInsts;

    /// <summary>
    /// 字段端口配置
    /// </summary>
    public FieldPortCfg portCfg;
    /// <summary>
    /// 字段Style设置
    /// </summary>
    public FieldStyleCfg styleCfg;

    /// <summary>
    /// 投影Dson类型
    /// 
    /// 注：用于将自定义结构导出为Dson结构。
    /// 注：List和Map字段的Pop信息表示Value的信息。
    /// </summary>
    public DsonType dsonType = DsonType.EndOfObject;
    /// <summary>
    /// 类型的菜单路径
    /// </summary>
    public string menuPath;
    /// <summary>
    /// List/Map/Nullable元素的展示配置
    /// </summary>
    public VariableCfg elementCfg;

    // 缓存
    private Func<int, string> _intPopNameFunc;
    private Func<string, string> _stringPopNameFunc;

    public Func<int, string> intPopNameFunc => _intPopNameFunc ??= value => {
        int index = intPopValues.IndexOf(value);
        return index < 0 ? index.ToString() : popNames[index]; // 可能是失效值
    };
    public Func<string, string> stringPopNameFunc => _stringPopNameFunc ??= value => {
        int index = stringPopValues.IndexOf(value);
        return index < 0 ? value : popNames[index]; // 可能是失效值
    };

    public bool HasPopNames => popNames != null;
    public bool HasMaskNames => maskNames != null;
    public bool HasBranchCfg => branchCfgs != null;
    public bool HasSupportedTypes => supportedTypes != null;
    public bool HasSupportedInsts => supportedInsts != null;
    public bool HasPortCfg => portCfg != null;

    /// <summary>
    /// 字段和类型都需要调用该接口
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static VariableCfg Parse(DSElement element) {
        if (!element.IsOriginDefine) {
            throw new Exception("element must be OriginDefine");
        }
        VariableCfg cfg = new VariableCfg();
        Annotation annotation = element.GetAnnotation(DSAnnotations.EDITOR);
        if (annotation != null) {
            ParseBaseOptions(annotation.AsObject(), cfg);
        }
        annotation = element.GetAnnotation(DSAnnotations.EDITOR_STYLE);
        if (annotation != null) {
            ParseStyleOptions(annotation.AsObject(), cfg);
        }
        annotation = element.GetAnnotation(DSAnnotations.PORT_FIELD);
        if (annotation != null) {
            ParsePortInfo(annotation.AsObject(), cfg);
        }
        // Pop和Branch都是多注解 - 只能用于字段
        List<Annotation> annotations = element.GetAnnotations(DSAnnotations.POP_FIELD);
        if (annotations.Count > 0) {
            ParsePopInfo(annotations, cfg, (DSField)element);
        }
        annotations = element.GetAnnotations(DSAnnotations.BRANCH_FIELD);
        if (annotations.Count > 0) {
            ParseBranchInfo(annotations, cfg, (DSField)element);
        }
        // Mask字段
        annotation = element.GetAnnotation(DSAnnotations.MASK_FIELD);
        if (annotation != null) {
            ParseMaskInfo(annotation.AsArray(), cfg);
        }
        // 多态字段
        annotation = element.GetAnnotation(DSAnnotations.PLOY_FIELD);
        if (annotation != null) {
            ParsePolyInfo(annotation.AsArray(), cfg);
        }
        // 如果是枚举类型，提前缓存PopNames
        if (element.Kind == DSElementKind.Enum) {
            ParseEnumPops(cfg, (DSNamedType)element);
        }
        // 拷贝List的配置到元素
        if (element is DSField field
            && (DSUtil.IsCollectionOrMapType(field.Type) || DSUtil.IsNullableType(field.Type))) {
            VariableCfg elementCfg = new VariableCfg();
            CopyListCfg(elementCfg, cfg);
            cfg.elementCfg = elementCfg;
        }
        // 暂不缓存Codec中的编码样式，意义不大 
        return cfg;
    }

    private static void CopyListCfg(VariableCfg varCfg, VariableCfg listCfg) {
        varCfg.displayType = listCfg.displayType;
        varCfg.min = listCfg.min;
        varCfg.max = listCfg.max;
        varCfg.isDelayed = listCfg.isDelayed;
        varCfg.isMultiline = listCfg.isMultiline;
        varCfg.isInteger = listCfg.isInteger;
        varCfg.dsonType = listCfg.dsonType;

        varCfg.popNames = listCfg.popNames;
        varCfg.intPopValues = listCfg.intPopValues;
        varCfg.stringPopValues = listCfg.stringPopValues;
        varCfg.maskNames = listCfg.maskNames;
        varCfg.supportedTypes = listCfg.supportedTypes;
    }

    private static void ParseEnumPops(VariableCfg cfg, DSNamedType element) {
        List<DSEnumValue> enumValues = element.GetEnumValues();
        cfg.popNames = new List<string>(enumValues.Count);
        cfg.intPopValues = new List<int>(enumValues.Count);
        int maxNumber = 0;
        for (int index = 0; index < enumValues.Count; index++) {
            DSEnumValue enumValue = enumValues[index];
            cfg.intPopValues.Add(enumValue.Number);
            //
            string displayName = enumValue.SimpleName;
            Annotation annotation = enumValue.GetAnnotation(DSAnnotations.EDITOR);
            if (annotation != null) {
                displayName = Annotation.GetString(annotation.AsObject(), DSAnnotations.KEY_DISPLAY_NAME, displayName);
            }
            cfg.popNames.Add(displayName);
            maxNumber = Math.Max(maxNumber, enumValue.Number);
        }
        // 则额外解析Mask信息 - 需要去掉末尾的空白
        int maxLen = Math.Min(32, maxNumber + 1);
        cfg.maskNames = new List<string>(maxLen);
        for (int index = 0; index < maxLen; index++) {
            DSEnumValue enumValue = element.GetEnumValue(index);
            if (enumValue == null) {
                continue;
            }
            cfg.maskNames.Add(enumValue.SimpleName);
        }
        cfg.maskNames.TrimExcess();
    }

    private static void ParsePopInfo(List<Annotation> annotations, VariableCfg cfg, DSField element) {
        cfg.popNames = new List<string>(annotations.Count);
        if (element.Type.SimpleName == DSKeywords.TYPE_INT32) {
            cfg.intPopValues = new List<int>(annotations.Count);
        } else {
            cfg.stringPopValues = new List<string>(annotations.Count);
        }
        for (int index = 0; index < annotations.Count; index++) {
            Annotation annotation = annotations[index];
            DsonObject<string> dsonObject = annotation.AsObject();
            DsonValue dsonValue = dsonObject[DSAnnotations.KEY_VALUE];
            if (cfg.intPopValues != null) {
                // int32类型必须配置displayName
                string displayName = dsonObject[DSAnnotations.KEY_DISPLAY_NAME].AsString();
                cfg.popNames.Add(displayName);
                cfg.intPopValues.Add(dsonValue.AsNumber().IntValue);
            } else {
                // 如果缺少双引号，字符串可能被误解析为数字；我们这里不进行兼容处理：特殊字符串加必须双引号，是规范
                string displayName = Annotation.GetString(dsonObject, DSAnnotations.KEY_DISPLAY_NAME, dsonValue.AsString());
                cfg.popNames.Add(displayName);
                cfg.stringPopValues.Add(dsonValue.AsString());
            }
        }
    }

    private static void ParseBranchInfo(List<Annotation> annotations, VariableCfg cfg, DSField element) {
        cfg.branchCfgs = new List<FieldBranchCfg>(annotations.Count);
        List<DSField> allFields = element.EnclosingElement.GetFields();
        for (int index = 0; index < annotations.Count; index++) {
            Annotation annotation = annotations[index];
            DsonObject<string> dsonObject = annotation.AsObject();
            string ctrlName = dsonObject[DSAnnotations.KEY_CTRL].AsString();
            if (ctrlName == element.SimpleName) {
                throw new InvalidOperationException("ctrl == element.SimpleName");
            }
            FieldBranchCfg branchCfg = new FieldBranchCfg();
            branchCfg.ctrl = ctrlName;
            branchCfg.ctrlIndex = CollectionUtil.IndexOfCustom(allFields, e => e.SimpleName == ctrlName);
            // 
            DsonValue dsonValue = dsonObject[DSAnnotations.KEY_VALUE];
            if (dsonValue.IsNumber) {
                branchCfg.intValue = dsonValue.AsNumber().IntValue;
            } else {
                branchCfg.value = dsonValue.AsString();
            }
            //
            branchCfg.displayName = dsonObject[DSAnnotations.KEY_DISPLAY_NAME].AsString();
            if (dsonObject.TryGetValue(DSAnnotations.KEY_TOOLTIP, out dsonValue)) {
                branchCfg.tooltip = dsonValue.AsString();
            }
            cfg.branchCfgs.Add(branchCfg);
        }
    }

    private static void ParseMaskInfo(DsonArray<string> dsonArray, VariableCfg cfg) {
        cfg.maskNames = new List<string>(dsonArray.Count);
        for (int index = 0; index < dsonArray.Count; index++) {
            DsonValue dsonValue = dsonArray[index];
            cfg.maskNames.Add(dsonValue.AsString());
        }
    }

    private static void ParsePolyInfo(DsonArray<string> dsonArray, VariableCfg cfg) {
        cfg.supportedTypes = new List<string>(dsonArray.Count);
        for (int index = 0; index < dsonArray.Count; index++) {
            DsonValue dsonValue = dsonArray[index];
            cfg.supportedTypes.Add(dsonValue.AsString());
        }
    }

    private static void ParsePortInfo(DsonObject<string> dsonObject, VariableCfg cfg) {
        FieldPortCfg portCfg = new FieldPortCfg();
        DsonValue dsonValue;
        if (dsonObject.TryGetValue(DSAnnotations.KEY_SIDE, out dsonValue)) {
            portCfg.side = Enum.Parse<Side>(dsonValue.AsString(), true);
            Debug.Assert(portCfg.side != Side.Top, "side == top");
        }
        cfg.portCfg = portCfg;
    }

    private static void ParseStyleOptions(DsonObject<string> dsonObject, VariableCfg cfg) {
        FieldStyleCfg styleCfg = new FieldStyleCfg();
        DsonValue dsonValue;
        if (dsonObject.TryGetValue(DSAnnotations.KEY_MAX_WIDTH, out dsonValue)) styleCfg.maxWidth = dsonValue.AsNumber();
        if (dsonObject.TryGetValue(DSAnnotations.KEY_MAX_HEIGHT, out dsonValue)) styleCfg.maxHeight = dsonValue.AsNumber();
        //
        if (dsonObject.TryGetValue(DSAnnotations.KEY_LABEL_MARGIN, out dsonValue)) {
            styleCfg.labelMargin = dsonValue.AsNumber();
        }
        if (dsonObject.TryGetValue(DSAnnotations.KEY_X_LABEL_MARGIN, out dsonValue)) {
            styleCfg.xLabelMargin = dsonValue.AsNumber();
        }
        if (dsonObject.TryGetValue(DSAnnotations.KEY_Y_LABEL_MARGIN, out dsonValue)) {
            styleCfg.yLabelMargin = dsonValue.AsNumber();
        }
        if (dsonObject.TryGetValue(DSAnnotations.KEY_Z_LABEL_MARGIN, out dsonValue)) {
            styleCfg.zLabelMargin = dsonValue.AsNumber();
        }
        if (dsonObject.TryGetValue(DSAnnotations.KEY_W_LABEL_MARGIN, out dsonValue)) {
            styleCfg.wLabelMargin = dsonValue.AsNumber();
        }
        // TODO
        cfg.styleCfg = styleCfg;
    }

    private static void ParseBaseOptions(DsonObject<string> dsonObject, VariableCfg cfg) {
        DsonValue dsonValue;
        if (dsonObject.TryGetValue(DSAnnotations.KEY_DISPLAY_TYPE, out dsonValue)) {
            cfg.displayType = Enum.Parse<DisplayType>(dsonValue.AsString(), true);
        }
        if (dsonObject.TryGetValue(DSAnnotations.KEY_DISPLAY_NAME, out dsonValue)) {
            cfg.displayName = dsonValue.AsString();
        }
        if (dsonObject.TryGetValue(DSAnnotations.KEY_TOOLTIP, out dsonValue)) {
            cfg.tooltip = dsonValue.AsString();
        }
        //
        if (dsonObject.TryGetValue(DSAnnotations.KEY_DSON_TYPE, out dsonValue)) {
            cfg.dsonType = Enum.Parse<DsonType>(dsonValue.AsString(), true);
        }
        if (dsonObject.TryGetValue(DSAnnotations.KEY_MENU_PATH, out dsonValue)) {
            cfg.menuPath = dsonValue.AsString();
        }
        //
        if (dsonObject.TryGetValue(DSAnnotations.KEY_MIN, out dsonValue)) {
            cfg.min = dsonValue.AsNumber();
        }
        if (dsonObject.TryGetValue(DSAnnotations.KEY_MAX, out dsonValue)) {
            cfg.max = dsonValue.AsNumber();
        }
        cfg.initNull = Annotation.GetBool(dsonObject, DSAnnotations.KEY_INIT_NULL);
        cfg.isDelayed = Annotation.GetBool(dsonObject, DSAnnotations.KEY_IS_DELAYED);
        cfg.isMultiline = Annotation.GetBool(dsonObject, DSAnnotations.KEY_IS_MULTILINE);
        cfg.isInteger = Annotation.GetBool(dsonObject, DSAnnotations.KEY_IS_INTEGER);
    }
}

/// <summary>
/// 分支字段配置
/// </summary>
public sealed class FieldBranchCfg
{
    public string ctrl; // 控制字段的名字
    public int ctrlIndex; // 控制字段的索引 - 避免每帧搜索
    public string value; // 控制字段的值 - 数字或字符串
    public int intValue; // 控制字段的值 - 避免每帧解析
    public string displayName; // 展示别名
    public string tooltip; // tip
}

/// <summary>
/// Port端口设置
///
/// 注意：字段路径不能经过List/Map字段，即不支持动态路径绑定。
/// </summary>
public sealed class FieldPortCfg
{
    public Side side = Side.Right; // 端口的显示位置
}

/// <summary>
/// Port的显示位置
/// </summary>
public enum Side
{
    Left,
    Right,
    Bottom,
    Top // 不可手动指定为Top区，Top区固定为Parent连接点，即Input区域。
}

/// <summary>
/// 额外的Style配置
/// </summary>
public sealed class FieldStyleCfg
{
    /// <summary>
    /// 视图的最大宽高
    /// </summary>
    public DsonNumber maxWidth;
    public DsonNumber maxHeight;

    public DsonNumber labelMargin;
    public DsonNumber xLabelMargin;
    public DsonNumber yLabelMargin;
    public DsonNumber zLabelMargin;
    public DsonNumber wLabelMargin;
}
}