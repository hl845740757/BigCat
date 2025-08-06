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
using UnityEngine;
using Wjybxx.Commons;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Wjybxx.BigCat.UI
{
/// <summary>
/// UI视图节点
/// 
/// 1.UINode并不由Unity引擎调度，而是由我们的<see cref="WindowMgr"/>调度。
/// 2.为减少开销，UINode和ViewScript是合并抽象的，由子类负责绘制 -- 子类应当命名为View。
/// 3.子类可以将Node绑定的数据模型向下类型转换，缓存到本地。
/// 4.可以通过<see cref="Behaviour.enabled"/>属性控制Node是否启用。
/// 5.Node并不独占GameObject，因此Show和Hide的时候不可以调用<see cref="GameObject.SetActive"/>，只操作关联的<see cref="elements"/>即可。
/// 6.Node每次<see cref="OnHide"/>的时候都应该清理临时数据。
/// </summary>
public class UINode : MonoBehaviour
{
    /// <summary>
    /// 视图配置
    /// </summary>
    public UINodeCfg nodeCfg;
    /// <summary>
    /// 默认配置
    /// 注：由于大多数情况下只有一个配置，因此我们将默认配置提到外层，便于配置
    /// </summary>
    [SerializeField]
    private UINodeDisplayCfg defaultDisplayCfg;
    /// <summary>
    /// 更多展示模式配置
    /// </summary>
    [SerializeField]
    private List<UINodeDisplayCfg> moreDisplayCfgs = new List<UINodeDisplayCfg>();

    /// <summary>
    /// Node所属的窗口
    /// </summary>
    [NonSerialized] private Window _window;
    /// <summary>
    /// 父节点
    /// </summary>
    [NonSerialized] private UINode _parent;
    /// <summary>
    /// Node绑定的数据
    ///
    /// 1.可能是逻辑层数据模型，也可能是表现层数据模型
    /// 2.建议子类强转之后保存自身上
    /// </summary>
    [NonSerialized] private object _dataModel;

    /// <summary>
    /// 控制标记，避免过多的bool字段
    /// </summary>
    [NonSerialized] private int _ctl;
    /// <summary>
    /// 重入Id，只增不减
    ///
    /// 1.enter和exit都增加；
    /// 2.事件处理脚本可以捕获该id，以判断UI是否进入到了新的生命周期；
    /// </summary>
    [NonSerialized] private int _reentryId;
    /// <summary>
    /// 在Update队列中的索引
    /// </summary>
    [NonSerialized] internal int qIndex = -1;
    /// <summary>
    /// 在父节点中的索引(hook始终是-1)
    /// </summary>
    [NonSerialized] internal int uiIndex = -1;

    /// <summary>
    /// Node绑定的黑板
    ///
    /// 注：
    /// 1.黑板用于逻辑上归于同一组UI的Controller交互，通常只包含一些简单的属性。
    /// 2.由于Node自身可能充当Controller，因此保存在Node上。
    /// 3.黑板每次绑定到父节点时都会重置，可以在OnShow的时候手动替换黑板引用。
    /// </summary>
    [NonSerialized] protected Blackboard blackboard;
    /// <summary>
    /// 外部控制器（事件处理器）
    ///
    /// 注：可能为null，要么没有事件逻辑，要么被自己或父节点处理了。
    /// </summary>
    [NonSerialized] protected Controller controller;

    /// <summary>
    /// Node当前的展示模式
    /// </summary>
    [NonSerialized] protected UINodeDisplayCfg curDisplayCfg;
    /// <summary>
    /// Node要操作的对象
    /// </summary>
    [NonSerialized] protected readonly List<GameObject> elements = new List<GameObject>();
    /// <summary>
    /// Node要操作的钩子节点
    /// </summary>
    [NonSerialized] protected readonly List<UINode> hooks = new List<UINode>();
    /// <summary>
    /// Node的子节点
    /// </summary>
    [NonSerialized] protected readonly List<UINode> children = new List<UINode>();

    #region 生命周期

    public void Show(Window window, UINode parent, object dataModel, int displayMode = -1) {
        if (IsShowing) {
            throw new InvalidOperationException("node is already showing");
        }
        bool firstShow = this._window == null;
        _window = window;
        _parent = parent;
        _dataModel = dataModel;
        _reentryId++;
        _ctl |= UIInternal.MASK_SHOWING;
        if (firstShow) {
            InitController();
        }
        // 重置UI元素
        if (displayMode >= 0) {
            UINodeDisplayCfg displayCfg = nodeCfg.FindDisplayCfg(displayMode);
            if (displayCfg == null) {
                throw new ArgumentException("invalid display mode: " + displayMode);
            }
            ResetDisplayElements(displayCfg);
        } else {
            ResetDisplayElements(nodeCfg.defaultDisplayCfg);
        }
        int rid = _reentryId;
        OnShow(firstShow);
        // 确保未退出
        if (rid == _reentryId && NeedUpdate) {
            window.AddUpdateNode(this);
        }
    }

    private void ResetDisplayElements(UINodeDisplayCfg displayCfg) {
        curDisplayCfg = displayCfg;
        elements.Clear();
        hooks.Clear();
        children.Clear();
        if (displayCfg.elements.Count > 0) elements.AddRange(displayCfg.elements);
        if (displayCfg.hooks.Count > 0) hooks.AddRange(displayCfg.hooks);
        if (displayCfg.children.Count > 0) {
            children.AddRange(displayCfg.children);
            RefreshChildrenIndex();
        }
    }

    private void InitController() {
        if (nodeCfg.newBlackboard) {
            blackboard ??= new Blackboard();
        } else {
            blackboard = _parent ? _parent.Blackboard : _window.Blackboard;
        }
        if (!controller && (controller = UIInternal.FindController(this))) {
            controller.Init(this);
        }
    }

    public void Hide() {
        if (!IsShowing) {
            return;
        }
        _reentryId++;
        _ctl &= ~UIInternal.MASK_SHOWING;
        _window.RemoveUpdateNode(this);
        // 隐藏所有元素
        foreach (GameObject element in elements) {
            element.SetActive(false);
        }
        foreach (UINode hook in hooks) {
            hook.Hide();
        }
        foreach (UINode child in children) {
            child.Hide();
        }
        controller?.OnHide();
        OnHide();
        _ctl = 0;
        // 私有黑板只需清理
        if (nodeCfg.newBlackboard) {
            blackboard?.Clear();
        } else {
            blackboard = null;
        }
    }

    /// <summary>
    /// UI节点启动的时候调用
    ///
    /// 1.通常的逻辑为：初始化事件监听，为子节点绑定数据，刷新一次UI。
    /// 2.如果需要心跳，可在此方法中设置<see cref="NeedUpdate"/>。
    /// 3.如果有直接操控的<see cref="elements"/>，需要重写该方法
    /// </summary>
    /// <param name="firstShow"></param>
    protected virtual void OnShow(bool firstShow) {
        foreach (UINode hook in hooks) {
            if (hook.enabled) {
                ShowChild(hook, hook.name);
            }
        }
        foreach (UINode child in children) {
            if (child.enabled) {
                ShowChild(child);
            }
        }
        controller?.OnShow(firstShow);
    }

    /// <summary>
    /// 重新绘制UI
    ///
    /// 注：如果有直接操控的<see cref="elements"/>，需要重写该方法。
    /// </summary>
    public virtual void Repaint() {
        _ctl &= ~UIInternal.MASK_DIRTY_REPAINT;
        foreach (UINode hook in hooks) {
            if (hook.IsShowing) {
                hook.Repaint();
            }
        }
        foreach (UINode child in children) {
            if (child.IsShowing) {
                child.Repaint();
            }
        }
    }

    /// <summary>
    /// UI节点退出的时候调用
    /// </summary>
    protected virtual void OnHide() {

    }

    /// <summary>
    /// UI心跳方法
    ///
    /// 关注<see cref="NeedUpdate"/>属性和<see cref="ClearDirtyRepaint"/>方法。
    /// </summary>
    public virtual void OnUpdate() {
    }

    /// <summary>
    /// 是否需要Update
    /// 注：该属性为false的节点也可以加入到调度队列，但执行一次<see cref="OnUpdate"/>后就会被删除。
    /// </summary>
    public bool NeedUpdate {
        get => (_ctl & UIInternal.MASK_NEED_UPDATE) != 0;
        set => _ctl = BitFlags.Set(_ctl, UIInternal.MASK_NEED_UPDATE, value);
    }

    /// <summary>
    /// 标记为需要下一帧重新绘制
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkDirtyRepaint() {
        _ctl |= UIInternal.MASK_DIRTY_REPAINT;
    }

    /// <summary>
    /// 清理重新绘制标记
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ClearDirtyRepaint() {
        if ((_ctl & UIInternal.MASK_DIRTY_REPAINT) != 0) {
            _ctl &= ~UIInternal.MASK_DIRTY_REPAINT;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 切换展示模式
    ///
    /// 注：默认的切换可能会显得生硬，因此子类可重写实现。
    /// </summary>
    /// <param name="mode"></param>
    public virtual bool ChangeDisplayMode(int mode) {
        UINodeDisplayCfg displayCfg = nodeCfg.FindDisplayCfg(mode);
        if (displayCfg == null) {
            return false;
        }
        if (curDisplayCfg != null && curDisplayCfg.mode == mode) {
            return false;
        }
        // 隐藏Elements - 可能不同模式存在交集，由子类自行优化
        foreach (GameObject element in elements) {
            element.SetActive(false);
        }
        foreach (UINode hook in hooks) {
            hook.Hide();
        }
        foreach (UINode child in children) {
            child.Hide();
        }
        // 切换Elements
        UINodeDisplayCfg prevDisplayCfg = curDisplayCfg;
        ResetDisplayElements(displayCfg);
        if (IsShowing) {
            OnDisplayModeChanged(prevDisplayCfg);
        }
        return true;
    }

    /// <summary>
    /// 模式切换
    /// 
    /// 注：如果有直接操控的<see cref="elements"/>，需要重写该方法
    /// </summary>
    /// <param name="prevDisplayCfg"></param>
    protected virtual void OnDisplayModeChanged(UINodeDisplayCfg prevDisplayCfg) {
        // 可能存在在不同模式下都展示的Node，因此需要检测IsShowing
        foreach (UINode hook in hooks) {
            if (!hook.IsShowing && hook.enabled) {
                ShowChild(hook);
            }
        }
        foreach (UINode child in children) {
            if (!child.IsShowing && child.enabled) {
                ShowChild(child);
            }
        }
        controller?.OnDisplayModeChanged(prevDisplayCfg);
    }

    #endregion

    #region child排序

    /// <summary>
    /// 在兄弟节点中的排序
    /// </summary>
    /// <returns></returns>
    public int GetSiblingIndex() => uiIndex;

    /// <summary>
    /// 设置在兄弟节点中的排序
    /// </summary>
    /// <param name="index"></param>
    public void SetSiblingIndex(int index) {
        if (_parent) {
            _parent.SetChildIndex(this, index);
        }
    }

    /// <summary>
    /// 设置为兄弟节点中的第一个
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetAsFirstSibling() {
        if (_parent) {
            _parent.SetChildIndex(this, 0);
        }
    }

    /// <summary>
    /// 设置为兄弟节点中的最后一个
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetAsLastSibling() {
        if (_parent) {
            _parent.SetChildIndex(this, children.Count - 1);
        }
    }

    /// <summary>
    /// 设置子节点的索引
    ///
    /// 1.是否支持-1删除子节点取决于子类
    /// 2.可能需要重新绑定子节点的数据
    /// </summary>
    protected virtual void SetChildIndex(UINode child, int index) {
        int prevIndex = child.uiIndex;
        if (prevIndex == index) {
            return;
        }
        children.RemoveAt(prevIndex);
        children.Insert(index, child);
        RefreshChildrenIndex(prevIndex, index);
    }

    /// <summary>
    /// 设置在父节点中的索引。
    /// 
    /// 注：该方法用于辅助<see cref="SetChildIndex"/>实现。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Internal_SetIndex(int index) {
        uiIndex = index;
    }

    /// <summary>
    /// 刷新子节点的索引
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RefreshChildrenIndex() {
        UIInternal.RefreshChildrenIndex(children);
    }

    /// <summary>
    /// 刷新子节点的索引
    ///
    /// 注：为方便使用，允许start大于end，会自动纠正。
    /// </summary>
    /// <param name="start">开始索引-包含</param>
    /// <param name="end">结束索引-包含</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RefreshChildrenIndex(int start, int end = -1) {
        if (end == -1) {
            end = children.Count - 1;
            UIInternal.RefreshChildrenIndex(children, start, end);
        } else if (start <= end) { // 等于也需要刷新
            UIInternal.RefreshChildrenIndex(children, start, end);
        } else {
            UIInternal.RefreshChildrenIndex(children, end, start);
        }
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 用户显式指定child的数据
    /// </summary>
    public void ShowChild(UINode child, object dataModel, int displayMode = -1) {
        child.Show(_window, this, dataModel, displayMode);
    }

    /// <summary>
    /// 根据child的数据地址自动解析展示数据
    ///
    /// 注：hook节点的地址是精确地址，不会使用index解析其数据。
    /// </summary>
    /// <param name="child"></param>
    public void ShowChild(UINode child) {
        object dataModel = _window.windowMgr.ResolveDataModel(_dataModel, child.nodeCfg.dataAddress, uiIndex);
        child.Show(_window, this, dataModel);
    }

    public GameObject FindElement(string name) {
        return UIInternal.FindElement(elements, name);
    }

    public UINode FindChild(string name) {
        return UIInternal.FindNode(children, name);
    }

    public UINode FindHook(string name) {
        return UIInternal.FindNode(hooks, name);
    }

    public void FindHooks(string name, List<UINode> outList) {
        UIInternal.FindNodes(hooks, name, outList);
    }

    #endregion

    #region unity脚本生命周期

    protected void Awake() {
        nodeCfg ??= new UINodeCfg();
        nodeCfg.defaultDisplayCfg = defaultDisplayCfg;
        nodeCfg.moreDisplayCfgs = moreDisplayCfgs; // 发布到cfg对象，方便外部访问
    }

    protected virtual void OnEnable() {
        if (IsShowing) Repaint();
    }

    protected virtual void OnDisable() {
    }

    protected virtual void OnDestroy() {
        _window = null;
        _parent = null;
        _dataModel = null;

        curDisplayCfg = null;
        elements.Clear();
        hooks.Clear();
        children.Clear();
    }

#if UNITY_EDITOR
    protected virtual void Reset() {
        nodeCfg ??= new UINodeCfg();
        defaultDisplayCfg ??= new UINodeDisplayCfg();
    }

    protected virtual void OnValidate() {
    }
#endif

    #endregion

    #region props

    public bool IsShowing => (_ctl & UIInternal.MASK_SHOWING) != 0;

    public Window Window => _window;
    public UINode Parent => _parent;
    public object DataModel => _dataModel;

    public int ReentryId => _reentryId;

    public Blackboard Blackboard {
        get => blackboard;
        set => blackboard = value;
    }

    public Controller Controller {
        get => controller;
        set => controller = value;
    }

    public UINodeDisplayCfg CurrentDisplayCfg => curDisplayCfg;

    #endregion
}
}