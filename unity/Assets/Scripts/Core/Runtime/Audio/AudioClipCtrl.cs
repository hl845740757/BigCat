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

using UnityEngine;
using Wjybxx.BigCat.Assetor;

namespace Wjybxx.BigCat.Audio
{
/// <summary>
/// 单个<see cref="AudioClip"/>控制器
/// </summary>
public sealed class AudioClipCtrl
{
    private readonly AudioPlayer player;
    private readonly AudioSource audioSource;
    private AudioRequest request;
    private AssetHandle handle;

    /// <summary>
    /// Clip的状态
    /// </summary>
    private Status status;
    /// <summary>
    /// 淡入淡出状态
    /// </summary>
    private FadeStatus fadeStatus;
    /// <summary>
    /// 淡出时间
    /// </summary>
    private float fadeOutTime;
    /// <summary>
    /// 淡入淡出进度(时间)
    /// </summary>
    private float fadeProgress;

    public AudioClipCtrl(AudioPlayer player, AudioSource audioSource) {
        this.player = player;
        this.audioSource = audioSource;
    }

    /// <summary>
    /// Clip的播放状态
    /// </summary>
    public bool IsPlaying => status == Status.Playing;
    public bool IsStopped => status == Status.Stopped;
    /// <summary>
    /// 当前音频地址
    /// </summary>
    public string AudioPath => request.audioPath;

    /// <summary>
    /// 播放请求
    /// </summary>
    public void Play(in AudioRequest request, AssetHandle handle) {
        this.request = request;
        this.handle = handle;
        //
        status = Status.Loading;
        if (handle.IsCompleted) {
            OnLoadCompleted(handle);
        } else {
            handle.Completed += OnLoadCompleted;
        }
    }

    private void OnLoadCompleted(AssetHandle handle2) {
        if (this.handle != handle2) {
            return; // 已被中断播放；测试handle的准确性高于status
        }
        status = Status.Ready;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="deltaTime">deltaTime</param>
    /// <param name="targetVolume">播放器的目标音量</param>
    public void Update(float deltaTime, float targetVolume) {
        if (status < Status.Ready) return; // Stopped/Loading
        if (status == Status.Ready) {
            if (player.HasPrevClip) {
                return;
            }
            StartPlay();
        }
        // Playing状态
        if (fadeStatus == FadeStatus.Stopped) {
            audioSource.volume = targetVolume;
            if (!audioSource.isPlaying && audioSource.time == 0f) {
                Stop(); // 测试time以区分Pause和Stop
            }
            return;
        }
        fadeProgress += deltaTime;
        // FadeOut
        if (fadeStatus == FadeStatus.FadeOut) {
            if (fadeProgress < fadeOutTime) {
                float t = fadeProgress / fadeOutTime;
                audioSource.volume = Mathf.Lerp(targetVolume, 0, t);
            } else {
                Stop();
            }
            return;
        }
        // FadeIn
        if (fadeProgress < request.fadeInTime) {
            float t = fadeProgress / request.fadeInTime;
            audioSource.volume = Mathf.Lerp(0, targetVolume, t);
        } else {
            audioSource.volume = targetVolume;
            fadeStatus = FadeStatus.Stopped;
        }
    }

    /// <summary>
    /// 启动新片段
    /// </summary>
    private void StartPlay() {
        status = Status.Playing;
        audioSource.clip = handle.GetAsset<AudioClip>();
        audioSource.loop = request.loop;
        audioSource.time = 0;
        audioSource.Play();
        // 淡入 -- 感觉初始音量不为0更好？另外，加载的时间是否算在淡入时间中？
        if (request.fadeInTime > 0) {
            fadeStatus = FadeStatus.FadeIn;
            fadeProgress = 0;
            audioSource.volume = 0;
        } else {
            fadeStatus = FadeStatus.Stopped;
            fadeProgress = 0;
        }
    }

    /// <summary>
    /// 执行淡出
    /// </summary>
    /// <param name="fadeTime">淡出时间</param>
    /// <returns>是否进入淡出状态</returns>
    public bool FadeOut(float fadeTime) {
        if (status == Status.Playing && fadeTime > 0) {
            fadeStatus = FadeStatus.FadeOut;
            fadeOutTime = fadeTime;
            fadeProgress = 0;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 停止播放
    /// </summary>
    public void Stop() {
        if (status == Status.Playing) {
            audioSource.Stop();
            audioSource.clip = null;
        }
        status = Status.Stopped;
        fadeStatus = FadeStatus.Stopped;
        handle.Release();
        handle = default;
        request = default;
    }

    private enum Status
    {
        Stopped, // 空闲
        Loading, // 加载中
        Ready, // 已就绪（等待前一个片段淡出）
        Playing, // 正在播放状态
    }

    private enum FadeStatus
    {
        Stopped = 0,
        FadeIn = 1,
        FadeOut = 2,
    }
}
}