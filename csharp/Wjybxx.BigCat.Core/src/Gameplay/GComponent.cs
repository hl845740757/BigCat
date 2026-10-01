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
/// 3.未使用<see cref="ComponentDefineAttribute"/>的情况下，默认为数据组件。
/// 4.避免在组件中定义任何与具体业务挂钩的方法，仅可定义数据结构的维护方法。
/// </summary>
public abstract class GComponent
{
    /// <summary>
    /// 组件id池
    /// </summary>
    public static readonly ComponentIdPool ID_POOL = ComponentIdPool.NewPool();
    public static readonly ComponentIdPool.Interceptor ID_INTERCEPTOR = (builder, attribute) => {
        if (attribute == null) builder.Kind = ComponentKind.Data;
    };
#nullable disable
    [NonSerialized] private GameUnit _gameUnit;
    [NonSerialized] private ComponentId _cid;
    private bool _enabled = true; // 启用状态，需要持久化
#nullable restore

    protected GComponent() {
    }

    #region internal

    /// <summary>
    /// 绑定实体
    /// </summary>
    internal void SetEntity(GameUnit gameUnit) {
        this._gameUnit = gameUnit;
    }

    #endregion

#nullable disable

    #region Props

    public ComponentId Cid {
        get => _cid ??= ID_POOL.ValueOf(GetType(), ID_INTERCEPTOR);
        set {
            if (_cid != null) {
                throw new InvalidOperationException();
            }
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

    public GameUnit GameUnit => _gameUnit;

    #endregion

    #region 接口行为

    /// <summary>
    /// 重置组件状态
    ///
    /// 注；将组件重置为内存初始状态（正常构造的空实体），后续复用实体时需重新填充数据。
    /// </summary>
    public virtual void Reset() {
        _enabled = true;
    }

    #endregion
}

internal class GComponentListHelper : IComponentListHelper<GComponent>
{
    public static readonly GComponentListHelper Inst = new GComponentListHelper();

    public ComponentId GetCid(GComponent element) {
        return element.Cid;
    }

    public GComponent? GetNext(GComponent element) {
        return null;
    }

    public void SetNext(GComponent element, GComponent? next) {
        throw new NotSupportedException();
    }
}
}