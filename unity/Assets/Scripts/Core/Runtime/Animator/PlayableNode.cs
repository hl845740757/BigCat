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
using UnityEngine.Playables;

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// Playable节点信息
///
/// 注：Graph整体的结构保持不变，只动态切换绑的的<see cref="Playable"/>。
/// </summary>
public sealed class PlayableNode
{
    /// <summary>
    /// 节点的名字(禁止修改)
    /// </summary>
    public string name;
    /// <summary>
    /// Playable类型
    /// </summary>
    public EPlayableType playableType;
    /// <summary>
    /// 脚本模板
    /// </summary>
    [SerializeReference]
    public PlayableBehaviour template;

    /// <summary>
    /// GetPlayableType的返回值类型：<see cref="IPlayable"/>
    /// </summary>
    [NonSerialized]
    public Playable playable;
    /// <summary>
    /// 关联的脚本对象
    /// </summary>
    [NonSerialized]
    public PlayableBehaviour behaviour;
    /// <summary>
    /// 输入端口（信息归属上游）
    /// </summary>
    [NonSerialized]
    public List<PlayablePort> inputs = new List<PlayablePort>();
    /// <summary>
    /// 输出端口(通常为1)
    /// </summary>
    public List<PlayablePort> outputs = new List<PlayablePort>();
}
}