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
using Wjybxx.BigCat.Gameplay;
using Wjybxx.Commons.Fx;

namespace Wjybxx.BigCat.UI
{
/// <summary>
/// <see cref="Window"/>的组件。
///
/// 注意：UI相关的对象都需要实现<see cref="Reset"/>，因为Window对象需要复用 -- 需要支持以新数据打开界面。
/// </summary>
public abstract class WComponent
{
    /// <summary>
    /// 组件id池
    /// </summary>
    public static readonly ComponentIdPool ID_POOL = ComponentIdPool.NewPool();
#nullable disable
    /// <summary>
    /// 关联的配置对象
    /// 
    /// 注：手动创建的组件可能没有配置对象。
    /// </summary>
    [NonSerialized] public WComponentCfg config;
    [NonSerialized] private Window _window;
    [NonSerialized] private ComponentId _cid;
    [NonSerialized] private ComponentStatus _status = ComponentStatus.New;

    [NonSerialized] private WComponent _next; // 索引用，避免为每个组件创建一个List
    [NonSerialized] internal WIndexes indexes = WIndexes.Create();
#nullable restore

    #region internal

    internal WComponent? Next {
        get => _next;
        set => _next = value;
    }

    internal void SetEntity(Window window) {
        if (this._status != ComponentStatus.New) {
            throw new InvalidOperationException("already bind");
        }
        this._window = window ?? throw new ArgumentNullException(nameof(window));
        this._status = ComponentStatus.Initialized;
        this.OnAwake();
    }

    internal void InvokeDestroy() {
        _status = ComponentStatus.Destroyed;
        try {
            OnDestroy();
        }
        finally {
            _window = null;
        }
    }

    internal void InvokeStart() {
        _status = ComponentStatus.Running;
        Start();
    }

    /** 调用{@link #stop()}方法 */
    internal void InvokeStop() {
        _status = ComponentStatus.Shutdown;
        try {
            Stop();
        }
        finally {
            _status = ComponentStatus.Terminated;
        }
    }

    #endregion

    #region Props

    public ComponentId Cid {
        get => _cid ??= ID_POOL.ValueOf(GetType());
        set {
            if (_status != ComponentStatus.New) {
                throw new InvalidOperationException();
            }
            _cid = value;
        }
    }

    public Window Window => _window;
    public ComponentStatus Status => _status;

    #endregion

    #region 接口行为

    protected virtual void OnAwake() {
    }

    protected virtual void OnDestroy() {
    }

    public virtual void ResolveDependence() {
    }

    protected virtual void Start() {
    }

    public virtual void EarlyUpdate() {
    }

    public virtual void Update() {
    }

    public virtual void LateUpdate() {
    }

    protected virtual void Stop() {
    }

    /// <summary>
    /// 重置数据
    ///
    /// 1.清理运行过程中产生的临时数据，以支持重新启动。
    /// 2.Window相关组件必须实现重置方法，以支持复用
    /// </summary>
    public virtual void Reset() {
        if (_status > ComponentStatus.Initialized) {
            _status = ComponentStatus.Initialized;
        }
    }

    /// <summary>
    /// 自定义事件支持
    /// </summary>
    public virtual void OnEvent(object eventData) {

    }

    #endregion
}

internal class WComponentListHelper : ComponentListHelper<WComponent>
{
    public static readonly WComponentListHelper Inst = new WComponentListHelper();

    public ComponentId GetCid(WComponent element) {
        return element.Cid;
    }

    public WComponent? GetNext(WComponent element) {
        return element.Next;
    }

    public void SetNext(WComponent element, WComponent? next) {
        element.Next = next;
    }
}
}