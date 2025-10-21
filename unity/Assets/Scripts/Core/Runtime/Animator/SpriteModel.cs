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
    /// 2.虽然使用数字也是可以的，但使用起来有较多不便，动作的Name同理。
    /// </summary>
    [Tooltip("部件id")]
    public string partId;
    /// <summary>
    /// 部件归属的组
    ///
    /// 注：
    /// 1.将Body相关的部件归属在同一组，使得我们可以按组切换模型动作。
    /// 2.角色通常可划分为三组：Body + 武器 + 其它。
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
    /// (也应该通过路径引用)
    /// </summary>
    [Tooltip("模型默认图集，即默认贴图")]
    public SpriteGroup spriteGroup;
    /// <summary>
    /// 模型动作
    /// </summary>
    [Tooltip("逻辑动作名到美术资源的映射")]
    public List<SpriteMotionRedir> motionList = new();
    /// <summary>
    /// 模型动作映射缓存
    /// </summary>
    [NonSerialized]
    public Dictionary<string, SpriteAnimationClip> motionDic = new();

    /// <summary>
    /// 动作之间的融合配置
    /// (不处理动作重映射)
    /// </summary>
    [Tooltip("动作融合信息")]
    public List<AnimationMixCfg> motionMixCfgList = new();
    /// <summary>
    /// 动作融合配置缓存
    /// </summary>
    [NonSerialized]
    public Dictionary<(string, string), AnimationMixCfg> motionMixCfgDic = new();

    /// <summary>
    /// 查找动作
    /// </summary>
    /// <param name="motionName">要查找的动作名</param>
    /// <returns></returns>
    public SpriteAnimationClip FindMotion(string motionName) {
#if UNITY_EDITOR
        foreach (var motion in motionList) {
            if (motion.name == motionName) {
                return motion.clip;
            }
        }
        return null;
#else
        motionDic.TryGetValue(motionName, out var motion);
        return motion;
#endif
    }

    /// <summary>
    /// 查找动作融合配置
    /// </summary>
    /// <param name="motionA"></param>
    /// <param name="motionB"></param>
    /// <returns></returns>
    public AnimationMixCfg FindMotionMixCfg(string motionA, string motionB) {
#if UNITY_EDITOR
        foreach (AnimationMixCfg mixCfg in motionMixCfgList) {
            if (mixCfg.motionA == motionA && mixCfg.motionB == motionB) {
                return mixCfg;
            }
        }
        return null;
#else
        (string, string) key = (motionA, motionB);
        return motionMixCfgDic.TryGetValue(key, out var mixCfg) ? mixCfg : null;
#endif
    }

    #region 序列化

    public void OnBeforeSerialize() {
        // 如何保存由用户决定
    }

    public void OnAfterDeserialize() {
        // 编辑器模式下会频繁的序列化和反序列化，因此不池化字符串
#if !UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(partId)) {
            partId = string.Intern(partId);
        }
        // Motion
        motionDic.Clear();
        for (int index = 0; index < motionList.Count; index++) {
            var motionRedir = motionList[index];
            if (string.IsNullOrWhiteSpace(motionRedir.name)) {
                continue;
            }
            // name池化
            motionRedir.name = string.Intern(motionRedir.name);
            motionDic.Add(motionRedir.name, motionRedir.clip);
        }
        // MixCfg
        motionMixCfgDic.Clear();
        for (int index = 0; index < motionMixCfgList.Count; index++) {
            AnimationMixCfg mixCfg = motionMixCfgList[index];
            if (string.IsNullOrWhiteSpace(mixCfg.motionA)
                || string.IsNullOrWhiteSpace(mixCfg.motionB)) {
                continue;
            }
            mixCfg.motionA = string.Intern(mixCfg.motionA);
            mixCfg.motionB = string.Intern(mixCfg.motionB);
            motionMixCfgList[index] = mixCfg; // 兼容值类型
            motionMixCfgDic[(mixCfg.motionA, mixCfg.motionB)] = mixCfg;
        }
#endif
    }

    #endregion
}
}