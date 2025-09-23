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

using System.Collections.Generic;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 动画状态配置（序列帧动画、Spine、3D动画）
/// 
/// 注：提取该抽象以方便扩展真正的AnimationState。
/// </summary>
[DsonSerializable]
public sealed class AnimationStateCfg
{
    /// <summary>
    /// 部件组 => 要播放的Action
    ///
    /// 注：数据量很少，通常1~2，因此也可以改用List。
    /// </summary>
    public ArrayDictionary<int, string> group2Actions = new ArrayDictionary<int, string>();
    /// <summary>
    /// 所有的事件
    /// </summary>
    public List<AnimationEvent> events = new List<AnimationEvent>();
}
}