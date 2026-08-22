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
using Wjybxx.BigCat.Core;
using Wjybxx.BigCatTool.Core;
using Wjybxx.BigCatTool.DataScript;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson;
using Wjybxx.Dson.Codec;

namespace Wjybxx.BigCat.Editor.DataScript
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
    /// 注：List和Map字段的Pop信息表示Value的信息。
    /// </summary>
    public DsonNumber min, max;
    /// <summary>
    /// 是否延迟响应输入
    /// 注：List和Map字段的Pop信息表示Value的信息。
    /// </summary>
    public bool isDelayed;
    /// <summary>
    /// 是否是多行文本
    /// 注：List和Map字段的Pop信息表示Value的信息。
    /// </summary>
    public bool isMultiline;
    /// <summary>
    /// 是否是整数类型AABB
    /// 注：List和Map字段的Pop信息表示Value的信息。
    /// </summary>
    public bool isInteger;
    /// <summary>
    /// 资产路径是否是文件夹路径
    /// </summary>
    public bool isFolder;
    /// <summary>
    /// List字段是否是表单
    /// </summary>
    public bool isSheet;
    /// <summary>
    /// 资产路径类型
    /// </summary>
    public ObjectPathType pathType;

    /// <summary>
    /// Pop字段的展示名
    ///
    /// 注：List和Map字段的Pop信息表示Value的信息；Enum类型会提前缓存该值。
    /// </summary>
    public List<string> popNames;
    /// <summary>
    /// int值和枚举值的可选值
    ///
    /// 注：List和Map字段的Pop信息表示Value的信息；Enum类型会提前缓存该值。
    /// </summary>
    public List<int> intPopValues;
    /// <summary>
    /// 字符串的可选值，用于pop类型字段
    ///
    /// 注：List和Map字段的Pop信息表示Value的信息。
    /// </summary>
    public List<string> stringPopValues;
    /// <summary>
    /// 字符串候选值，用于普通string字段
    ///
    /// 注：List和Map字段的Pop信息表示Value的信息。
    /// </summary>
    public List<string> candidatesValues;
    /// <summary>
    /// Mask字段的展示名
    /// 
    /// 注：List和Map字段的Pop信息表示Value的信息；Enum类型会提前缓存该值。
    /// </summary>
    public List<string> maskNames;
    /// <summary>
    /// pop/mask字段关联的索引枚举(symbol)
    /// </summary>
    public string targetEnum;
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
    /// 端口名
    /// </summary>
    public List<KeyValuePair<string, string>> portNameRemap;
    /// <summary>
    /// 字段端口配置
    /// </summary>
    public FieldPortCfg portCfg;

    /// <summary>
    /// 最小宽度 - 表单模式下生效
    /// </summary>
    public DsonNumber minWidth;
    /// <summary>
    /// 最大宽度 - 表单模式下生效
    /// </summary>
    public DsonNumber maxWidth;
    /// <summary>
    /// 最大高度
    /// </summary>
    public DsonNumber maxHeight;
    /// <summary>
    /// 标签边距 - 表单模式下生效
    /// </summary>
    public DsonNumber labelMargin;
    /// <summary>
    /// Vector等原子结构的标签编辑，也适用于Pair
    /// </summary>
    public List<DsonNumber> labelMargins;

    /// <summary>
    /// 投影Dson类型
    /// 
    /// 注：用于将自定义结构导出为Dson结构。
    /// 注：List和Map字段的Pop信息表示Value的信息。
    /// </summary>
    public DsonType dsonType = DsonType.EndOfObject;
    /// <summary>
    /// 数据节点的特征值，用于搜索栏字段
    /// </summary>
    public Features nodeFeatures = Features.EnablePort;
    /// <summary>
    /// 序列化特征值（缓存，以避免频繁解析）
    /// </summary>
    public SerializeFeatures encodeFeatures;
    /// <summary>
    /// List/Map/Nullable元素的展示配置
    /// </summary>
    public VariableCfg elementCfg;

    // 缓存
    private Func<int, string> _intPopNameFunc;
    private Func<string, string> _stringPopNameFunc;

    public Func<int, string> intPopNameFunc => _intPopNameFunc ??= value => {
        int index = intPopValues.IndexOf(value);
        return index < 0 ? value.ToString() : popNames[index]; // 可能是失效值
    };
    public Func<string, string> stringPopNameFunc => _stringPopNameFunc ??= value => {
        int index = stringPopValues.IndexOf(value);
        return index < 0 ? value : popNames[index]; // 可能是失效值
    };

    public bool ContainsTypeSymbol(string typeSymbol) {
        return supportedTypes != null && supportedTypes.Contains(typeSymbol);
    }

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
        cfg.encodeFeatures = DSUtil.GetEncodeFeatures(DSUtil.GetOptions(element));
        //
        Annotation annotation = element.GetAnnotation(DSAnnotations.EDITOR);
        if (annotation != null) {
            ParseEditorOptions(annotation.AsObject(), cfg, element);
        }
        annotation = element.GetAnnotation(DSAnnotations.PORT_FIELD);
        if (annotation != null) {
            ParsePortInfo(annotation.AsObject(), cfg);
        }
        annotation = element.GetAnnotation(DSAnnotations.PORT_NAME_REMAP);
        if (annotation != null) {
            ParsePortRemap(annotation.AsObject(), cfg);
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
        // 多态字段
        annotations = element.GetAnnotations(DSAnnotations.PLOY_FIELD);
        if (annotations.Count > 0) {
            ParsePolyInfo(annotations, cfg);
        }

        // Mask字段 - 自动合并多注解的值
        annotations = element.GetAnnotations(DSAnnotations.MASK_FIELD);
        if (annotations.Count > 0) {
            ParseMaskInfo(annotations, cfg);
        }
        // 候选值信息 - 自动合并多注解的值
        annotations = element.GetAnnotations(DSAnnotations.CANDIDATES);
        if (annotations.Count > 0) {
            ParseCandidateInfo(annotations, cfg);
        }

        // 如果是枚举类型，提前缓存PopNames
        if (element.Kind == DSElementKind.Enum) {
            ParseEnumPops(cfg, (DSNamedType)element);
        } else if (element is DSField field) {
            // 默认tooltip
            if (cfg.tooltip == null && field.Comments.TryPeekLast(out string comment)
                                    && !Annotation.IsAnnotationComment(comment)) {
                int idx = ObjectUtil.IndexOfNonWhitespace(comment, 2);
                cfg.tooltip = idx > 0 ? comment.Substring(idx) : null;
            }
            cfg.tooltip ??= field.TypeSymbol;

            // 拷贝List的配置到元素
            if ((DSUtil.IsCollectionOrMapType(field.Type)
                 || DSUtil.IsNullableType(field.Type)
                 || DSUtil.IsPairType(field.Type))) {
                VariableCfg elementCfg = new VariableCfg();
                CopyListCfg(elementCfg, cfg);
                cfg.elementCfg = elementCfg;
            }
        }
        return cfg;
    }

    private static void CopyListCfg(VariableCfg varCfg, VariableCfg listCfg) {
        varCfg.displayType = listCfg.displayType;
        varCfg.min = listCfg.min;
        varCfg.max = listCfg.max;
        varCfg.isDelayed = listCfg.isDelayed;
        varCfg.isMultiline = listCfg.isMultiline;
        varCfg.isInteger = listCfg.isInteger;
        varCfg.isFolder = listCfg.isFolder;
        varCfg.pathType = listCfg.pathType;
        varCfg.labelMargins = listCfg.labelMargins;

        varCfg.popNames = listCfg.popNames;
        varCfg.intPopValues = listCfg.intPopValues;
        varCfg.stringPopValues = listCfg.stringPopValues;
        varCfg.candidatesValues = listCfg.candidatesValues;
        varCfg.maskNames = listCfg.maskNames;
        varCfg.targetEnum = listCfg.targetEnum;
        varCfg.supportedTypes = listCfg.supportedTypes;
        //
        varCfg.dsonType = listCfg.dsonType;
        varCfg.encodeFeatures = listCfg.encodeFeatures.GetElementFeatures();
    }

    private static void ParseEnumPops(VariableCfg cfg, DSNamedType element) {
        List<DSEnumValue> enumValues = element.GetEnumValues();
        cfg.popNames = new List<string>(enumValues.Count);
        cfg.intPopValues = new List<int>(enumValues.Count);
        for (int index = 0; index < enumValues.Count; index++) {
            DSEnumValue enumValue = enumValues[index];
            cfg.intPopValues.Add(enumValue.Number);
            cfg.popNames.Add(GetEnumDisplayName(enumValue));
        }
        // 解析Mask信息(额外缓存)
        if (DSUtil.IsFlagEnum(element)) {
            int maxIndex = -1;
            string[] maskNames = new string[32];
            foreach (DSEnumValue enumValue in element.GetEnumValues()) {
                if (!MathCommon.IsPowerOfTwo(enumValue.Number)) continue;
                int bitIndex = MathCommon.NumberOfTrailingZeros(enumValue.Number);
                maskNames[bitIndex] = GetEnumDisplayName(enumValue);
                maxIndex = Math.Max(maxIndex, bitIndex);
            }
            cfg.maskNames = new List<string>(maxIndex + 1);
            for (int index = 0; index <= maxIndex; index++) {
                cfg.maskNames.Add(maskNames[index] ?? index.ToString());
            }
        } else if (DSUtil.IsIndexesEnum(element)) {
            int maxIndex = -1;
            Dictionary<int, string> maskNames = new(32); // 可能超过32位
            foreach (DSEnumValue enumValue in element.GetEnumValues()) {
                int bitIndex = enumValue.Number;
                maskNames[bitIndex] = GetEnumDisplayName(enumValue);
                maxIndex = Math.Max(maxIndex, bitIndex);
            }
            cfg.maskNames = new List<string>(maxIndex + 1);
            for (int index = 0; index <= maxIndex; index++) {
                if (!maskNames.TryGetValue(index, out string maskName)) {
                    maskName = ""; // 空字符串显示为横线
                }
                cfg.maskNames.Add(maskName);
            }
        }
    }

    private static string GetEnumDisplayName(DSElement element) {
        Annotation annotation = element.GetAnnotation(DSAnnotations.EDITOR);
        string displayName = element.Name;
        if (annotation != null && annotation.AsObject().TryGetValue(DSAnnotations.KEY_DISPLAY_NAME, out DsonValue dsonValue)) {
            displayName = dsonValue.AsString();
        }
        Annotation region = GetBelongRegion(element);
        if (region != null) {
            DsonValue regionName = region.DsonValue.AsArray()[0];
            return regionName.AsString() + "/" + displayName;
        }
        return displayName;
    }

    private static Annotation GetBelongRegion(DSElement element) {
        int ln = element.OriginDefine.StartLine;
        Annotation prev = null;
        foreach (Annotation annotation in element.GetEnclosingFile().Annotations) {
            if (annotation.ln > ln) return prev;
            if (annotation.type == DSAnnotations.ENDREGION) {
                prev = null;
                continue;
            }
            if (annotation.type == DSAnnotations.REGION) {
                prev = annotation;
            }
        }
        return prev;
    }

    private static void ParsePopInfo(List<Annotation> annotations, VariableCfg cfg, DSField element) {
        cfg.popNames = new List<string>(annotations.Count);
        if (element.Type.Name == DSKeywords.TYPE_INT32) {
            cfg.intPopValues = new List<int>(annotations.Count);
        } else {
            cfg.stringPopValues = new List<string>(annotations.Count);
        }
        for (int index = 0; index < annotations.Count; index++) {
            Annotation annotation = annotations[index];
            DsonHeader<string> header = Dsons.GetHeader(annotation.DsonValue)!;
            if (header.TryGetValue(DsonHeader.Names_ClassName, out DsonValue clsName)) {
                cfg.targetEnum = clsName.AsString();
                break;
            }
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
            if (ctrlName == element.Name) {
                throw new InvalidOperationException("ctrl == self");
            }
            FieldBranchCfg branchCfg = new FieldBranchCfg();
            branchCfg.ctrl = ctrlName;
            branchCfg.ctrlIndex = allFields.FindIndex(e => e.Name == ctrlName);
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

    private static void ParseCandidateInfo(List<Annotation> annotations, VariableCfg cfg) {
        cfg.candidatesValues = new List<string>();
        foreach (Annotation annotation in annotations) {
            DsonArray<string> dsonArray = annotation.AsArray();
            foreach (DsonValue dsonValue in dsonArray) {
                cfg.candidatesValues.Add(dsonValue.AsString());
            }
        }
        cfg.candidatesValues.TrimExcess();
    }

    private static void ParseMaskInfo(List<Annotation> annotations, VariableCfg cfg) {
        cfg.maskNames = new List<string>();
        foreach (Annotation annotation in annotations) {
            DsonHeader<string> header = Dsons.GetHeader(annotation.DsonValue)!;
            if (header.TryGetValue(DsonHeader.Names_ClassName, out DsonValue clsName)) {
                cfg.targetEnum = clsName.AsString();
                break;
            }
            DsonArray<string> dsonArray = annotation.AsArray();
            foreach (DsonValue dsonValue in dsonArray) {
                cfg.maskNames.Add(dsonValue.AsString());
            }
        }
        cfg.maskNames.TrimExcess();
    }

    private static void ParsePolyInfo(List<Annotation> annotations, VariableCfg cfg) {
        cfg.supportedTypes = new List<string>();
        foreach (Annotation annotation in annotations) {
            DsonArray<string> dsonArray = annotation.AsArray();
            foreach (DsonValue dsonValue in dsonArray) {
                cfg.supportedTypes.Add(dsonValue.AsString());
            }
        }
        cfg.supportedTypes.TrimExcess();
    }

    private static void ParsePortRemap(DsonObject<string> dsonObject, VariableCfg cfg) {
        cfg.portNameRemap = new List<KeyValuePair<string, string>>(dsonObject.Count);
        foreach (var pair in dsonObject) {
            cfg.portNameRemap.Add(new(pair.Key, pair.Value.AsString()));
        }
    }

    private static void ParsePortInfo(DsonObject<string> dsonObject, VariableCfg cfg) {
        FieldPortCfg portCfg = new FieldPortCfg();
        DsonValue dsonValue;
        if (dsonObject.TryGetValue(DSAnnotations.KEY_SIDE, out dsonValue)) {
            portCfg.side = Enum.Parse<Side>(dsonValue.AsString(), true);
            Debug.Assert(portCfg.side != Side.Top, "side == top");
        }
        portCfg.distinct = Annotation.GetBool(dsonObject, DSAnnotations.KEY_DISTINCT);
        portCfg.expanded = Annotation.GetBool(dsonObject, DSAnnotations.KEY_EXPANDED);
        cfg.portCfg = portCfg;
    }

    private static void ParseEditorOptions(DsonObject<string> dsonObject, VariableCfg cfg, DSElement element) {
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
        if (dsonObject.TryGetValue(DSAnnotations.KEY_NODE_FEATURES, out dsonValue)) {
            cfg.nodeFeatures = DSUtil.ParseFlags<Features>(dsonValue);
        }
        if (dsonObject.TryGetValue(DSAnnotations.KEY_PATH_TYPE, out dsonValue)) {
            cfg.pathType = Enum.Parse<ObjectPathType>(dsonValue.AsString(), true);
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
        cfg.isFolder = Annotation.GetBool(dsonObject, DSAnnotations.KEY_IS_FOLDER);
        cfg.isSheet = Annotation.GetBool(dsonObject, DSAnnotations.KEY_IS_SHEET);
        //
        if (dsonObject.TryGetValue(DSAnnotations.KEY_MIN_WIDTH, out dsonValue)) {
            cfg.minWidth = dsonValue.AsNumber();
        }
        if (dsonObject.TryGetValue(DSAnnotations.KEY_MAX_WIDTH, out dsonValue)) {
            cfg.maxWidth = dsonValue.AsNumber();
        }
        if (dsonObject.TryGetValue(DSAnnotations.KEY_MAX_HEIGHT, out dsonValue)) {
            cfg.maxHeight = dsonValue.AsNumber();
        }
        // 边距
        if (dsonObject.TryGetValue(DSAnnotations.KEY_LABEL_MARGIN, out dsonValue)) {
            cfg.labelMargin = dsonValue.AsNumber();
        }
        if (dsonObject.TryGetValue(DSAnnotations.KEY_LABEL_MARGINS, out dsonValue)) {
            cfg.labelMargins = new List<DsonNumber>();
            foreach (DsonValue value in dsonValue.AsArray()) {
                DsonNumber margin = value.IsNumber ? value.AsNumber() : null; // 可能null
                cfg.labelMargins.Add(margin);
            }
        }
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
/// </summary>
public sealed class FieldPortCfg
{
    public Side side = Side.Right; // 端口的显示位置
    public bool distinct; // 是否去重
    public bool expanded; // 是否默认展开 - 同侧只能出现一个默认展开端口
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
}