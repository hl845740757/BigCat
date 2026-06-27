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
using Wjybxx.BigCat.Assetor;
using Wjybxx.Commons;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// 音效组
///
/// 注意：文件名必须唯一，程序总是通过文件名引用。
/// </summary>
[CreateAssetMenu(menuName = "BigCat/AudioGroup", fileName = "NewAudioGroup")]
public class AudioGroup : ScriptableObject, IBinaryAssetReceiver
{
    /// <summary>
    /// 是否优先使用name引用
    /// </summary>
    [Tooltip("是否可通过[name引用]代替[路径引用]，如果name具有唯一性，则可以勾选")]
    public bool preferName = true;
    /// <summary>
    /// 绑定的文件夹
    /// 注：AudioGroup可以和SpriteAtlas一样放在文件夹外部。
    /// </summary>
    public string bindFolder;

    /// <summary>
    /// 关联的音效资源
    /// </summary>
    [ContextMenuItem("刷新", "Refresh")]
    public AudioClip[] audioClips = Array.Empty<AudioClip>();
    /// <summary>
    /// 根据name建立的映射
    /// </summary>
    [NonSerialized]
    private Dictionary<string, AudioClip> audioClipDic;

    public AudioClip this[int index] {
        get => audioClips[index];
        set => audioClips[index] = value;
    }

    public int Count {
        get => audioClips.Length;
        set => Array.Resize(ref audioClips, value);
    }

    public AudioClip GetAudioClip(ObjectPath audioPath) {
        string localPath = audioPath.localPath;
        if (!string.IsNullOrEmpty(localPath)) {
            return GetAudioClip(localPath);
        }
        int index = audioPath.localId;
        if (index < 0 || index >= audioClips.Length) {
            return null;
        }
        return audioClips[index];
    }

    /// <summary>
    /// 通过index查找Sprite
    /// </summary>
    public AudioClip GetAudioClip(int index) {
        if (index < 0 || index >= audioClips.Length) {
            return null;
        }
        return audioClips[index];
    }

    /// <summary>
    /// 通过name查找Audio
    /// </summary>
    public AudioClip GetAudioClip(string name) {
        if (audioClipDic == null) {
            RefreshDic();
        }
        if (audioClipDic!.TryGetValue(name, out AudioClip value)) {
            return value;
        }
        return null;
    }

    private void RefreshDic() {
        Dictionary<string, AudioClip> dict = audioClipDic;
        if (dict == null) {
            dict = audioClipDic = new Dictionary<string, AudioClip>(audioClips.Length);
        } else {
            dict.Clear();
        }
        dict.EnsureCapacity(audioClips.Length);
        foreach (AudioClip audioClip in audioClips) {
            dict[audioClip.name] = audioClip;
        }
    }

    public void Unpack(BinaryAsset binAsset) {
        // TODO
    }
#if UNITY_EDITOR
    /// <summary>
    /// 刷新绑定数据
    /// (注：放在这里方便工具统一调用，避免依赖Editor)
    /// </summary>
    [ContextMenu("Refresh")]
    public void Refresh() {
        string groupAssetDir = SpriteGroup.GetBindFolder(this, bindFolder);
        string[] findAssets = AssetDatabase.FindAssets("t:AudioClip", new[] { groupAssetDir });
        List<AudioClip> list = new(findAssets.Length);
        foreach (string guid in findAssets) {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (audioClip && audioClip.name.IndexOf('(') < 0) { // 名字包含括号的忽略
                list.Add(audioClip);
            }
        }
        //
        audioClips = list.ToArray();
        Sort();
        EditorUtility.SetDirty(this);
    }

    /// <summary>
    /// 根据name排序
    ///
    /// 注：可保证命名重复的情况下结果稳定。
    /// </summary>
    private void Sort() {
        List<AudioClip> list = new List<AudioClip>(audioClips);
        list.RemoveAll(e => e == null);
        if (list.Count == 0) {
            audioClips = list.ToArray();
            return;
        }
        list.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        audioClips = list.ToArray();
        RefreshDic();
    }
#endif
}
}