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
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCat.UI
{
/// <summary>
/// 桌面抽象(更像手机的分屏)
///
/// PS：桌面是一个简单的窗口容器，职责很轻。
/// </summary>
public sealed class Desktop
{
    /// <summary>
    /// 桌面id
    /// </summary>
    private readonly int _desktopId;
    /// <summary>
    /// 根画布
    /// </summary>
    private readonly Canvas _canvas;
    /// <summary>
    /// 当前桌面的窗口
    /// </summary>
    private readonly List<Window> _stack = new(4);
    /// <summary>
    /// 窗口开启序号
    /// (用于实现新打开的窗口在更上层)
    /// (会动态修正，避免无限膨胀)
    /// </summary>
    private int openOrder;
    /// <summary>
    /// 桌面是否处于显示状态
    /// </summary>
    private bool showing;

    internal Desktop(int desktopId, Canvas canvas) {
        _desktopId = desktopId;
        _canvas = canvas;
    }

    /// <summary>
    /// 桌面id
    /// </summary>
    public int DesktopId => _desktopId;
    /// <summary>
    /// 当前是否处于显示状态
    /// </summary>
    public bool IsShowing => showing;
    /// <summary>
    /// 当前栈信息
    /// </summary>
    public List<Window> Stack => _stack;
    /// <summary>
    /// 顶部Window
    /// </summary>
    public Window TopWindow => _stack.TryPeekLast(out Window window) ? window : null;
    /// <summary>
    /// 底部Window
    /// </summary>
    public Window BottomWindow => _stack.TryPeekFirst(out Window window) ? window : null;

    #region stack

    /// <summary>
    /// 展示桌面
    /// </summary>
    internal void Show() {
        showing = true;
        SortWindows(); // 会setActive
        foreach (Window window in _stack) {
            window.Repaint();
        }
    }

    /// <summary>
    /// 隐藏桌面
    ///
    /// 注意：桌面被隐藏，不意味着窗口逻辑被暂停。
    /// </summary>
    internal void Hide() {
        showing = false;
        foreach (Window window in _stack) {
            window.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 添加窗口
    /// </summary>
    internal void Add(Window window, bool refresh = true) {
        if (window.desktop != null) throw new ArgumentException("Window.desktop != null");
        window.desktop = this;
        window.openOrder = ++openOrder;
        _stack.Add(window);
        if (!showing) {
            window.gameObject.SetActive(false);
        }
        if (refresh) {
            SortWindows();
        }
    }

    /// <summary>
    /// 删除窗口
    /// </summary>
    internal void Remove(Window window, bool refresh = true) {
        if (window.desktop != this) throw new ArgumentException("window.desktop != this");
        _stack.Remove(window);
        window.desktop = null;
        if (refresh) {
            SortWindows();
        }
    }

    /// <summary>
    /// 删除窗口
    /// </summary>
    internal void RemoveAt(int index, bool refresh = true) {
        Window window = _stack[index];
        _stack.RemoveAt(index);
        window.desktop = null;
        if (refresh) {
            SortWindows();
        }
    }

    /// <summary>
    /// 将创建移动到顶部
    /// (允许用户调用；通常用户点击某个窗口的时候，就需要将Window移动到顶层)
    /// </summary>
    public void MoveToTop(Window window) {
        if (window.desktop != this) throw new ArgumentException("window.desktop != this");
        if (TopWindow == window) {
            return;
        }
        _stack.Remove(window);
        _stack.Add(window);
        SortWindows();
    }

    /// <summary>
    /// 将窗口移动到底部
    /// (允许用户调用)
    /// </summary>
    public void MoveToBottom(Window window) {
        if (window.desktop != this) throw new ArgumentException("window.desktop != this");
        if (BottomWindow == window) {
            return;
        }
        _stack.Remove(window);
        _stack.Insert(0, window);
        SortWindows();
    }

    /// <summary>
    /// 重排序Window层级
    /// 
    /// TODO PC游戏的话，窗口是可以挪动的，需要动态处理Window的Active状态；暂改为public，允许用户随时调用。
    /// </summary>
    public void SortWindows() {
        // 非显示状态下不纠正窗口层级，Show的时候会修正
        if (!showing) {
            return;
        }
        openOrder = 0;
        foreach (Window window in _stack) {
            window.openOrder = ++openOrder;
        }
        _stack.Sort(WindowComparer.Inst);
        // 理论上可以不设置Active，但不必要的渲染会浪费性能
        for (int index = _stack.Count - 1; index >= 0; index--) {
            Window window = _stack[index];
            window.transform.SetSiblingIndex(index);
            window.gameObject.SetActive(!IsOverlapped(window, index));
        }
        // 修正Window的画布层级 -- 需要为每个Window画布预留一段层级，才能确保上面界面的Node一定显示在最上面
        const int sortOrderPerWindow = WindowCfg.MAX_SORT_ORDER + 1;
        for (int index = 0; index < _stack.Count; index++) {
            Window window = _stack[index];
            window.SetCanvasLayer(_canvas.sortingLayerID, _canvas.sortingOrder + (index * sortOrderPerWindow));
        }
    }

    private bool IsOverlapped(Window window, int index) {
        // 完整测试的话会比较复杂，开销也比较大，先只做简单测试，即要求必须某个单一界面完整覆盖
        // 理论上可能被多个界面组合覆盖
        Rect winTransRect = window.transform.rect;
        for (int i = index + 1; i < _stack.Count; i++) {
            if (_stack[i].DisplayMode == WindowDisplayMode.Fullscreen) {
                return true;
            }
            Rect upperRect = _stack[i].transform.rect;
            if (upperRect.Overlaps(winTransRect)) {
                return true; // TODO 存在旋转的情况下正确吗？
            }
        }
        return false;
    }

    private class WindowComparer : IComparer<Window>
    {
        internal static WindowComparer Inst { get; } = new WindowComparer();

        public int Compare(Window lhs, Window rhs) {
            if (lhs == null || rhs == null) {
                return lhs == null ? -1 : 1;
            }
            int r = lhs.windowCfg.sortLayer.CompareTo(rhs.windowCfg.sortLayer);
            if (r != 0) return r;
            r = lhs.windowCfg.sortOrder.CompareTo(rhs.windowCfg.sortOrder);
            return lhs.openOrder.CompareTo(rhs.openOrder);
        }
    }

    #endregion
}
}