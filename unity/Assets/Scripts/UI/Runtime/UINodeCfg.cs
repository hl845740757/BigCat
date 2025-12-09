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
///
/// TODO 数据模型绑定配置？其实对于游戏开发而言，自动数据绑定不那么必要。
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
    /// 数据地址
    ///
    /// 0.空字符串表示沿用父节点数据
    /// 1.斜杠开头表绝对路径：<code>/view/login/xxx</code>
    /// 2.非斜杠开头表父节点数据的相对路径:<code>login/xxx</code>
    /// </summary>
    [Tooltip("View关联的数据模型地址，规则请查看文档")]
    public string dataAddress;

    /// <summary>
    /// 黑板处理策略
    ///
    /// 注：用于处理公共节点的复用问题。
    /// </summary>
    [Tooltip("黑板策略，用于告诉view和controller如何处理黑板的初始化")]
    public int blackboardPolicy;
    /// <summary>
    /// 自定义flags
    ///
    /// 注：建议高16位用于运行时程序控制，低16位用于编辑器静态配置。
    /// </summary>
    public int flags;

    /// <summary>
    /// 当前Node操作的GameObject
    ///
    /// 注：GameObject的名字是有意义的，Node根据Name查找GameObject。
    /// </summary>
    [Tooltip("当前Node直接控制的文本和按钮等")]
    public List<GameObject> elements = new List<GameObject>();
    /// <summary>
    /// 当前Node的子节点
    ///
    /// 注：hook与children混合配置以减少不必要的开销，通过name区分即可。
    /// </summary>
    public List<UINode> children = new List<UINode>();
    /// <summary>
    /// child模板
    ///
    /// 注：多数情况下为空数组，数组开销更低
    /// </summary>
    [Tooltip("子节点模板，用于动态创建子节点的情况，如ListView")]
    public UINode[] templates = Array.Empty<UINode>();

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
    public UINode FindNode(string name) {
        return UIInternal.FindNode(children, name);
    }

    /// <summary>
    /// 查找指定name的所有子节点
    /// </summary>
    public void FindNodes(string name, List<UINode> outList) {
        UIInternal.FindNodes(children, name, outList);
    }

    /// <summary>
    /// 查找指定name的模板节点
    /// </summary>
    public UINode FindTemplate(string name) {
        return UIInternal.FindNode(templates, name);
    }

    #endregion
}
}