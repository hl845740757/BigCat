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

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// 图片组
///
/// 注意：文件名必须唯一，程序总是通过文件名引用。
/// </summary>
[CreateAssetMenu(menuName = "BigCat/SpriteGroup", fileName = "NewSpriteGroup")]
public class SpriteGroup : ScriptableObject
{
    /// <summary>
    /// 是否优先使用name引用
    /// </summary>
    [Tooltip("是否可通过[name引用]代替[路径引用]，如果name具有唯一性，则可以勾选")]
    public bool preferName = true;
    /// <summary>
    /// 绑定的文件夹，如果为空则表示当前文件夹
    /// 注：SpriteGroup可以和SpriteAtlas一样放在文件夹外部。
    /// </summary>
    public string bindFolder;

    /// <summary>
    /// 名字是000开始的有序数字
    /// 
    /// 注：
    /// 1.图片会放入到指定的槽位，空缺槽保持为null。
    /// 2.保持顺序性的图组可以通过index访问，不建立额外的字典缓存。
    /// 3.该属性确定以后不可修改，否则可能导致资源引用错误。
    /// </summary>
    public bool sequenced;
    /// <summary>
    /// 所管理的图片
    /// </summary>
    public Sprite[] sprites = Array.Empty<Sprite>();
    /// <summary>
    /// 图片映射
    /// </summary>
    public List<SpriteLink> linkList = new List<SpriteLink>();
    /// <summary>
    /// 无序图组根据name建立的映射
    /// </summary>
    [NonSerialized]
    private Dictionary<string, Sprite> spriteDic;

    public Sprite this[int index] {
        get => sprites[index];
        set => sprites[index] = value;
    }

    public int Count {
        get => sprites.Length;
        set => Array.Resize(ref sprites, value);
    }

    public Sprite GetSprite(ObjectPath spritePath) {
        string localPath = spritePath.localPath;
        if (!string.IsNullOrEmpty(localPath)) {
            return GetSprite(localPath);
        }
        int index = (int)spritePath.localId;
        if (index < 0 || index >= sprites.Length) {
            return null;
        }
        return sprites[index];
    }

    /// <summary>
    /// 通过index查找Sprite
    /// </summary>
    public Sprite GetSprite(int index) {
        if (index < 0 || index >= sprites.Length) {
            return null;
        }
        return sprites[index];
    }

    /// <summary>
    /// 通过name查找Sprite
    /// </summary>
    public Sprite GetSprite(string name) {
        if (sequenced) {
            if (int.TryParse(name, out int index)) {
                return sprites[index];
            }
        } else {
            if (spriteDic == null) {
                RefreshDic();
            }
            if (spriteDic!.TryGetValue(name, out Sprite value)) {
                return value;
            }
        }
        return null;
    }

    private void RefreshDic() {
        Dictionary<string, Sprite> dict = spriteDic;
        if (dict == null) {
            dict = spriteDic = new Dictionary<string, Sprite>(sprites.Length);
        } else {
            dict.Clear();
        }
        dict.EnsureCapacity(sprites.Length);
        foreach (Sprite sprite in sprites) {
            dict[sprite.name] = sprite;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Refresh")]
    private void Refresh() {
        Refresh(this);
    }

    /// <summary>
    /// 刷新图组信息
    /// (注：放在这里方便工具统一调用，避免依赖Editor)
    /// </summary>
    /// <param name="group"></param>
    public static void Refresh(SpriteGroup group) {
        string groupAssetDir;
        if (string.IsNullOrEmpty(group.bindFolder)) {
            groupAssetDir = AssetDatabase.GetAssetPath(group);
            groupAssetDir = groupAssetDir.Substring(0, groupAssetDir.LastIndexOf('/'));
        } else {
            groupAssetDir = group.bindFolder;
        }
        string[] findAssets = AssetDatabase.FindAssets("t:Sprite", new[] { groupAssetDir });
        List<Sprite> list = new(findAssets.Length);
        foreach (string guid in findAssets) {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite && sprite.name.IndexOf('(') < 0) { // 名字包含括号的忽略
                list.Add(sprite);
            }
        }
        //
        group.sprites = list.ToArray();
        group.Sort();
        EditorUtility.SetDirty(group);
    }

    /// <summary>
    /// 根据name排序
    ///
    /// 注：非sequence图组也排序，可保证命名重复的情况下结果稳定。
    /// </summary>
    private void Sort() {
        List<Sprite> list = new List<Sprite>(sprites);
        list.RemoveAll(e => e == null);
        if (list.Count == 0) {
            sprites = list.ToArray();
            return;
        }
        // 非序列图，按照name排序，保持加载结果稳定
        if (!sequenced) {
            list.Sort(CompareSprite);
            sprites = list.ToArray();
            RefreshDic();
            return;
        }
        // 序列图，按照index插入
        int length = int.Parse(list.PeekLast().name) + 1;
        sprites = new Sprite[length];
        foreach (Sprite sprite in list) {
            string spriteName = sprite.name;
            if (spriteName.Length > 3) {
                throw new InvalidOperationException("invalid sprite name: " + spriteName);
            }
            int index = int.Parse(spriteName);
            sprites[index] = sprite;
        }
        // 索引映射
        foreach (SpriteLink link in linkList) {
            if (sprites[link.index] == null) {
                sprites[link.index] = sprites[link.dest];
            }
        }
    }

    private static int CompareSprite(Sprite a, Sprite b) {
        string nameA = a.name;
        string nameB = b.name;
        // 如果都是数字，则按照数字排序
        bool b1 = int.TryParse(nameA, out int num1);
        bool b2 = int.TryParse(nameB, out int num2);
        if (b1 && b2) {
            return num1.CompareTo(num2);
        }
        // 数字排普通字符串前面
        if (b1) return -1;
        if (b2) return 1;
        // 否则按照字符串排序
        return string.Compare(nameA, nameB, StringComparison.Ordinal);
    }
#endif
}
}