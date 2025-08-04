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
/// 4.Controller和Node通过名字匹配，如果<see cref="nodeCfg"/>有名字，则Controller也必须具有相同的名字。
/// 5.可以通过<see cref="Behaviour.enabled"/>属性控制Node是否启用。
/// 6.Node并不独占GameObject，因此Show和Hide的时候不可以调用<see cref="GameObject.SetActive"/>，只操作关联的<see cref="elements"/>即可。
/// 7.Node每次<see cref="OnHide"/>的时候都应该清理临时数据。
/// </summary>
public class UINode : MonoBehaviour
{
    /// <summary>
    /// 视图配置
    /// </summary>
    public UINodeCfg nodeCfg;
    /// <summary>
    /// </summary>
    [SerializeField]
    [Tooltip("至少需要一个配置，第一个配置为默认展示模式")]
    private List<UINodeDisplayCfg> displayCfgs = new List<UINodeDisplayCfg>();

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
    [NonReorderable] internal int qIndex;

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
    /// Node的子节点
    /// </summary>
    [NonSerialized] protected readonly List<UINode> children = new List<UINode>();

    #region 生命周期

    protected void Awake() {
        nodeCfg ??= new UINodeCfg();
        nodeCfg.displayCfgs = displayCfgs; // 发布到cfg对象，方便外部访问
    }

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
            UINodeDisplayCfg displayCfg = FindDisplayCfg(displayMode);
            if (displayCfg == null) {
                throw new ArgumentException("invalid display mode: " + displayMode);
            }
            ResetDisplayElements(displayCfg);
        } else {
            ResetDisplayElements(nodeCfg.displayCfgs[0]);
        }
        // 先添加到Update队列 -- 这样用户Show过程中触发Hide仍然安全
        if (NeedUpdate) {
            window.AddUpdateNode(this);
        }
        OnShow(firstShow);
    }

    private void ResetDisplayElements(UINodeDisplayCfg displayCfg) {
        curDisplayCfg = displayCfg;
        elements.Clear();
        children.Clear();
        elements.AddRange(displayCfg.elements);
        children.AddRange(displayCfg.children);
    }

    private void InitController() {
        // 黑板是服务controller的
        if (nodeCfg.newBlackboard) {
            blackboard = new Blackboard();
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
        // 关闭
        foreach (GameObject element in elements) {
            element.SetActive(false);
        }
        foreach (UINode child in children) {
            child.Hide();
        }
        // 从Update队列删除
        if (qIndex >= 0) {
            _window.RemoveUpdateNode(this);
        }
        controller?.OnHide();
        OnHide();
        _ctl = 0;
        blackboard = null;
    }

    /// <summary>
    /// UI节点启动的时候调用
    ///
    /// 1.通常的逻辑为：初始化事件监听，为子节点绑定数据，刷新一次UI。
    /// 2.也可以根据Node自身的状态判断是否是首次展示。
    /// </summary>
    /// <param name="firstShow"></param>
    protected virtual void OnShow(bool firstShow) {
        foreach (UINode child in children) {
            if (!child.IsShowing && child.enabled) {
                ShowChild(child);
            }
        }
        controller?.OnShow(firstShow);
    }

    /// <summary>
    /// 重新绘制UI
    ///
    /// 注：如果有需要展示的Element，应当重写该方法。
    /// </summary>
    public virtual void Repaint() {
        ClearDirtyRepaint();
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
    /// 是否需要Update
    /// </summary>
    protected virtual bool NeedUpdate => false;

    /// <summary>
    /// UI心跳方法
    /// </summary>
    public virtual void OnUpdate() {

    }

    /// <summary>
    /// 切换展示模式
    /// </summary>
    /// <param name="mode"></param>
    public bool ChangeDisplayMode(int mode) {
        UINodeDisplayCfg displayCfg = FindDisplayCfg(mode);
        if (displayCfg == null) {
            return false;
        }
        if (curDisplayCfg != null && curDisplayCfg.mode == mode) {
            return false;
        }
        // 关闭
        foreach (GameObject element in elements) {
            element.SetActive(false);
        }
        foreach (UINode child in children) {
            child.Hide();
        }
        // 切换Elements
        UINodeDisplayCfg prevDisplayCfg = curDisplayCfg;
        ResetDisplayElements(displayCfg);
        // 刷新
        if (IsShowing) {
            OnDisplayModeChanged(prevDisplayCfg);
        }
        return true;
    }

    /// <summary>
    /// Node的显式模式改变
    /// </summary>
    /// <param name="prevDisplayCfg"></param>
    protected virtual void OnDisplayModeChanged(UINodeDisplayCfg prevDisplayCfg) {
        foreach (UINode child in children) {
            if (!child.IsShowing && child.enabled) {
                ShowChild(child);
            }
        }
        controller?.OnDisplayModeChanged(prevDisplayCfg);
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
    public void ClearDirtyRepaint() {
        _ctl &= ~UIInternal.MASK_DIRTY_REPAINT;
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 用户显式指定child的数据
    /// </summary>
    public void ShowChild(UINode child, object dataModel) {
        child.Show(_window, this, dataModel);
    }

    /// <summary>
    /// 根据child的数据地址自动解析展示数据
    /// </summary>
    /// <param name="child"></param>
    public void ShowChild(UINode child) {
        object dataModel = _window.windowMgr.ResolveDataModel(_dataModel, child.nodeCfg.dataAddress);
        child.Show(_window, this, dataModel);
    }

    public UINodeDisplayCfg FindDisplayCfg(int mode) {
        return UIInternal.FindDisplayCfg(nodeCfg.displayCfgs, mode);
    }

    public GameObject FindElement(string name) {
        return UIInternal.FindElement(elements, name);
    }

    public UINode FindChild(string name) {
        return UIInternal.FindNode(children, name);
    }

    #endregion

#if UNITY_EDITOR
    protected virtual void Reset() {
        nodeCfg ??= new UINodeCfg();
        if (displayCfgs.Count == 0) {
            displayCfgs.Add(new UINodeDisplayCfg());
        }
    }

    protected virtual void OnValidate() {
        if (displayCfgs.Count == 0) {
            displayCfgs.Add(new UINodeDisplayCfg());
        }
    }
#endif

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