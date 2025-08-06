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
    /// 注：一般View和Controller独占一个GameObject。
    /// </summary>
    [Tooltip("如果指定了name，则controller也必须指定name")]
    public string name;
    /// <summary>
    /// 创建新黑板(切割上下文)
    /// </summary>
    [Tooltip("是否需要切割黑板，当Controller数据和父节点存在冲突时勾选")]
    public bool newBlackboard;

    /// <summary>
    /// 数据地址
    ///
    /// 0.空字符串表示沿用父节点数据
    /// 1.斜杠开头表绝对路径：<code>/view/login/xxx</code>
    /// 2.非斜杠开头表父节点数据的相对路径:<code>login/xxx</code>
    /// </summary>
    [Tooltip("View关联的数据模型地址，规则请查看文档")]
    public string dataAddress;
    /// <summary>
    /// 自定义flags
    ///
    /// 注：建议高16位用于运行时程序控制，低16位用于编辑器静态配置。
    /// </summary>
    public int flags;

    /// <summary>
    /// 默认展示模式配置
    /// 
    /// 注：此为缓存字段，为方便策划配置，持久化数据在<see cref="UINode"/>上。
    /// </summary>
    [NonSerialized] public UINodeDisplayCfg defaultDisplayCfg;
    /// <summary>
    /// 更多展示模式配置
    /// 
    /// 注：此为缓存字段，为方便策划配置，持久化数据在<see cref="UINode"/>上。
    /// </summary>
    [NonSerialized] public List<UINodeDisplayCfg> moreDisplayCfgs;

    #region 工具方法

    /// <summary>
    /// 查找指定显示模式的配置
    /// </summary>
    /// <returns>如果不存在匹配的配置，则返回null</returns>
    public UINodeDisplayCfg FindDisplayCfg(int mode) {
        if (defaultDisplayCfg != null && defaultDisplayCfg.mode == mode) {
            return defaultDisplayCfg;
        }
        foreach (UINodeDisplayCfg displayCfg in moreDisplayCfgs) {
            if (displayCfg.mode == mode) return displayCfg;
        }
        return null;
    }

    #endregion
}

/// <summary>
/// 视图展示模式配置
///
/// TODO 数据模型绑定配置？其实对于游戏开发而言，自动数据绑定不那么必要。
/// </summary>
[Serializable]
public class UINodeDisplayCfg
{
    /// <summary>
    /// 展示模式，0是合法值
    /// </summary>
    [Tooltip("展示模式，0表示默认模式")]
    public int mode = 0;
    /// <summary>
    /// 当前Node操作的GameObject
    ///
    /// 注：GameObject的名字是有意义的，Node根据Name查找GameObject。
    /// </summary>
    [Tooltip("当前Node直接控制的文本和按钮等")]
    public List<GameObject> elements = new List<GameObject>();
    /// <summary>
    /// 当前Node的钩子节点
    ///
    /// 注：钩子节点可以重名，即表示将List类型的元素平铺展开。
    /// </summary>
    [Tooltip("钩子是需要当前Node特殊控制的节点")]
    public List<UINode> hooks = new List<UINode>();
    /// <summary>
    /// 当前Node的子节点
    /// </summary>
    [Tooltip("子节点之间通常应当等价，父节点不需要区分它们；如果你需要为子节点分配特殊的名字，通常应该实现为钩子节点")]
    public List<UINode> children = new List<UINode>();

    /// <summary>
    /// child模板
    /// </summary>
    [Tooltip("子节点模板，用于父节点动态创建子节点的情况，如ListView")]
    public UINode templateChild;

    #region util

    /// <summary>
    /// 查找要操作的GameObject
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public GameObject FindElement(string name) {
        return UIInternal.FindElement(elements, name);
    }

    /// <summary>
    /// 查找子视图
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public UINode FindChild(string name) {
        return UIInternal.FindNode(children, name);
    }

    /// <summary>
    /// 查找钩子节点
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public UINode FindHook(string name) {
        return UIInternal.FindNode(hooks, name);
    }

    /// <summary>
    /// 查询指定name的所有钩子节点
    /// </summary>
    /// <param name="name"></param>
    /// <param name="outList"></param>
    public void FindHooks(string name, List<UINode> outList) {
        UIInternal.FindNodes(hooks, name, outList);
    }

    #endregion
}
}