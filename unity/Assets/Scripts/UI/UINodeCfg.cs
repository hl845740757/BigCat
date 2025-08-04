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

namespace Wjybxx.BigCat.UI
{
/// <summary>
/// 视图配置
/// </summary>
[Serializable]
public class UINodeCfg
{
    /// <summary>
    /// 视图脚本的名字
    /// </summary>
    [Tooltip("如果指定了name，则controller也必须指定name")]
    public string name;
    /// <summary>
    /// 创建新黑板(切割上下文)
    /// </summary>
    [Tooltip("当前Controller是否和父节点无关")]
    public bool newBlackboard;

    /// <summary>
    /// 数据地址
    ///
    /// 0.空字符串表示沿用父节点数据
    /// 1.斜杠开头表绝对路径：<code>/view/login/xxx</code>
    /// 2.非斜杠开头表父节点数据的相对路径:<code>login/xxx</code>
    /// </summary>
    public string dataAddress;
    /// <summary>
    /// Node展示模式
    /// 
    /// 1.第一个为默认模式。
    /// 2.为方便编辑，真实数据存储Node对象上，这里是缓存。
    /// </summary>
    [NonSerialized] public List<UINodeDisplayCfg> displayCfgs;

    #region 工具方法

    public UINodeDisplayCfg FindDisplayCfg(int mode) {
        return UIInternal.FindDisplayCfg(displayCfgs, mode);
    }

    #endregion
}

/// <summary>
/// 视图展示模式配置
///
/// TODO 数据模型绑定配置？其实对于游戏开发而言，数据绑定不那么必要。
/// </summary>
[Serializable]
public class UINodeDisplayCfg
{
    /// <summary>
    /// 展示模式，0是合法值
    /// </summary>
    public int mode = 0;
    /// <summary>
    /// 当前Node操作的GameObject
    ///
    /// 注：GameObject的名字是有意义的，Node根据Name查找GameObject。
    /// </summary>
    public List<GameObject> elements = new List<GameObject>();
    /// <summary>
    /// 当前Node的子节点
    /// </summary>
    public List<UINode> children = new List<UINode>();

    /// <summary>
    /// 查找要操作的GameObject
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public GameObject FindElement(string name) {
        return UIInternal.FindElement(elements, name);
    }

    /// <summary>
    /// 查找子视图。
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public UINode FindChild(string name) {
        return UIInternal.FindNode(children, name);
    }
}
}