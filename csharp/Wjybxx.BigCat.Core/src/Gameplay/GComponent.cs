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
using Wjybxx.Commons;
using Wjybxx.Commons.Fx;

namespace Wjybxx.BigCat.Gameplay
{
/// <summary>
/// 游戏对象的组件
///
/// 注：
/// 1.不会在同一类<see cref="GameUnit"/>上出现的组件，其组件Id可以使用相同的index，以节省空间。
/// 2.重用组件对象时（进入新的生命周期时），建议更新对象的实例id。
/// </summary>
public abstract class GComponent
{
    /// <summary>
    /// 组件id池
    /// </summary>
    public static readonly ComponentIdPool ID_POOL = ComponentIdPool.NewPool();
#nullable disable
    [NonSerialized] private GameUnit _gameUnit;
    [NonSerialized] private ComponentId _cid;
    [NonSerialized] private ComponentStatus _status = ComponentStatus.New;
    private bool _enabled = true; // 启用状态，需要持久化

    [NonSerialized] private GComponent? _next; // 索引用，避免为每个组件创建一个List
    [NonSerialized] internal GIndexes indexes = GIndexes.Create();
#nullable restore

    protected GComponent() {
    }

    #region internal

    internal GComponent? Next {
        get => _next;
        set => _next = value;
    }

    /// <summary>
    /// 绑定实体
    /// </summary>
    internal void SetEntity(GameUnit gameUnit) {
        if (this._status != ComponentStatus.New) {
            throw new InvalidOperationException("already bind");
        }
        this._gameUnit = gameUnit ?? throw new ArgumentNullException(nameof(gameUnit));
        this._status = ComponentStatus.Initialized;
        this.OnAwake();
    }

    /// <summary>
    /// 销毁组件
    /// </summary>
    /// <returns></returns>
    internal void InvokeDestroy() {
        _status = ComponentStatus.Destroyed;
        try {
            OnDestroy();
        }
        finally {
            _gameUnit = null;
        }
    }

    #endregion

#nullable disable

    #region Props

    public ComponentId Cid {
        get => _cid ??= ID_POOL.ValueOf(GetType());
        set {
            CheckStatus();
            _cid = value;
        }
    }

    /// <summary>
    /// 组件的启用状态
    /// </summary>
    public bool Enabled {
        get => _enabled;
        set => _enabled = value;
    }

    private void CheckStatus() {
        if (_status != ComponentStatus.New) {
            throw new InvalidOperationException();
        }
    }

    public GameUnit GameUnit => _gameUnit;
    public ComponentStatus Status => _status;

    #endregion

    #region 接口行为

    /// <summary>
    /// 注意：
    /// 与Unity中的Awake不同，此时游戏对象可能尚未加入到场景 -- 取决于用户调用<see cref="GameUnit.SetInitialized"/>的时机；
    /// 因此该方法只可访问<see cref="GameUnit"/>自身的数据，不可访问外部（Scene）数据。
    /// </summary>
    protected virtual void OnAwake() {
    }

    /// <summary>
    /// 注：只有执行了<see cref="OnAwake"/>方法的情况下，才会执行该方法。
    /// </summary>
    protected virtual void OnDestroy() {
    }

    /// <summary>
    /// 注：如果遵循数据与行为分离架构，游戏对象通常不需要实现该方法。
    /// </summary>
    public virtual void ResolveDependence() {
    }

    /// <summary>
    /// 注：框架默认不会调度该方法，由用户扩展
    /// </summary>
    public virtual void OnEnable() {
    }

    /// <summary>
    /// 注：框架默认不会调度该方法，由用户扩展
    /// </summary>
    public virtual void OnDisable() {
    }

    /// <summary>
    /// 重置组件状态
    /// (清理运行过程中产生的临时数据，以支持重用对象 -- 跨场景重用)
    /// </summary>
    public virtual void Reset() {
        if (_status > ComponentStatus.Initialized) {
            _status = ComponentStatus.Initialized;
        }
        _enabled = true;
    }

    #endregion
}

internal class GComponentListHelper : ComponentListHelper<GComponent>
{
    public static readonly GComponentListHelper Inst = new GComponentListHelper();

    public ComponentId GetCid(GComponent element) {
        return element.Cid;
    }

    public GComponent? GetNext(GComponent element) {
        return element.Next;
    }

    public void SetNext(GComponent element, GComponent? next) {
        element.Next = next;
    }
}
}