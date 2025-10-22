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
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCat.CoreEditor
{
/// <summary>
/// <see cref="DSElement"/>上的注解信息缓存
/// </summary>
public sealed class DataDisplayCfg
{
    /// <summary>
    /// 展示名
    /// </summary>
    public string displayName;
    /// <summary>
    /// Tip
    /// </summary>
    public string tooltip;

    /// <summary>
    /// 数字字段的最大最小值
    /// </summary>
    public DsonNumber min, max;
    /// <summary>
    /// 是否初始化为null
    /// </summary>
    public bool initNull;
    /// <summary>
    /// 投影Dson类型
    ///
    /// 注：用于将自定义结构导出为Dson结构。
    /// </summary>
    public DsonType dsonType = DsonType.EndOfObject;

    /// <summary>
    /// 展示类型
    ///
    /// 1.List和Map字段的DisplayType表示为Value的展示类型。
    /// 2.字段没有指定展示类型的情况下，需要在运行时查找字段类型的配置，无法提前处理 - 原始的字段定义可能是泛型。
    /// </summary>
    public DataDisplayType displayType;
    /// <summary>
    /// Pop字段的展示名
    ///
    /// 注：List和Map字段的Pop信息表示Value的信息。
    /// </summary>
    public GUIContent[] popNames;
    /// <summary>
    /// int值和枚举值的可选值
    /// </summary>
    public int[] intPopValues;
    /// <summary>
    /// 字符串的可选值
    /// </summary>
    public GUIContent[] stringPopValues;
    /// <summary>
    /// Mask字段时的展示名
    ///
    /// 1.如果是枚举，根据枚举类信息填充
    /// 2.如果是int，则根据用户的配置填充
    /// </summary>
    public string[] maskNames;
    /// <summary>
    /// 多态字段的可用类型
    ///
    /// 注：如果是List或Map字段，表示Value的多态类型。
    /// </summary>
    public string[] supportedTypes;
    /// <summary>
    /// 类型关联的可用实例，可用于初始化类型
    /// </summary>
    public List<DSInst> supportedInsts;

    /// <summary>
    /// 标签类字段配置
    ///
    /// 注：理论上不同的分支下，所有配置都可能需要独立...
    /// (暂不处理，不然配置的复杂度太高；真要处理的可以通过多ctrl实现)
    /// </summary>
    public List<BranchFieldCfg> branchCfgs;
    /// <summary>
    /// 端口字段配置
    /// </summary>
    public PortFieldCfg portCfg;

    /// <summary>
    /// List和Map字段是否启用滚动视图
    /// </summary>
    public bool scrollView;
    /// <summary>
    /// 类型的菜单路径
    /// </summary>
    public string menuPath;
    /// <summary>
    /// List/Map/Nullable元素的展示配置
    /// </summary>
    public DataDisplayCfg elementCfg;

    public bool HasDisplayType { get; private set; }
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
    public static DataDisplayCfg Parse(DSElement element) {
        if (!element.IsOriginDefine) {
            throw new Exception("element must be OriginDefine");
        }
        DataDisplayCfg cfg = new DataDisplayCfg();
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
            ParsMaskInfo(annotation.AsArray(), cfg);
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
            DataDisplayCfg elementCfg = new DataDisplayCfg();
            CopyListCfg(elementCfg, cfg);
            cfg.elementCfg = elementCfg;
        }
        // 暂不缓存Codec中的编码样式，意义不大 
        return cfg;
    }

    private static void CopyListCfg(DataDisplayCfg varCfg, DataDisplayCfg listCfg) {
        varCfg.min = listCfg.min;
        varCfg.max = listCfg.max;
        varCfg.dsonType = listCfg.dsonType;

        varCfg.HasDisplayType = listCfg.HasDisplayType;
        varCfg.displayType = listCfg.displayType;
        varCfg.popNames = listCfg.popNames;
        varCfg.intPopValues = listCfg.intPopValues;
        varCfg.stringPopValues = listCfg.stringPopValues;
        varCfg.maskNames = listCfg.maskNames;
        varCfg.supportedTypes = listCfg.supportedTypes;
    }

    private static void ParseEnumPops(DataDisplayCfg cfg, DSNamedType element) {
        List<DSEnumValue> enumValues = element.GetEnumValues();
        cfg.popNames = new GUIContent[enumValues.Count];
        cfg.intPopValues = new int[enumValues.Count];
        int maxNumber = 0;
        for (int index = 0; index < enumValues.Count; index++) {
            DSEnumValue enumValue = enumValues[index];
            cfg.intPopValues[index] = enumValue.Number;
            //
            Annotation annotation = enumValue.GetAnnotation(DSAnnotations.EDITOR);
            if (annotation != null
                && annotation.AsObject().TryGetValue(DSAnnotations.KEY_DISPLAY_NAME, out DsonValue dsonValue)) {
                string displayName = dsonValue.AsString();
                cfg.popNames[index] = new GUIContent(displayName);
            } else {
                cfg.popNames[index] = new GUIContent(enumValue.SimpleName);
            }
            maxNumber = Math.Max(maxNumber, enumValue.Number);
        }
        // 则额外解析Mask信息 - 需要去掉末尾的空白
        int maxLen = Math.Min(32, maxNumber + 1);
        int len = 0;
        cfg.maskNames = new string[maxLen];
        for (int index = 0; index < maxLen; index++) {
            DSEnumValue enumValue = element.GetEnumValue(index);
            if (enumValue == null) {
                continue;
            }
            cfg.maskNames[index] = enumValue.SimpleName;
            len = index + 1;
        }
        Array.Resize(ref cfg.maskNames, len);
    }

    private static void ParsePopInfo(List<Annotation> annotations, DataDisplayCfg cfg, DSField element) {
        cfg.popNames = new GUIContent[annotations.Count];
        if (element.Type.SimpleName == DSKeywords.TYPE_INT32) {
            cfg.intPopValues = new int[annotations.Count];
        } else {
            cfg.stringPopValues = new GUIContent[annotations.Count];
        }
        for (int index = 0; index < annotations.Count; index++) {
            Annotation annotation = annotations[index];
            DsonObject<string> dsonObject = annotation.AsObject();
            DsonValue dsonValue = dsonObject[DSAnnotations.KEY_VALUE];
            if (cfg.intPopValues != null) {
                // int值才需要配置displayName
                string displayName = dsonObject[DSAnnotations.KEY_DISPLAY_NAME].AsString();
                cfg.popNames[index] = new GUIContent(displayName);
                cfg.intPopValues[index] = dsonValue.AsNumber().IntValue;
            } else {
                // 如果缺少双引号，字符串可能被误解析为数字；我们这里不进行兼容处理：特殊字符串加必须双引号，是规范
                cfg.popNames[index] = GUIContent.none; // 以后可能会用到
                cfg.stringPopValues[index] = new GUIContent(dsonValue.AsString());
            }
        }
    }

    private static void ParseBranchInfo(List<Annotation> annotations, DataDisplayCfg cfg, DSField element) {
        cfg.branchCfgs = new List<BranchFieldCfg>(annotations.Count);
        List<DSField> allFields = element.EnclosingElement.GetFields();
        for (int index = 0; index < annotations.Count; index++) {
            Annotation annotation = annotations[index];
            DsonObject<string> dsonObject = annotation.AsObject();
            string ctrlName = dsonObject[DSAnnotations.KEY_CTRL].AsString();
            if (ctrlName == element.SimpleName) {
                throw new InvalidOperationException("ctrl == element.SimpleName");
            }
            BranchFieldCfg branchCfg = new BranchFieldCfg();
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

    private static void ParsMaskInfo(DsonArray<string> dsonArray, DataDisplayCfg cfg) {
        cfg.maskNames = new string[dsonArray.Count];
        for (int index = 0; index < dsonArray.Count; index++) {
            DsonValue dsonValue = dsonArray[index];
            cfg.maskNames[index] = dsonValue.AsString();
        }
    }

    private static void ParsePolyInfo(DsonArray<string> dsonArray, DataDisplayCfg cfg) {
        cfg.supportedTypes = new string[dsonArray.Count];
        for (int index = 0; index < dsonArray.Count; index++) {
            DsonValue dsonValue = dsonArray[index];
            cfg.supportedTypes[index] = new string(dsonValue.AsString());
        }
    }

    private static void ParsePortInfo(DsonObject<string> dsonObject, DataDisplayCfg cfg) {
        PortFieldCfg portCfg = new PortFieldCfg();
        if (dsonObject.TryGetValue(DSAnnotations.KEY_SIDE, out DsonValue dsonValue)) {
            portCfg.side = Enum.Parse<Side>(dsonValue.AsString(), true);
            Debug.Assert(portCfg.side != Side.Top, "side == top");
        }
        cfg.portCfg = portCfg;
    }

    private static void ParseStyleOptions(DsonObject<string> dsonObject, DataDisplayCfg cfg) {
        cfg.scrollView = Annotation.GetBool(dsonObject, DSAnnotations.KEY_SCROLL_VIEW);
    }

    private static void ParseBaseOptions(DsonObject<string> dsonObject, DataDisplayCfg cfg) {
        if (dsonObject.TryGetValue(DSAnnotations.KEY_DISPLAY_NAME, out DsonValue dsonValue)) {
            cfg.displayName = dsonValue.AsString();
        }
        if (dsonObject.TryGetValue(DSAnnotations.KEY_TOOLTIP, out dsonValue)) {
            cfg.tooltip = dsonValue.AsString();
        }
        if (dsonObject.TryGetValue(DSAnnotations.KEY_DISPLAY_TYPE, out dsonValue)) {
            cfg.displayType = Enum.Parse<DataDisplayType>(dsonValue.AsString(), true);
            cfg.HasDisplayType = true;
        }
        if (dsonObject.TryGetValue(DSAnnotations.KEY_DSON_TYPE, out dsonValue)) {
            cfg.dsonType = Enum.Parse<DsonType>(dsonValue.AsString(), true);
        }
        if (dsonObject.TryGetValue(DSAnnotations.KEY_MIN, out dsonValue)) {
            cfg.min = dsonValue.AsNumber();
        }
        if (dsonObject.TryGetValue(DSAnnotations.KEY_MAX, out dsonValue)) {
            cfg.max = dsonValue.AsNumber();
        }
        cfg.initNull = Annotation.GetBool(dsonObject, DSAnnotations.KEY_INIT_NULL);
        //
        if (dsonObject.TryGetValue(DSAnnotations.KEY_MENU_PATH, out dsonValue)) {
            cfg.menuPath = dsonValue.AsString();
        }
    }
}

/// <summary>
/// Port端口设置
/// </summary>
public sealed class PortFieldCfg
{
    public Side side = Side.Right; // 端口的显示位置
    public bool preferChild = false; // Port是否指的是List元素上的字段
}

/// <summary>
/// 分支字段配置
/// </summary>
public sealed class BranchFieldCfg
{
    public string ctrl; // 控制字段的名字
    public int ctrlIndex; // 控制字段的索引 - 避免每帧搜索
    public string value; // 控制字段的值 - 数字或字符串
    public int intValue; // 控制字段的值 - 避免每帧解析
    public string displayName; // 展示别名
    public string tooltip; // tip
}
}