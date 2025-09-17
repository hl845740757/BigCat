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
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 基于帧动画的（角色)模型
/// </summary>
[CreateAssetMenu(menuName = "FrameAnimation/AnimationModel", fileName = "NewAnimationModel")]
public sealed class FrameAnimationModel : ScriptableObject, ISerializationCallbackReceiver
{
    /// <summary>
    /// 模型部件id
    ///
    /// 注：
    /// 1.运行会根据id创建对应name的子GameObject负责部件的渲染。
    /// 2.虽然使用数字也是可以的，但使用起来有较多不便，Action的Name同理。
    /// </summary>
    [Tooltip("部件id")]
    public string partId;
    /// <summary>
    /// 部件归属的组
    ///
    /// 注：
    /// 1.将Body相关的部件归属在同一组，使得我们可以按组切换模型动作。
    /// 2.角色通常划分为两组：Body + 武器。
    /// </summary>
    [Tooltip("部件所属的组，建议角色Body相关的部件归属在同一组")]
    public int partGroupId;
    /// <summary>
    /// 部件渲染层级
    /// (图层内排序)
    /// </summary>
    [Tooltip("部件渲染顺序")]
    public int orderInLayer;

    /// <summary>
    /// 模型基础动画
    ///
    /// 注：对于角色模型，推荐所有帧打包为一个帧动画，然后通过区间播放；这可以有更好的加载效率，也避免过多的资产文件。
    /// </summary>
    public FrameAnimationClip modelClip;
    /// <summary>
    /// 模型动作
    /// </summary>
    public List<FrameAnimationAction> actionList = new List<FrameAnimationAction>();
    /// <summary>
    /// 动作名到动作的映射
    /// </summary>
    [NonSerialized]
    public Dictionary<string, FrameAnimationAction> actionDic = new();

    /// <summary>
    /// 动作之间的融合配置
    /// </summary>
    public List<AnimationMixCfg> actionMixCfgList = new();
    /// <summary>
    /// 动作融合配置缓存
    /// </summary>
    [NonSerialized]
    public Dictionary<(string, string), AnimationMixCfg> actionMixCfgDic = new();

    /// <summary>
    /// 查找Action
    /// </summary>
    /// <param name="actionName"></param>
    /// <returns></returns>
    public FrameAnimationAction FindAction(string actionName) {
#if UNITY_EDITOR
        foreach (var action in actionList) {
            if (action.name == actionName) {
                return action;
            }
        }
        return null;
#else
        return actionDic.TryGetValue(actionName, out var action) ? action : null;
#endif
    }

    /// <summary>
    /// 查找Action融合配置
    /// </summary>
    /// <param name="actionA"></param>
    /// <param name="actionB"></param>
    /// <returns></returns>
    public AnimationMixCfg FindActionMixCfg(string actionA, string actionB) {
#if UNITY_EDITOR
        foreach (AnimationMixCfg mixCfg in actionMixCfgList) {
            if (mixCfg.actionA == actionA && mixCfg.actionB == actionB) {
                return mixCfg;
            }
        }
        return null;
#else
        (string, string) key = (actionA, actionB);
        return actionMixCfgDic.TryGetValue(key, out var actionMixCfg) ? actionMixCfg : null;
#endif
    }

    #region 序列化

    public void OnBeforeSerialize() {
        // 用于用户可能在编辑器中直接重排序Action，因此如何保存由用户决定
        // actionList.Clear();
        // actionList.AddRange(actionDic.Values);
    }

    public void OnAfterDeserialize() {
        // 编辑器模式下会频繁的序列化和反序列化，因此不池化字符串
#if !UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(partId)) {
            partId = string.Intern(partId);
        }
        // Action
        actionDic.Clear();
        for (int index = 0; index < actionList.Count; index++) {
            FrameAnimationAction action = actionList[index];
            if (string.IsNullOrWhiteSpace(action.name)) {
                continue;
            }
            // name池化
            action.name = string.Intern(action.name);
            actionList[index] = action; // 兼容值类型
            actionDic.Add(action.name, action);
        }
        // MixCfg
        actionMixCfgDic.Clear();
        for (int index = 0; index < actionMixCfgList.Count; index++) {
            AnimationMixCfg mixCfg = actionMixCfgList[index];
            if (string.IsNullOrWhiteSpace(mixCfg.actionA)
                || string.IsNullOrWhiteSpace(mixCfg.actionB)) {
                continue;
            }
            mixCfg.actionA = string.Intern(mixCfg.actionA);
            mixCfg.actionB = string.Intern(mixCfg.actionB);
            actionMixCfgList[index] = mixCfg; // 兼容值类型
            actionMixCfgDic[(mixCfg.actionA, mixCfg.actionB)] = mixCfg;
        }
#endif
    }

    #endregion
}
}