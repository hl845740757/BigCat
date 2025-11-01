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
using UnityEngine;

namespace Wjybxx.BigCat.UI
{
/// <summary>
/// MVC中的Controller
///
/// 1.Controller的生命周期跟随Node。
/// 2.不可定义<see cref="MonoBehaviour"/>中的Update方法，Controller主要用于处理事件，不应当包含Update逻辑。
/// 3.不可使用<see cref="MonoBehaviour"/>中的协程方法，如果需要协程功能，请通过<see cref="Window"/>的组件实现。
///
/// TODO：是否提供一个接口类型？目前是觉得在编辑器中指定Controller更灵活。
/// </summary>
public abstract class Controller : MonoBehaviour
{
    /// <summary>
    /// 关联的Node的名字
    ///
    /// 1.配置项，运行时不可修改
    /// 2.如果指定了名字，则只会匹配指定name的Node
    /// </summary>
    public string nodeName;
    /// <summary>
    /// 关联的UI视图
    ///
    /// 注意：UI视图一旦绑定，便不会再改变。
    /// </summary>
    [NonSerialized] protected UINode node;

    /// <summary>
    /// 执行初始化
    ///
    /// 1.该方法在Node激活后，展示前调用。
    /// 2.Controller的初始化不可以依赖Node的数据。
    /// </summary>
    public virtual void Init(UINode node) {
        this.node = node;
    }

    /// <summary>
    /// Node显示时调用
    /// </summary>
    /// <param name="firstShow"></param>
    public virtual void OnShow(bool firstShow) {
    }

    /// <summary>
    /// Node隐藏时调用
    /// (用于清理数据，关闭资源)
    /// </summary>
    public virtual void OnHide() {
    }
}
}