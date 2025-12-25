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

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 端口信息
/// </summary>
public sealed class PlayablePort
{
    /// <summary>
    /// 端口名
    /// </summary>
    public string name;
    /// <summary>
    /// 端口初始权重
    /// </summary>
    public float weight;
    /// <summary>
    /// 端口索引
    /// 注：运行时根据实际数据赋值，避免程序依赖，程序使用端口名建立稳定引用。
    /// </summary>
    [NonSerialized]
    public int index;
    /// <summary>
    /// 端口连线的起点
    /// </summary>
    [NonSerialized]
    public PlayableNode srcNode;
    /// <summary>
    /// 端口连接的目标(可能为null)
    /// </summary>
    [SerializeReference]
    public PlayableNode destNode;
}
}