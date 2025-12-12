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
using Wjybxx.Commons;
using Wjybxx.Commons.Logger;
using ILogger = Wjybxx.Commons.Logger.ILogger;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 所有资产对象提供者的抽象接口
///
/// 注：
/// 1.同一个assetPath，只要加载结果相同，就指向同一个Provider。
/// 2.Provider整体采用延迟销毁方案，当不存在引用一段时间后才执行销毁。
/// 3.在已销毁的情况下收到底层异步加载完成事件时，应当放弃加载结果（自动销毁）。
/// </summary>
public abstract class Provider : ResourceTask
{
    protected static readonly ILogger logger = LoggerFactory.GetLogger(typeof(Provider));

    /// <summary>
    /// 关联的资源管理器
    /// </summary>
    public readonly ResourceManager resourceMgr;
    /// <summary>
    /// 唯一ID
    /// </summary>
    public readonly ProviderId pid;
    /// <summary>
    /// 引用计数为0的帧时间
    /// </summary>
    public long TimeReleased { get; internal set; }
    /// <summary>
    /// 资源的访问帧时间
    /// </summary>
    public long TimeAccessed { get; private set; }
    /// <summary>
    /// 引用计数
    /// </summary>
    public int RefCount { get; private set; }
    /// <summary>
    /// 用户回调
    /// </summary>
    private List<HandleCallback> handleCallbacks;

    protected Provider(ResourceManager resourceMgr, ProviderId pid) {
        this.resourceMgr = resourceMgr;
        this.pid = pid;
    }

    #region 引用计数

    /// <summary>
    /// 增加引用计数
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Retain(int count = 1) {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        ThrowIfDestroyed();
        if (RefCount == 0) {
            resourceMgr.RemoveFromIdles(this);
        }
        RefCount += count;
    }

    /// <summary>
    /// 减少引用计数
    /// </summary>
    public void Release(int count = 1) {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (count > RefCount) throw new ArgumentOutOfRangeException(nameof(count), $"refCount: {RefCount}, count: {count}");
        ThrowIfDestroyed();
        RefCount -= count;
        if (RefCount == 0) {
            resourceMgr.AddToIdles(this);
        }
    }

    /// <summary>
    /// 尚未释放的资源句柄id
    /// </summary>
    private static readonly Dictionary<uint, int> retainHandles = new(1000);

    /// <summary>
    /// 获取Handle自身的引用计数
    /// </summary>
    /// <param name="handle"></param>
    /// <returns></returns>
    public int GetRefCount(AssetHandle handle) {
        retainHandles.TryGetValue(handle.HandleId, out int count);
        return count;
    }

    /// <summary>
    /// 增加资源句柄
    /// </summary>
    public void Retain(AssetHandle handle, int count = 1) {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (count == 0) return;
        if (retainHandles.TryGetValue(handle.HandleId, out int prevCount)) {
            retainHandles[handle.HandleId] = prevCount + count;
        } else {
            retainHandles[handle.HandleId] = count;
            Retain();
        }
    }

    /// <summary>
    /// 释放资源句柄
    /// </summary>
    public void Release(AssetHandle handle, int count = 1) {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (count == 0) return;
        if (!retainHandles.TryGetValue(handle.HandleId, out int prevCount) || count > prevCount) {
            throw new ArgumentOutOfRangeException(nameof(count), $"prev: {prevCount}, count: {count}");
        }
        if (count < prevCount) {
            retainHandles[handle.HandleId] = prevCount - count;
        } else {
            retainHandles.Remove(handle.HandleId);
            Release();
        }
    }

    /// <summary>
    /// 更新Provider的访问时间
    ///
    /// 注：该方法不传播到关联的资产（如Bundle），以减少开销。
    /// </summary>
    public void UpdateAccessTime() {
        ThrowIfDestroyed();
        //
        TaskScheduler scheduler;
        if ((scheduler = Scheduler) != null) {
            TimeAccessed = scheduler.FrameTime;
        }
    }

    /// <summary>
    /// 当前是否可销毁
    ///
    /// 注：基础条件由父类统一处理，子类通常只需要检查回调和依赖的任务是否已完成。
    /// </summary>
    /// <returns></returns>
    public virtual bool CanDestroy() {
        return IsCompleted && RefCount == 0 && !HasCallbacks;
    }

    #endregion

    #region callback

    /// <summary>
    /// 当前是否有回调任务
    /// </summary>
    public bool HasCallbacks => handleCallbacks != null && handleCallbacks.Count > 0;

    public override void NotifyListeners() {
        List<HandleCallback> callbacks = handleCallbacks;
        if (callbacks == null || callbacks.Count == 0) return;
        // 当前迭代过程中新增的回调立即通知没有问题，因为也是延迟执行的
        IsNotifying = true;
        for (int i = 0; i < callbacks.Count; i++) {
            HandleCallback wrapper = callbacks[i];
            if (!wrapper.HasCallback) continue;
            try {
                wrapper.callback(wrapper.handle);
            }
            catch (Exception ex) {
                logger.Warn(ex);
            }
        }
        callbacks.Clear();
        IsNotifying = false;
    }

    public void RegisterHandleCallback(AssetHandle handle, Action<AssetHandle> callback) {
        ThrowIfDestroyed();
        handleCallbacks ??= new List<HandleCallback>(4);
        handleCallbacks.Add(new HandleCallback(handle, callback));
        if (IsCompleted && handleCallbacks.Count == 1) {
            Scheduler.DelayNotifyListener(this);
        }
    }

    public void UnregisterHandleCallback(AssetHandle handle, Action<AssetHandle> callback) {
        if (handleCallbacks == null) return;
        HandleCallback wrapper = new HandleCallback(handle, callback);
        int index = handleCallbacks.IndexOf(wrapper);
        if (IsNotifying) {
            handleCallbacks[index] = default;
        } else {
            handleCallbacks.RemoveAt(index);
        }
    }

    private bool IsNotifying {
        get => (flags & MASK_NOTIFYING) != 0;
        set => flags = BitFlags.Set(flags, MASK_NOTIFYING, value);
    }

    private readonly struct HandleCallback : IEquatable<HandleCallback>
    {
        public readonly AssetHandle handle;
        public readonly Action<AssetHandle> callback;

        public HandleCallback(AssetHandle handle, Action<AssetHandle> callback) {
            this.handle = handle;
            this.callback = callback;
        }

        public bool HasCallback => callback != null;

        public bool Equals(HandleCallback other) {
            return handle.Equals(other.handle) && Equals(callback, other.callback);
        }

        public override bool Equals(object obj) {
            return obj is HandleCallback other && Equals(other);
        }

        public override int GetHashCode() {
            return (handle.GetHashCode() * 397) ^ (callback != null ? callback.GetHashCode() : 0);
        }
    }

    #endregion
}
}