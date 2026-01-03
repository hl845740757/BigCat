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

namespace Wjybxx.BigCat.Animator
{
/// <summary>
/// 动画播放请求
/// </summary>
[Serializable]
public struct AnimationRequest
{
    public SpriteAnimationClip clip; // 播放的动画
    public EWrapMode wrapMode; // 动画播放模式
    public int startFrame; // 开始帧
    public int endFrame; // 结束帧
}
}