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
/// 角色动作融合配置
///
/// 注：考虑到扩展性，应保持为引用类型。
/// </summary>
[Serializable]
public sealed class AnimationMixCfg
{
    public string actionA;
    public string actionB;
    public float weightA = 0.5f;
    public float weightB = 0.5f;
    public float crossFadeTime; // 过渡时间

    public AnimationMixCfg() {
    }

    public AnimationMixCfg(AnimationMixCfg src) {
        this.actionA = src.actionA;
        this.actionB = src.actionB;
        this.weightA = src.weightA;
        this.weightB = src.weightB;
        this.crossFadeTime = src.crossFadeTime;
    }
}
}