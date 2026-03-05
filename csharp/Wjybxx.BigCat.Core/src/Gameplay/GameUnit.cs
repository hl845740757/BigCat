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
using System.Runtime.CompilerServices;
using Wjybxx.BigCat.Util;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Fx;
using Wjybxx.Commons.Logger;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Codec.Attributes;

#if UNITY_2021_3_OR_NEWER
using UnityEngine;
using ILogger = Wjybxx.Commons.Logger.ILogger;
#endif

namespace Wjybxx.BigCat.Gameplay
{
/// <summary>
/// 游戏单位
///
/// <h3>组件管理</h3>
/// 1.面向过程：GameUnit对象上只包含数据组件，不包含行为组件。
/// 2.反序列化创建实例：GameUnit组件在编辑器中配置。
/// 3.GameUnit在初始化完成后禁止增删组件，只有在New状态下可增删组件。
/// 4.GameUnit暂时限制最多128类组件。
/// 5.数据组件不支持重复，需要提前将元素转换为List类型。
///
/// <h3>对象重用</h3>
/// 理论上讲，按Component复用对内存更好，但我们还是选择以GameUnit为单位复用。有几个原因：
/// 1.游戏对象主要通过反序列化创建，因此按组件复用的话，数据的初始化管理会比较麻烦 —— 需要从模板拷贝。
/// 2.以GameUnit为单位复用会更加安全，也会更容易扩展。
/// 3.重用对象时需要更改<see cref="InstId"/>，使得旧id引用无法从GameUnitMgr查询到对象。
/// 4.完全由手工创建的对象，如子弹，可以进行单独的池化管理。
///
/// <h3>资源管理</h3>
/// 由于该程序集不能依赖下游的资源管理程序集，因此需要通过额外组件来寄存所有需要跟随GameUnit销毁的资源句柄。
/// 
/// 注：
/// 1.为避免和引擎的GameObject命名冲突，我们命名为GameUnit。
/// 2.没有实现<see cref="IEntity"/>接口，因为没必要。
/// </summary>
[DsonSerializable(SkipFields = new[] { "*" })]
public sealed class GameUnit
{
    private static readonly ILogger logger = LoggerFactory.GetLogger<GameUnit>();
#nullable disable
    /// <summary>
    /// 配置表id
    /// </summary>
    [NonSerialized] private int _configId;
    /// <summary>
    /// 实例id
    /// </summary>
    [NonSerialized] private long _instId;
    /// <summary>
    /// 对象的状态
    /// 注：默认情况下，游戏对象只包含<see cref="ComponentStatus.New"/>、<see cref="ComponentStatus.Initialized"/>
    /// 和<see cref="ComponentStatus.Destroyed"/>三个状态，因为游戏对象是数据对象。
    /// </summary>
    [NonSerialized] private ComponentStatus _status = ComponentStatus.New;
    /// <summary>
    /// 激活状态，需要持久化
    /// </summary>
    private bool _active = true;

    /// <summary>
    /// 所在的场景
    /// </summary>
    [NonSerialized] private Scene _scene;
    /// <summary>
    /// 游戏对象的内部代理
    /// 注意：
    /// 1.这里的Agent与游戏对象的生命周期并不绑定，存储在这里仅仅是为了避免频繁查询。
    /// 2.建议使用默认对象代替Null。
    /// </summary>
    [NonSerialized] private GameUnitAgent _agent;
    /// <summary>
    /// 用户自定义数据
    /// </summary>
    [NonSerialized] private object _userData;

    /// <summary>
    /// GameUnit上的组件
    /// </summary>
    private readonly List<GComponent> _components = new List<GComponent>();
    /// <summary>
    /// 索引后的组件
    /// </summary>
    [NonSerialized] private readonly ComponentList<GComponent?> _indexedComponents = new(GComponentListHelper.Inst);

    /// <summary>
    /// 对象在各缓存列表的索引
    /// </summary>
    [NonSerialized] internal GIndexes indexes = GIndexes.Create();
    /// <summary>
    /// 在视野格子中的缓存索引(预设字段)
    /// </summary>
    [NonSerialized] public GIndexes aoiIndexes = GIndexes.Create();
#nullable restore

    public GameUnit() {

    }

    public GameUnit(IDsonObjectReader reader) {
        _active = reader.ReadBool("active");
        // 组件列表
        List<GComponent> components = reader.ReadObject<List<GComponent>>("components");
        _components.EnsureCapacity(components.Count);
        _indexedComponents.EnsureCapacity(components.Count);
        foreach (GComponent component in components) {
            _components.Add(component);
            _indexedComponents.Add(component);
        }
    }

    public void WriteObject(IDsonObjectWriter writer) {
        writer.WriteBool("active", _active);
        writer.WriteObject("components", _components);
    }

    #region props

    public int ConfigId {
        get => _configId;
        set => _configId = value;
    }
    public long InstId {
        get => _instId;
        set => _instId = value;
    }

    public ComponentStatus Status => _status;

    public Scene Scene {
        get => _scene;
        set => _scene = value;
    }
    public GameUnitAgent Agent {
        get => _agent;
        set => _agent = value;
    }
    public object UserData {
        get => _userData;
        set => _userData = value;
    }

    /// <summary>
    /// 游戏单位在列表中的下标
    /// (通常用于外部数据对齐)
    /// </summary>
    public int Index => indexes.v0;

    /// <summary>
    /// 是否处于激活状态
    /// </summary>
    public bool IsActive {
        get => _active;
        set => _active = value;
    }

    #endregion

    #region 生命周期

    /// <summary>
    /// 将游戏对象标记为已初始化完成
    /// 注：应当在加入场景前调用。
    /// </summary>
    public void SetInitialized() {
        if (_status == ComponentStatus.Destroyed) {
            throw new InvalidOperationException("already destroyed");
        }
        _status = ComponentStatus.Initialized;
        // 初始化模块
        foreach (GComponent component in _components) {
            if (component.Cid.shared) {
                continue;
            }
            if (component.Status == ComponentStatus.New) {
                component.SetEntity(this);
            }
        }
        // 解决模块之间的依赖
        foreach (GComponent component in _components) {
            if (component.Cid.shared) {
                continue;
            }
            component.ResolveDependence();
        }
    }

    /// <summary>
    /// 重置对象
    /// 注：建议调用该方法后更新instId
    /// </summary>
    public void Reset() {
        if (_status == ComponentStatus.Destroyed) {
            throw new InvalidOperationException("already destroyed");
        }
        foreach (GComponent component in _components) {
            if (component.Cid.shared) continue;
            if (component.Status == ComponentStatus.New) continue;
            component.Reset();
        }
        indexes.Clear();
        aoiIndexes.Clear();

        if (_status > ComponentStatus.Initialized) {
            _status = ComponentStatus.Initialized;
        }
        _active = true;
        // 清理init后产生的数据
        _scene = null;
        _agent = null;
        _userData = null;
    }

    /// <summary>
    /// 销毁对象
    /// 注：Unity对象需要手动销毁。
    /// </summary>
    public void Destroy() {
        if (_status == ComponentStatus.Destroyed) {
            return;
        }
        _status = ComponentStatus.Destroyed;
        foreach (GComponent component in _components) {
            if (component.Cid.shared) continue;
            if (component.Status == ComponentStatus.New) continue;
            try {
                component.InvokeDestroy();
            }
            catch (Exception ex) {
                logger.Warn(ex, "component.Destroy caught exception");
            }
        }
        _scene = null;
        _agent = null;
        _userData = null;
        _components.Clear();
        _indexedComponents.Clear();
    }

    #endregion

#nullable disable

    #region 组件模式

    /// <summary>
    /// 添加组件
    /// </summary>
    /// <param name="comp">套添加的组件</param>
    /// <param name="addFirst">是否添加到首部，通常用于插入基础组件</param>
    public void AddComponent(GComponent comp, bool addFirst = false) {
        if (_status != ComponentStatus.New) throw new InvalidOperationException();
        if (_indexedComponents.Count(comp.Cid) >= comp.Cid.maxCount) {
            throw new InvalidOperationException($"countLimit: {comp.Cid.maxCount}");
        }
        if (addFirst) {
            _components.Insert(0, comp);
        } else {
            _components.Add(comp);
        }
        _indexedComponents.Add(comp, addFirst);
    }

    /// <summary>
    /// 是否包含目标组件
    /// </summary>
    public bool ContainsComponent(GComponent comp) {
        return _indexedComponents.Contains(comp);
    }

    /// <summary>
    /// 当前组件数量
    /// </summary>
    public int ComponentsCount => _components.Count;

    /// <summary>
    /// 组件的掩码，用户快速测试游戏单位包含的组件
    /// </summary>
    public GBitSet ComponentsMask => _indexedComponents.Mask;

    /// <summary>
    /// 获取原始的组件List
    /// 注：不可直接修改List
    /// </summary>
    public List<GComponent> Components => _components;

    // 泛型
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetComponent<T>(ComponentId<T> cid) where T : class {
        return _indexedComponents.Get(cid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetLastComponent<T>(ComponentId<T> cid) where T : class {
        return _indexedComponents.GetLast(cid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<T> GetComponents<T>(ComponentId<T> cid, List<T>? outList = null) where T : class {
        outList ??= new List<T>();
        _indexedComponents.Get(cid, outList);
        return outList;
    }

    // 非泛型
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CountComponent(ComponentId cid) {
        return _indexedComponents.Count(cid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GComponent? GetComponent(ComponentId cid) {
        return _indexedComponents.Get(cid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GComponent? GetLastComponent(ComponentId cid) {
        return _indexedComponents.GetLast(cid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<GComponent> GetComponents(ComponentId cid, List<GComponent>? outList = null) {
        outList ??= new List<GComponent>();
        _indexedComponents.Get(cid, outList);
        return outList;
    }

    #endregion

#nullable restore

    #region equals

    public override bool Equals(object obj) {
        return ReferenceEquals(this, obj);
    }

    public override int GetHashCode() {
        return (_configId.GetHashCode() * 397) ^ _instId.GetHashCode();
    }

    public override string ToString() {
        return $"GameUnit-{_configId}-{_instId}";
    }

    #endregion
}
}