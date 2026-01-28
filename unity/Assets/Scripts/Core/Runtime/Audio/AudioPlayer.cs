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
using Wjybxx.BigCat.Assetor;
using Wjybxx.BigCat.Core;
using Wjybxx.Commons;

namespace Wjybxx.BigCat.Audio
{
/// <summary>
/// 音频播放器
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioPlayer : MonoBehaviour
{
    /// <summary>
    /// 音量设置
    /// </summary>
    public AudioPlayerSettings settings = new AudioPlayerSettings();

    /// <summary>
    /// 音源
    /// </summary>
    private AudioSource audioSource;
    /// <summary>
    /// 播放状态
    /// </summary>
    private Status status;
    /// <summary>
    /// 前一个片段的控制器
    /// </summary>
    private AudioClipCtrl prevClipCtrl;
    /// <summary>
    /// 当前片段的控制器
    /// </summary>
    private AudioClipCtrl curClipCtrl;

    /// <summary>
    /// 短音效资源句柄(长音频由ctrl管理)
    /// </summary>
    private readonly Dictionary<string, AssetHandle> assetHandles = new();
    /// <summary>
    /// 加载中播放请求
    /// </summary>
    private readonly Dictionary<uint, AudioRequest> requests = new();
    /// <summary>
    /// 播放中的短音效上下文
    /// </summary>
    private readonly List<ShotContext> shotContexts = new();
    /// <summary>
    /// 上次检测短音效回收的时间
    /// </summary>
    private float _lastCheckTime;

    /// <summary>
    /// 资源加载回调
    /// </summary>
    private Action<AssetHandle> _onLoadCompleted;
    /// <summary>
    /// 音频播放结束事件
    /// (类似于FSM的状态切换事件，用于bgm播放器)
    /// </summary>
    public Action<AudioPlayer> onStopped;

    #region 状态查询

    /// <summary>
    /// 是否处于播放状态
    /// 注意：这是播放器的状态，不代表当前Clip的状态。
    /// </summary>
    public bool IsPlaying => status == Status.Playing;
    public bool IsStopped => status == Status.Stopped;
    public bool IsPaused => status == Status.Paused;

    public float ActualVolume => audioSource.volume;
    public float ActualPitch => audioSource.pitch;
    public bool ActualMute => audioSource.mute;

    /// <summary>
    /// 当前（待）播放的音频地址
    /// </summary>
    public string AudioPath => curClipCtrl != null ? curClipCtrl.AudioPath : null;

    #endregion

    #region play

    /// <summary>
    /// 播放短促音效
    /// </summary>
    /// <param name="audioPath">音效路径</param>
    /// <param name="volume">音量</param>
    public void PlayOneShot(ObjectPath audioPath, float volume = 1f) {
        PlayOneShot(audioPath.collection, audioPath.localPath, volume);
    }

    /// <summary>
    /// 播放短促音效
    /// </summary>
    /// <param name="groupPath">音效组路径</param>
    /// <param name="clipName">音效文件名(不含扩展名)</param>
    /// <param name="volume">音量</param>
    public void PlayOneShot(string groupPath, string clipName, float volume = 1f) {
        if (string.IsNullOrEmpty(groupPath) || string.IsNullOrEmpty(clipName)) {
            return;
        }
        Play(new AudioRequest()
        {
            playMode = AudioPlayMode.PlayOneShot,
            audioPath = groupPath,
            clipName = clipName,
            volume = volume,
        });
    }

    /// <summary>
    /// 播放长音效(如bgm)
    /// </summary>
    /// <param name="audioPath">音频路径</param>
    /// <param name="clipName">音频名</param>
    /// <param name="loop">是否循环播放</param>
    public void PlayClip(string audioPath, string clipName, bool loop) {
        Play(new AudioRequest()
        {
            playMode = AudioPlayMode.PlayClip,
            audioPath = audioPath,
            clipName = clipName,
            loop = loop,
        });
    }

    /// <summary>
    /// 播放2D音效
    ///
    /// 注：在资源尚未加载完成的情况下，短音频的重复请求会被丢弃，以避免异常的播放效果。
    /// </summary>
    /// <param name="request">播放请求</param>
    public void Play(in AudioRequest request) {
        string audioPath = request.audioPath;
        if (string.IsNullOrEmpty(audioPath)) {
            return;
        }
        // 短音效 - 资源handle在心跳方法中释放
        AssetHandle handle;
        if (request.playMode != AudioPlayMode.PlayClip) {
            if (audioSource.mute || audioSource.volume == 0f) return;
            if (assetHandles.TryGetValue(audioPath, out handle)) {
                handle.Retain();
            } else {
                handle = ResourceManager.Inst.LoadAssetAsync<AudioGroup>(audioPath);
                assetHandles.Add(audioPath, handle);
            }
            if (handle.IsErrorHandle) {
                return;
            }
            if (handle.IsCompleted) {
                OnLoadCompleted(handle, in request);
            } else {
                requests[handle.HandleId] = request;
                handle.Completed += _onLoadCompleted;
            }
            return;
        }
        // 长音频 - 资源handle由Ctrl管理
        handle = ResourceManager.Inst.LoadAssetAsync<AudioClip>(audioPath);
        if (handle.IsErrorHandle) {
            return;
        }
        // 先停止更久远的播放请求
        if (prevClipCtrl != null) {
            prevClipCtrl.Stop();
            prevClipCtrl = null;
        }
        // 淡出或停止当前Clip播放
        if ((prevClipCtrl = curClipCtrl) != null) {
            curClipCtrl = null;
            if (!prevClipCtrl.FadeOut(request.fadeOutTime)) {
                prevClipCtrl.Stop();
                prevClipCtrl = null;
            }
        }
        curClipCtrl = new AudioClipCtrl(this, audioSource);
        curClipCtrl.Play(in request, handle);
        // 加载状态也进入播放状态，如果暂停状态则保持暂停
        if (status == Status.Stopped) {
            status = Status.Playing;
        }
        // 同步非实时变化设置
        audioSource.spatialBlend = settings.spatialBlend;
        audioSource.dopplerLevel = settings.dopplerLevel;
        audioSource.spread = settings.spread;
    }

    internal bool HasPrevClip => prevClipCtrl != null;

    private void OnLoadCompleted(AssetHandle handle) {
        if (requests.Remove(handle.HandleId, out AudioRequest request)) {
            OnLoadCompleted(handle, in request);
        }
    }

    private void OnLoadCompleted(AssetHandle handle, in AudioRequest request) {
        AudioGroup audioGroup = handle.GetAsset<AudioGroup>();
        AudioClip audioClip = audioGroup.GetAudioClip(request.clipName);
        if (!audioClip) {
            ReleaseHandle(handle);
            return;
        }
        audioSource.PlayOneShot(audioClip, audioSource.volume * request.volume);
        shotContexts.Add(new ShotContext(handle, audioClip.length + Time.time + 0.1f));
    }

    private void ReleaseHandle(AssetHandle handle) {
        handle.Release();
        if (handle.ReferenceCount == 0) {
            assetHandles.Remove(handle.Location);
        }
    }

    private void Update() {
        float tickTime = Time.time;
        if (tickTime - _lastCheckTime >= 0.1f) {
            _lastCheckTime = tickTime;
            CheckShotContexts(tickTime);
        }
        // 同步音量信息 - PlayOneShot也需要同步
        AudioPlayerSettings parentSettings = settings.parent;
        if (parentSettings != null) {
            settings.realMute = parentSettings.realMute || settings.mute;
            settings.realPitch = parentSettings.realPitch * settings.pitch;
            settings.realVolume = parentSettings.realVolume * settings.volume;
        } else {
            settings.realMute = settings.mute;
            settings.realPitch = settings.pitch;
            settings.realVolume = settings.volume;
        }
        audioSource.mute = settings.realMute;
        audioSource.pitch = settings.realPitch;
        float targetVolume = settings.realVolume;
        //
        if (status != Status.Playing) {
            audioSource.volume = targetVolume;
            return;
        }
        // 更新Clip
        float deltaTime = Time.deltaTime;
        if (prevClipCtrl != null) {
            prevClipCtrl.Update(deltaTime, targetVolume);
            if (!prevClipCtrl.IsStopped) {
                return;
            }
            prevClipCtrl = null;
            deltaTime = 0;
        }
        if (curClipCtrl != null) {
            curClipCtrl.Update(deltaTime, targetVolume);
            if (!curClipCtrl.IsStopped) {
                return;
            }
            curClipCtrl = null;
            onStopped?.Invoke(this);
        }
    }

    private void CheckShotContexts(float tickTime) {
        for (int idx = 0; idx < shotContexts.Count; idx++) {
            ShotContext context = shotContexts[idx];
            if (tickTime < context.deadline) {
                continue;
            }
            shotContexts.RemoveAt(idx--);
            ReleaseHandle(context.handle);
        }
    }

    /// <summary>
    /// 暂停播放
    /// </summary>
    public void Pause() {
        if (status == Status.Playing) {
            status = Status.Paused;
            audioSource.Pause();
        }
    }

    /// <summary>
    /// 恢复播放
    /// </summary>
    public void Resume() {
        if (status == Status.Paused) {
            status = Status.Playing;
            audioSource.UnPause();
        }
    }

    /// <summary>
    /// 停止播放
    ///
    /// 注：调用Stop会导致丢弃要淡入播放的音频。
    /// </summary>
    public void Stop() {
        if (prevClipCtrl != null) {
            prevClipCtrl.Stop();
            prevClipCtrl = null;
        }
        if (curClipCtrl != null) {
            curClipCtrl.Stop();
            curClipCtrl = null;
        }
        status = Status.Stopped;
    }

    #endregion

    #region 生命周期

    private void Awake() {
        settings ??= new AudioPlayerSettings();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        _onLoadCompleted = OnLoadCompleted;
    }

    /// <summary>
    /// 运行时重置数据
    /// </summary>
    public void Reset() {
        Stop();
        foreach (AssetHandle handle in assetHandles.Values) {
            handle.Release(handle.ReferenceCount);
        }
        assetHandles.Clear();
        shotContexts.Clear();
        requests.Clear();
        settings.parent = null;
    }

    #endregion

    private readonly struct ShotContext
    {
        public readonly AssetHandle handle; // 资源句柄
        public readonly float deadline; // 播放截止时间(仅适用短促音效)

        public ShotContext(AssetHandle handle, float deadline) {
            this.handle = handle;
            this.deadline = deadline;
        }
    }

    private enum Status
    {
        Stopped = 0,
        Playing = 1,
        Paused = 2,
    }
}
}