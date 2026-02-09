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

using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCat.Audio
{
/// <summary>
/// 音频播放请求
/// </summary>
[DsonSerializable]
public struct AudioRequest
{
    public AudioPlayMode playMode; // 播放模式，决定参数有效性
    public string audioPath; // 资源路径 -- 短音频为音效组路径
    public string clipName; // 短音频名 -- 如果不为空，表示使用AudioGroup
    public bool loop; // 是否循环 -- 仅适用长音频
    public float volume; // 音量 -- 仅适用短促音效，长音频应当通过设置播放器的音量实现
    public float fadeInTime; // 音频淡入时间 -- 可能还需要支持其它淡出算法
    public float fadeOutTime; // 旧音频淡出时间
}

/// <summary>
/// 音频播放模式
/// </summary>
public enum AudioPlayMode : byte
{
    PlayOneShot = 0, // 播放短促音效
    PlayAtPoint = 1, // 播放短促音效(暂未实现)
    PlayClip = 2, // 普通播放
}
}