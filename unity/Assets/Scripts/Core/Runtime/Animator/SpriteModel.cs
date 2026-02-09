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

#if UNITY_EDITOR
using Wjybxx.Commons;
using Wjybxx.BigCat.Core;
using UnityEditor;
#endif

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 2D角色模型(资源)
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
    /// 2.角色通常可划分为三组：Body + 武器 + 其它；极限情况下可以每个部件1组。
    /// </summary>
    [Tooltip("部件所属的组")]
    public int partGroup;
    /// <summary>
    /// 部件的渲染层级
    /// </summary>
    public int partLayer;

#if UNITY_EDITOR
    /// <summary>
    /// 基准模型
    /// 注：使用路径引用，避免打包时产生不期望的依赖。
    /// </summary>
    [Tooltip("模型对象路径；当指定模板时，则读取模板模型的配置，在绑定folder中查找资源")]
    public string templatePath;
    /// <summary>
    /// 绑定的动画文件夹
    /// </summary>
    [Tooltip("绑定的动画文件夹")]
    public string bindFolder;
#endif
    /// <summary>
    /// 模型动作
    /// 1.由于攻击盒数据也在动作信息上，因此要求动作信息同步加载。
    /// 2.动作信息整体来说还是比较轻量级的，因此同步加载的影响较小。
    /// </summary>
    [Tooltip("动作列表；前面的覆盖后面的 - 特殊配置放前面")]
    [ContextMenuItem("刷新", "Refresh")]
    public List<SpriteMotionRedir> motionList = new();
    /// <summary>`
    /// 模型动作映射缓存(忽略大小写更易用)
    /// </summary>
    [NonSerialized]
    public readonly Dictionary<string, SpriteMotionRedir> motionDic = new(StringComparer.OrdinalIgnoreCase);

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
            motionDic.TryAdd(motion.name, motion); // 特殊动作放前面
        }
    }

    #endregion

    #region 维护

    private void Refresh() {
        string groupAssetDir = SpriteGroup.GetBindFolder(this, bindFolder);
        SpriteModel template = string.IsNullOrEmpty(templatePath) ? null : AssetDatabase.LoadAssetAtPath<SpriteModel>(templatePath);
        // 如果模板存在，则读取模板资源信息 - 然后覆盖本地信息
        if (template && template != this) {
            motionList.Clear();
            motionList.AddRange(template.motionList);
            for (int index = 0; index < motionList.Count; index++) {
                SpriteMotionRedir motion = motionList[index];
                if (!motion.clip) continue;
                // 替换为绑定目录下的资产
                string clipPath = groupAssetDir + "/" + motion.clip.name + ".asset";
                motion.clip = AssetDatabase.LoadAssetAtPath<SpriteAnimationClip>(clipPath);
                motionList[index] = motion;
            }
            EditorUtility.SetDirty(this);
            return;
        }
        // 如果没有模板，则删除无效的Motion映射，并自动导入新动画
        HashSet<string> existNames = new();
        for (int idx = 0; idx < motionList.Count; idx++) {
            SpriteMotionRedir motion = motionList[idx];
            if (motion.clip) {
                existNames.Add(motion.name);
                existNames.Add(motion.clip.name);
                continue;
            }
            if ((motion.options & AnimationOptions.ReservedMotion) != 0) {
                existNames.Add(motion.name);
            } else {
                motionList.RemoveAt(idx--);
            }
        }
        string[] findAssets = AssetDatabase.FindAssets("t:SpriteAnimationClip", new[] { groupAssetDir });
        foreach (string guid in findAssets) {
            string clipPath = AssetDatabase.GUIDToAssetPath(guid);
            SpriteAnimationClip clip = AssetDatabase.LoadAssetAtPath<SpriteAnimationClip>(clipPath);
            if (existNames.Contains(clip.name)) {
                continue;
            }
            SpriteMotionRedir motion = new SpriteMotionRedir() { name = clip.name, clip = clip };
            motionList.Add(motion);
        }
        EditorUtility.SetDirty(this);
    }

    #endregion
}
}