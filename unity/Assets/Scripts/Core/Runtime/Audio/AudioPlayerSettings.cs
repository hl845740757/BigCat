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
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCat.Audio
{
/// <summary>
/// 音频播放器设置
/// </summary>
[Serializable]
[DsonSerializable]
public sealed class AudioPlayerSettings
{
    [NonSerialized]
    public AudioPlayerSettings parent;
    [Range(0, 1)]
    public float volume = 1f; // 目标音量
    public float pitch = 1f; // 目标音高
    public bool mute; // 静音

    public float spatialBlend; // 空间混合 - 0即为2D音效
    public float dopplerLevel = 1f; // 多普勒效应等级
    public int spread; // 声音扩散角度，范围 [0, 360]
    // public float minDistance = 1f; // 最小距离
    // public float maxDistance = 500f; // 最大距离

    // 音量信息缓存，避免递归计算
    public float realVolume;
    public float realPitch;
    public bool realMute;

    public void SyncFrom(AudioPlayerSettings source) {
        this.volume = source.volume;
        this.pitch = source.pitch;
        this.mute = source.mute;
        //
        this.spatialBlend = source.spatialBlend;
        this.dopplerLevel = source.dopplerLevel;
        this.spread = source.spread;
    }
}
}