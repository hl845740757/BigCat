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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Concurrent;
using Object = UnityEngine.Object;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 资源句柄
/// </summary>
public readonly struct AssetHandle : IEquatable<AssetHandle>
{
    private static uint _nextId; // 理论上int也足够，客户端程序通常不会长久运行

    /// <summary>
    /// handleId
    /// </summary>
    private readonly uint _handleId;
    /// <summary>
    /// 用户请求的资源地址
    /// </summary>
    private readonly string _location;
    /// <summary>
    /// 资源对象提供者
    /// </summary>
    private readonly Provider _provider;

    internal AssetHandle(string location, Provider provider) {
        _handleId = ++_nextId;
        _location = location;
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    private AssetHandle(string location) {
        this._handleId = 0;
        this._location = location;
        this._provider = null;
    }

    #region 类型测试

    /// <summary>
    /// 资源句柄
    /// 
    /// 注：如果handleId为0，表示Handle无效
    /// </summary>
    public uint HandleId => _handleId;
    /// <summary>
    /// 用户请求的资源坐标
    /// </summary>
    public string Location => _location;
    /// <summary>
    /// 关联的资产路径
    /// </summary>
    public string AssetPath => _provider.pid.assetPath;
    /// <summary>
    /// 关联的资产类型
    /// 注：如果是原始文件资产，参数为<see cref="BinaryAsset"/>。
    /// </summary>
    public Type AssetType => _provider.pid.assetType;

    public bool IsNullHandle => _handleId == 0;
    public bool IsErrorHandle => _provider is ErrorProvider;
    public bool IsAssetHandle => _provider is AssetProvider;
    public bool IsBinaryAssetHandle => _provider is BinaryAssetProvider;
    public bool IsSceneAssetHandle => _provider is SceneAssetProvider;
    public bool IsInstanceHandle => _provider is InstanceProvider;

    #endregion

    #region 加载状态

    /// <summary>
    /// 加载任务是否已完成
    /// </summary>
    public bool IsCompleted => _provider.IsCompleted;
    /// <summary>
    /// 加载是否成功
    /// </summary>
    public bool IsSucceeded => _provider.IsSucceeded;
    /// <summary>
    /// 是否加载失败
    /// 
    /// 1.只有意料之外的异常，才会返回失败；
    /// 2.如果资产文件存在，但资产类型不匹配，也返回成功 —— 因为空数组是合法返回值。
    /// </summary>
    public bool IsFailed => _provider.IsFailed;
    /// <summary>
    /// 是否被取消
    /// </summary>
    public bool IsCancelled => _provider.IsCancelled;

    /// <summary>
    /// 获取关联的主资产对象
    /// 
    /// 1.加载完成之前返回null。
    /// 2.如果资源类型不匹配，也返回null（Null更高效）。
    /// 3.如果任务执行失败，也返回null。
    /// </summary>
    public Object Asset {
        get {
            _provider.UpdateAccessTime();
            return (Object)_provider.promise.result;
        }
    }
    /// <summary>
    /// 获取关联的所有资产对象
    /// </summary>
    public IReadOnlyList<Object> AllAssets {
        get {
            _provider.UpdateAccessTime();
            return (IReadOnlyList<Object>)_provider.promise.result;
        }
    }

    /// <summary>
    /// 关联关联的二进制资产
    /// </summary>
    public BinaryAsset BinaryAsset {
        get {
            _provider.UpdateAccessTime();
            return (BinaryAsset)_provider.promise.result;
        }
    }
    /// <summary>
    /// 获取加载的所有二进制资产
    /// </summary>
    public IReadOnlyList<BinaryAsset> AllBinaryAssets {
        get {
            _provider.UpdateAccessTime();
            return (IReadOnlyList<BinaryAsset>)_provider.promise.result;
        }
    }

    /// <summary>
    /// 获取关联的主资产对象(或实例)
    /// (可以转object类型以测试是否为null)
    /// </summary>
    public T GetAsset<T>() {
        _provider.UpdateAccessTime();
        return (T)_provider.promise.result;
    }

    /// <summary>
    /// 任务错误码
    /// </summary>
    public ResourceErrorCode ErrorCode {
        get {
            _provider.UpdateAccessTime();
            return _provider.promise.errorCode;
        }
    }

    #endregion

    #region callback

    /// <summary>
    /// 加载任务的优先级
    /// </summary>
    public int Priority {
        get => _provider.Priority;
        set => _provider.Priority = value;
    }

    /// <summary>
    /// 当前任务进度
    /// </summary>
    public float Progress => _provider.promise.progress;

    /// <summary>
    /// await回调支持
    /// 
    /// 注：
    /// 1.如果任务已完成，await后的代码将立即（同步）执行。
    /// 2.用户应当在Await方法后通过Handle查询任务结果。
    /// 3.Awaiter只能感知任务是否完成，在任务失败的情况下不会抛出异常 —— 用户总是需要安全释放Handle。
    /// </summary>
    public Awaiter GetAwaiter() => new Awaiter(_provider ?? throw new InvalidOperationException());

    /// <summary>
    /// 注册加载完成回调
    ///
    /// 注：即使任务已完成，回调仍将被延迟到下一帧执行，即该形式的回调总是异步执行。
    /// </summary>
    public event Action<AssetHandle> Completed {
        add => _provider.RegisterCallback(value, this);
        remove => _provider.UnregisterCallback(value, this);
    }

    /// <summary>
    /// 阻塞等待异步任务完成
    /// </summary>
    /// <param name="timeout">超时时间，毫秒</param>
    /// <returns>this</returns>
    /// <exception cref="BlockingOperationException">如果当前不支持阻塞完成</exception>
    public AssetHandle WaitForCompletion(long timeout = 15 * 1000) {
        if (_provider.IsCompleted) {
            return this;
        }
        TaskScheduler scheduler = _provider.Scheduler;
        if (timeout <= 0) {
            scheduler.WaitForCompletion(_provider, 0);
        } else {
            long deadline = scheduler.RealTime + timeout;
            scheduler.WaitForCompletion(_provider, deadline);
        }
        return this;
    }

    #endregion

    #region 引用计数

    /// <summary>
    /// 当前引用计数
    ///
    /// 注：Handle的引用计数和资源的引用计数是分离的，用户不可以通过当前Handle释放其它Handle增加的引用计数。
    /// </summary>
    public int ReferenceCount => _provider.GetRefCount(this);

    /// <summary>
    /// 保持资源（增加引用计数）
    /// </summary>
    /// <param name="count"></param>
    public void Retain(int count = 1) {
        _provider.UpdateAccessTime();
        _provider.Retain(this, count);
    }

    /// <summary>
    /// 释放资源（减少引用计数）
    /// </summary>
    public void Release(int count = 1) {
        // 允许Null句柄安全调用
        _provider?.Release(this, count);
    }

    /// <summary>
    /// 更新资源的访问时间
    ///
    /// 注；任何访问资产对象的方法都会触发刷新。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateAccessTime() {
        _provider.UpdateAccessTime();
    }

    /// <summary>
    /// 获取资源对象的访问时间
    ///
    /// 注：该时间戳为资源管理器内部的调度时间戳，时间单位为毫秒。
    /// </summary>
    /// <returns></returns>
    public long GetAccessTime() {
        return _provider.TimeAccessed;
    }

    /// <summary>
    /// 获取标记值
    /// 
    /// 注：用户可用8Bit，其逻辑用用户自行约定。
    /// </summary>
    /// <param name="index">范围0~7</param>
    /// <returns></returns>
    public bool GetFlag(int index) {
        return _provider.GetFlag(index);
    }

    public void SetFlag(int index, bool value) {
        _provider.SetFlag(index, value);
    }

    #endregion

    #region 特殊handle

    /// <summary>
    /// 创建一个Null句柄，但可以指定额外信息
    /// </summary>
    public static AssetHandle CreateNullHandle(string location) {
        return new AssetHandle(location);
    }

    /// <summary>
    /// 创建一个实例句柄
    ///
    /// 1.该功能的作用是允许用户使用统一接口（AssetHandle）访问资源和资源实例。
    /// 2.但实例的生命周期由用户自行管理，框架不会管理实例对象。
    /// 3.框架会自动销毁引用计数为0的Provider以避免内存泄漏。
    /// 4.该封装的成本相对较高，如果不是必须统一访问接口，更推荐封装为自定义结构。
    /// </summary>
    /// <returns></returns>
    public AssetHandle CreateInstanceHandle(Object inst) {
        if (!inst) throw new ArgumentNullException(nameof(inst));
        if (_provider is not AssetProviderBase provider) {
            throw new InvalidOperationException();
        }
        ProviderId pid = new ProviderId(
            provider.pid.assetPath + "/" + inst.GetInstanceID(),
            provider.pid.assetType,
            ELoadMethod.InstHandle);
        //
        InstanceProvider instProvider = new InstanceProvider(provider.resourceMgr, pid, this, inst);
        _provider.Scheduler.WaitForCompletion(instProvider, 0); // 立即完成
        AssetHandle handle = new AssetHandle(_location, instProvider);
        handle.Retain();
        return handle;
    }

    /// <summary>
    /// 如果当前是实例对象句柄，则返回关联的资产对象Handle
    /// </summary>
    /// <returns></returns>
    public AssetHandle GetBackendHandle() {
        if (_provider is not InstanceProvider provider) {
            throw new InvalidOperationException();
        }
        return provider.backendHandle;
    }

    #endregion

    #region 相对资源路径加载

    /// <summary>
    /// 基于当前资源路径的相对路径加载资源
    /// </summary>
    /// <param name="relativePath">相对路径</param>
    /// <param name="priority">加载优先级</param>
    public AssetHandle LoadAssetAsync<T>(string relativePath, int priority = 0) where T : Object {
        string path = ResourceManager.CombinePath(AssetPath, relativePath);
        return _provider.resourceMgr.LoadAssetAsync<T>(path, priority);
    }

    /// <summary>
    /// 加载主资源
    /// </summary>
    public AssetHandle LoadAssetAsync(string relativePath, Type assetType, int priority = 0) {
        string path = ResourceManager.CombinePath(AssetPath, relativePath);
        return _provider.resourceMgr.LoadAssetAsync(path, assetType, priority);
    }

    /// <summary>
    /// 加载主资源和子资源
    /// </summary>
    public AssetHandle LoadAssetWithSubAssetsAsync<T>(string relativePath, int priority = 0) where T : Object {
        string path = ResourceManager.CombinePath(AssetPath, relativePath);
        return _provider.resourceMgr.LoadAssetWithSubAssetsAsync<T>(path, priority);
    }

    /// <summary>
    /// 加载主资源和子资源
    /// </summary>
    public AssetHandle LoadAssetWithSubAssetsAsync(string relativePath, Type assetType, int priority = 0) {
        string path = ResourceManager.CombinePath(AssetPath, relativePath);
        return _provider.resourceMgr.LoadAssetWithSubAssetsAsync(path, assetType, priority);
    }

    /// <summary>
    /// 加载Location所属Bundle的所有指定类型资产
    /// 注：通常用于加载自定义资产，
    /// </summary>
    public AssetHandle LoadAllAssetsAsync<T>(string relativePath, int priority = 0) where T : Object {
        string path = ResourceManager.CombinePath(AssetPath, relativePath);
        return _provider.resourceMgr.LoadAllAssetsAsync<T>(path, priority);
    }

    /// <summary>
    /// 加载Location所属Bundle的所有指定类型资产
    /// 注：通常用于加载自定义资产，
    /// </summary>
    public AssetHandle LoadAllAssetsAsync(string relativePath, Type assetType, int priority = 0) {
        string path = ResourceManager.CombinePath(AssetPath, relativePath);
        return _provider.resourceMgr.LoadAllAssetsAsync(path, assetType, priority);
    }

    /// <summary>
    /// 加载原始二进制资产
    /// </summary>
    public AssetHandle LoadBinaryAssetAsync(string relativePath, int priority = 0) {
        string path = ResourceManager.CombinePath(AssetPath, relativePath);
        return _provider.resourceMgr.LoadBinaryAssetAsync(path, priority);
    }

    /// <summary>
    /// 加载Location所属Bundle的所有指定类型资产
    /// 注：通常用于加载自定义资产，
    /// </summary>
    public AssetHandle LoadAllBinaryAssetAsync(string relativePath, int priority = 0) {
        string path = ResourceManager.CombinePath(AssetPath, relativePath);
        return _provider.resourceMgr.LoadAllBinaryAssetAsync(path, priority);
    }

    #endregion

    #region equals

    public override int GetHashCode() {
        return (int)_handleId;
    }

    public bool Equals(AssetHandle other) {
        return _handleId == other._handleId
               && ReferenceEquals(_provider, other._provider);
    }

    public override bool Equals(object obj) {
        return obj is AssetHandle other && Equals(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(AssetHandle left, AssetHandle right) {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(AssetHandle left, AssetHandle right) {
        return !left.Equals(right);
    }

    #endregion

    #region awaiter

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly Provider _task;

        internal Awaiter(Provider task) {
            _task = task;
        }

        // 1.IsCompleted
        // IsCompleted只在Start后调用一次，EventLoop可以通过接口查询是否已在线程中
        public bool IsCompleted => _task.IsCompleted;

        // 2. GetResult
        // 状态机只在IsCompleted为true时，和OnCompleted后调用GetResult，因此在目标线程中 -- 不可手动调用
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult() {
            Debug.Assert(_task.IsCompleted);
        }

        // 3. OnCompleted
        /// <summary>
        /// 添加一个Future完成时的回调。
        /// </summary>
        /// <param name="continuation">回调任务</param>
        public void OnCompleted(Action continuation) {
            if (continuation == null) throw new ArgumentNullException(nameof(continuation));
            _task.RegisterCallback(continuation);
        }

        public void UnsafeOnCompleted(Action continuation) {
            if (continuation == null) throw new ArgumentNullException(nameof(continuation));
            _task.RegisterCallback(continuation);
        }
    }

    #endregion
}
}