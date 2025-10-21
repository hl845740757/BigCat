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
using UnityEngine;

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// 图片组
///
/// 注意：该对象的数据由程序维护，避免手动修改。
/// </summary>
[CreateAssetMenu(menuName = "BigCat/SpriteGroup", fileName = "NewSpriteGroup")]
public class SpriteGroup : ScriptableObject
{
    /// <summary>
    /// 是否优先使用name引用
    /// </summary>
    [Tooltip("是否可通过[name引用]代替[路径引用]，如果name具有唯一性，则可以勾选")]
    public bool preferName;
    /// <summary>
    /// 所管理的图片
    /// </summary>
    public Sprite[] sprites = Array.Empty<Sprite>();

    public Sprite this[int index] {
        get => sprites[index];
        set => sprites[index] = value;
    }

    public int Count {
        get => sprites.Length;
        set => Array.Resize(ref sprites, value);
    }

    public Sprite GetSprite(int index) {
        if (index < 0 || index >= sprites.Length) {
            return null;
        }
        return sprites[index];
    }

#if UNITY_EDITOR
    /// <summary>
    /// 根据图片名字查询索引
    /// </summary>
    /// <param name="spriteName"></param>
    /// <returns></returns>
    public int IndexOf(string spriteName) {
        for (int index = 0; index < sprites.Length; index++) {
            Sprite sprite = sprites[index];
            if (sprite && sprite.name == spriteName) {
                return index;
            }
        }
        return -1;
    }
#endif
}
}