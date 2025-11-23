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
using System.Reflection;
using UnityEngine;

namespace Wjybxx.BigCat.UI
{
/// <summary>
/// 组件配置类
///
/// 注：
/// 1.为方便策划在编辑器中编辑Window，建议每个组件类型设计一个配置类。
/// 2.该配置类可以实现<see cref="WindowAgentHolder"/>。
/// </summary>
public class WComponentCfg : MonoBehaviour
{
    #region internal

    /// <summary>
    /// 组件的类型名
    ///
    /// 注：
    /// 1.格式：<code>typeName, assemblyName</code>
    /// 2.默认反射创建，若不能反射创建，请重写<see cref="CreateComponent"/>方法。
    /// </summary>
    public string compTypeName;
    /// <summary>
    /// 配置类关联的实例
    ///
    /// 注：Debug需要定制Editor/Inspector，或是将需要展示的信息附加在配置对象。
    /// </summary>
    [NonSerialized] private WComponent _comp;

    /// <summary>
    /// 获取配置关联的Node实例
    ///
    /// 注：参数主要用于Editor模式下获取null值。
    /// </summary>
    /// <returns></returns>
    /// <param name="createIfAbsent">组件不存在时是否创建</param>
    public WComponent GetComponent(bool createIfAbsent = true) {
        if (_comp == null && createIfAbsent) {
            _comp = CreateComponent();
            _comp.config = this;
        }
        return _comp;
    }

    protected virtual WComponent CreateComponent() {
        string typeName = compTypeName;
        if (string.IsNullOrWhiteSpace(typeName)) {
            throw new Exception("typeName is null or empty");
        }
        Type type = Type.GetType(compTypeName);
        if (type == null) {
            throw new Exception($"type {typeName} not found");
        }
        ConstructorInfo constructorInfo = type.GetConstructor(Array.Empty<Type>());
        if (constructorInfo == null) {
            throw new Exception($"type {typeName} must contain a public constructor");
        }
        return (WComponent)constructorInfo.Invoke(null);
    }

    #endregion
}
}