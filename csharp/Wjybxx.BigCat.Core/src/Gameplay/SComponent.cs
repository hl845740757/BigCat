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
using Wjybxx.Commons.Fx;

namespace Wjybxx.BigCat.Gameplay
{
/// <summary>
/// <see cref="Scene"/>的组件
///
/// <h3>接口说明</h3>
/// 1.只有为<see cref="ComponentKind.Script"/>类型时才会被场景循环特殊调度，
/// 否则只调用<see cref="OnAwake"/>、<see cref="OnDestroy"/>方法。
/// 2.执行顺序为
/// <see cref="OnAwake"/>、<see cref="ResolveDependence"/>
/// <see cref="Start"/>、
/// <see cref="FixedUpdate"/>、<see cref="EarlyUpdate"/>、<see cref="Update"/>、<see cref="LateUpdate"/>
/// <see cref="Stop"/>、
/// <see cref="OnDestroy"/>。
/// </summary>
public abstract class SComponent
{
    /// <summary>
    /// 组件id池
    /// </summary>
    public static readonly ComponentIdPool ID_POOL = ComponentIdPool.NewPool();
#nullable disable
    [NonSerialized] private Scene _scene;
    [NonSerialized] private ComponentId _cid;
    [NonSerialized] private ComponentStatus _status = ComponentStatus.New;

    [NonSerialized] private SComponent _next; // 索引用，避免为每个组件创建一个List
    [NonSerialized] internal GIndexes indexes = GIndexes.Create(); // 索引缓存
#nullable restore

    protected SComponent() {
    }

    #region internal

    internal SComponent? Next {
        get => _next;
        set => _next = value;
    }

    internal void SetEntity(Scene scene) {
        if (this._status != ComponentStatus.New) {
            throw new InvalidOperationException("already bind");
        }
        this._scene = scene ?? throw new ArgumentNullException(nameof(scene));
        this._status = ComponentStatus.Initialized;
        this.OnAwake();
    }

    internal void InvokeDestroy() {
        _status = ComponentStatus.Destroyed;
        try {
            OnDestroy();
        }
        finally {
            _scene = null;
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

#nullable disable

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

    public Scene Scene => _scene;
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

    public virtual void FixedUpdate() {
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
    /// 1.清理运行过程中产生的临时数据，以支持重新启动；Reset后会重新start，但不会再执行onAwake。
    /// 2.Scene并不推荐复用，因此可不实现该方法。
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

internal class SComponentListHelper : IComponentListHelper<SComponent>
{
    public static readonly SComponentListHelper Inst = new SComponentListHelper();

    public ComponentId GetCid(SComponent element) {
        return element.Cid;
    }

    public SComponent? GetNext(SComponent element) {
        return element.Next;
    }

    public void SetNext(SComponent element, SComponent? next) {
        element.Next = next;
    }
}
}