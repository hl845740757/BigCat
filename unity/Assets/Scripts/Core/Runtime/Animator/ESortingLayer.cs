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

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 场景Sprite层级
/// </summary>
public enum ESortingLayer : byte
{
    DistantBack = 0, // 远程背景 - 天空河流
    MiddleBack = 1, // 中程背景 - 山石树木
    Bottom = 2, // 底层 - Tile
    Normal = 3, // 普通层 - 角色
    CloseBack = 4, // 近距背景 - 栏杆/围墙/树木
    Close = 5, // 近距层 - 云雾光照
}

/// <summary>
/// 场景Sprite层级内排序
/// </summary>
public enum ESortingOrder : byte
{
    Below = 0, // 普通层下方 - 被动交互对象(宝箱)
    Normal = 1, // 中层 - 角色
    Above = 2, // 普通层上方 - 角色特效
}
}