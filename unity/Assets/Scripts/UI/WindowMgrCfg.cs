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
using Wjybxx.BigCat.MVC;

namespace Wjybxx.BigCat.UI
{
/// <summary>
/// 窗口管理器的配置类
///
/// 注：
/// 1.在Unity下继承MonoBehavior，允许在编辑器中配置。
/// 2.建议绑定实例到容器中
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Canvas))]
[DisallowMultipleComponent]
public sealed class WindowMgrCfg : MonoBehaviour
{
    /// <summary>
    /// 窗口加载器
    /// </summary>
    [NonSerialized] public WindowLoader windowLoader;
    /// <summary>
    /// 聚合数据模型
    /// </summary>
    [NonSerialized] public IAggregationModel aggregationModel;
    /// <summary>
    /// 数据模型解析器
    /// </summary>
    [NonSerialized] public IDataModelResolver dataModelResolver = new DataModelResolver();

    /// <summary>
    /// 定时器的最小间隔
    /// </summary>
    public double minPeriod = 0.01;
    /// <summary>
    /// 非缩放时间定时器的最小间隔
    /// </summary>
    public double unscaledMinPeriod = 0.01;
    /// <summary>
    /// 是否启用帧定时器
    /// (UI系统通常是不需要的)
    /// </summary>
    [Tooltip("如果UI系统存在按帧Update的逻辑，则需要启用该选项")]
    public bool enableFrameQueue;

    public WindowMgrCfg() {
    }
}
}