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
using UnityEngine.U2D;

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 2D图片模型
///
/// 注：运行时需要指定图集（贴图），否则没有表现。
/// </summary>
[CreateAssetMenu(menuName = "BigCat/SpriteModel", fileName = "NewSpriteModel")]
public sealed class SpriteModel : ScriptableObject, ISerializationCallbackReceiver
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
    /// 模型默认图集
    /// </summary>
    [Tooltip("模型默认图集，即默认贴图")]
    public SpriteAtlas spriteAtlas;
    /// <summary>
    /// 模型动作
    /// </summary>
    [Tooltip("模型关联的动作")]
    public List<SpriteAnimationClip> actionList = new List<SpriteAnimationClip>();
    /// <summary>
    /// 动作映射
    /// </summary>
    [Tooltip("动作映射信息，逻辑动作名到美术资源的映射")]
    public List<KeyValuePair<string, string>> actionRemap = new();
    /// <summary>
    /// 动作名到动作的映射缓存
    /// (运行时使用，包含动作映射数据，反序列化自动处理)
    /// </summary>
    [NonSerialized]
    public Dictionary<string, SpriteAnimationClip> actionDic = new();

    /// <summary>
    /// 动作之间的融合配置
    /// (动作融合按照真实action配置，不处理动作重映射)
    /// </summary>
    [Tooltip("动作融合信息")]
    public List<AnimationMixCfg> actionMixCfgList = new();
    /// <summary>
    /// 动作融合配置缓存
    /// </summary>
    [NonSerialized]
    public Dictionary<(string, string), AnimationMixCfg> actionMixCfgDic = new();

    /// <summary>
    /// 查找Action
    ///
    /// 注：如果返回的资源名和请求Action名不一样，表示动作存在重映射。
    /// </summary>
    /// <param name="actionName">要查找的动作名</param>
    /// <param name="resolveRemap">是否处理重映射</param>
    /// <returns></returns>
    public SpriteAnimationClip FindAction(string actionName, bool resolveRemap = true) {
#if UNITY_EDITOR
        if (resolveRemap) {
            actionName = ResolveRemap(actionName);
        }
        foreach (var action in actionList) {
            if (action.name == actionName) {
                return action;
            }
        }
        return null;
#else
        if (!actionDic.TryGetValue(actionName, out var action)) return null;
        return (resolveRemap || action.name == actionName) ? action : null;
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

    /// <summary>
    /// 解析重映射信息，找到最终的actionName
    ///
    /// 注：避免频繁调用，性能不佳。
    /// </summary>
    /// <param name="actionName"></param>
    /// <returns></returns>
    public string ResolveRemap(string actionName) {
        for (int depth = 0; depth < 5; depth++) {
            KeyValuePair<string, string> pair = actionRemap.Find(pair2 => pair2.Key == actionName);
            if (pair.Key != actionName) {
                return actionName;
            }
            actionName = pair.Value;
        }
        throw new Exception("Action mapping exceeds limit");
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
            SpriteAction action = actionList[index];
            if (string.IsNullOrWhiteSpace(action.name)) {
                continue;
            }
            // name池化
            action.name = string.Intern(action.name);
            actionDic.Add(action.name, action);
        }

        // ActionRemap
        for (int index = 0; index < actionRemap.Count; index++) {
            KeyValuePair<string, string> pair = actionRemap[index];
            // name池化
            string key = string.Intern(pair.Key);
            string value = string.Intern(pair.Value);
            actionRemap[index] = new KeyValuePair<string, string>(key, value);
        }
        // 缓存最终映射结果
        for (int index = 0; index < actionRemap.Count; index++) {
            KeyValuePair<string, string> pair = actionRemap[index];
            string actionName = ResolveRemap(pair.Key);
            SpriteAction action = actionList.Find(e => e.name == actionName);
            actionDic.Add(pair.Key, action);
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