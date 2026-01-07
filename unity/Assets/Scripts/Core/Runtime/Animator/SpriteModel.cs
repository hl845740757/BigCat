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

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 2D角色模型
///
/// 注：运行时需要指定图集（贴图），否则没有表现。
/// </summary>
[CreateAssetMenu(menuName = "BigCat/SpriteModel", fileName = "NewSpriteModel")]
public sealed class SpriteModel : ScriptableObject
{
    /// <summary>
    /// 模型部件id
    ///
    /// 注：虽然使用数字也是可以的，但使用起来有较多不便，动作的Name同理。
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
    [Tooltip("部件所属的组")]
    public int partGroup;
    /// <summary>
    /// 部件的渲染层级
    /// </summary>
    public int partLayer;

    /// <summary>
    /// 模型id
    /// </summary>
    public int modelId;
    /// <summary>
    /// 默认贴图路径(延迟加载)
    /// </summary>
    [Tooltip("默认贴图")]
    public string spriteGroupPath;
    /// <summary>
    /// 模型动作
    /// 1.由于攻击盒数据也在动作信息上，因此要求动作信息同步加载。
    /// 2.动作信息整体来说还是比较轻量级的，因此同步加载的影响较小。
    /// </summary>
    [Tooltip("逻辑动作名到美术资源的映射")]
    public List<SpriteMotionRedir> motionList = new();
    /// <summary>`
    /// 模型动作映射缓存
    /// </summary>
    [NonSerialized]
    public readonly Dictionary<string, SpriteMotionRedir> motionDic = new();

    /// <summary>
    /// 查找动作
    /// </summary>
    /// <param name="motionName">要查找的动作名</param>
    /// <returns></returns>
    public SpriteMotionRedir FindMotion(string motionName) {
        motionDic.TryGetValue(motionName, out var motion);
        return motion;
    }

    #region 序列化

    private void OnEnable() {
        RebuildCache();
    }

    /// <summary>
    /// 构建缓存信息，允许运行时调用
    /// </summary>
    public void RebuildCache() {
        if (string.IsNullOrEmpty(partId)) {
            partId = this.name;
        }
        // Motion
        motionDic.Clear();
        motionDic.EnsureCapacity(motionList.Count);
        for (int index = 0; index < motionList.Count; index++) {
            var motion = motionList[index];
            if (!motion.clip) {
                continue;
            }
            // name池化 - 为空的情况下默认为动画名
            motion.name = string.IsNullOrEmpty(motion.name) ? motion.clip.name : motion.name;
            motionList[index] = motion;
            motionDic.Add(motion.name, motion);
        }
    }

    #endregion
}
}