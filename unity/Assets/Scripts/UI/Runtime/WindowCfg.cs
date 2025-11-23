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
/// UI窗口的配置
///
/// <h3>为什么实现MonoBehavior</h3>
/// Unity的设计就是一坨，AddComponent只能添加<see cref="MonoBehaviour"/>类型。
/// 这导致我们要避免和MonoBehavior的方法冲突的化，最好的方式就是编辑器不直接挂载最终<see cref="Window"/>，
/// 而是挂载<see cref="WindowCfg"/>。
///
/// TODO 记录上一次的展示模式？
/// </summary>
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
[ExecuteInEditMode]
public class WindowCfg : MonoBehaviour
{
    public const int MAX_DESKTOP = 5; // 最大桌面数
    public const int MAX_LAYER = 9; // 桌面画布的最大层级，其实是10层(0~9) - 4Bit
    public const int MAX_SORT_ORDER = 49; // 窗口画布的最大排序（子层级） - 6Bit

    public const string ANI_STATE_OPEN = "open"; // 窗口打开动画状态名
    public const string ANI_STATE_CLOSE = "close"; // 窗口关闭动画状态名

    /// <summary>
    /// 所属的桌面
    /// (也可认为是分组)
    /// </summary>
    [Range(0, 5)]
    [Tooltip("0表示未指定桌面，在哪个桌面请求打开，就显示在哪个桌面；否则打开到固定桌面")]
    public int desktopId = 0;
    /// <summary>
    /// 允许在哪些桌面打开
    /// </summary>
    [Range(1, 5)]
    [Tooltip("如果未指定，则可以在任意界面打开；暂未支持")]
    public List<int> allowDesktopIds = new List<int>();

    /// <summary>
    /// 窗口在桌面的层级，值越大越靠近上方
    /// </summary>
    [Range(0, MAX_LAYER)]
    [Tooltip("窗口画布的层级")]
    public int sortLayer = (MAX_LAYER + 1) / 2;
    /// <summary>
    /// 窗口在同层级内的排序
    /// </summary>
    [Range(0, MAX_SORT_ORDER)]
    [Tooltip("同层级窗口的排序")]
    public int sortOrder = (MAX_SORT_ORDER + 1) / 2;

    /// <summary>
    /// 数据地址
    /// </summary>
    public string dataAddress;
    /// <summary>
    /// UI根节点
    /// </summary>
    public UINode rootNode;
    /// <summary>
    /// 窗口特征值
    /// </summary>
    public WindowFeatures features;

    /// <summary>
    /// 是否是遮罩UI
    /// </summary>
    [Tooltip("是否是遮罩UI -- 遮罩UI的Layer和Order会自动设最大值")]
    public bool isMask;
    /// <summary>
    /// 窗口的类型(用户自定义tag)
    ///
    /// 注：其实理想的类型是long类型，64个Tag应当是够游戏窗口用的。
    /// </summary>
    public HashSet<int> tags = new HashSet<int>();
    /// <summary>
    /// 窗口的最大空闲时间
    ///
    /// 注：窗口关闭X秒以后自动销毁以释放内存
    /// </summary>
    [Tooltip("窗口关闭后的保留时间；单位秒，'-1'表示不销毁")]
    public float maxIdleTime = 30;

    /// <summary>
    /// 关联的Window实例
    /// </summary>
    [NonSerialized]
    private Window _window;

    #region Window

    /// <summary>
    /// 是否禁止关闭
    /// </summary>
    public bool unclosable => (features & WindowFeatures.Unclosable) != 0;
    /// <summary>
    /// 是否是跨桌面UI
    /// </summary>
    public bool isCrossDesktop => (features & WindowFeatures.CrossDesktop) != 0;
    
    public Window GetWindow() {
        return _window;
    }

    internal void SetWindow(Window window) {
        _window = window;
    }

    #endregion


#if UNITY_EDITOR
    private void OnEnable() {
        if (_window != null) {
            _window.MarkDirtyRepaint();
        }
    }

    private void Reset() {
        //
    }

    private void OnValidate() {
        if (isMask) {
            sortLayer = MAX_LAYER;
            sortOrder = MAX_SORT_ORDER;
        }

    }
#endif
}

/// <summary>
/// 窗口的展示样式
/// </summary>
public enum WindowDisplayMode
{
    Normal = 0, // 正常模式
    Fullscreen = 1, // 全屏模式 - 需要获取屏幕大小
    Floating = 2, // 浮动窗口 - 小窗模式
    Minimized = 3, // 最小化 - 即隐藏模式
}

/// <summary>
/// 窗口的互斥方式
///
/// 1.其实不建议使用互斥逻辑，窗口都打开也没什么问题 -- 因此不设计复杂的互斥规则。
/// 2.在不使用互斥逻辑的情况下，用户可以在打开窗口前通过Close方法关闭掉具有指定Tags的窗口。
/// </summary>
public enum WindowMutexMode
{
    None = 0,
}
}