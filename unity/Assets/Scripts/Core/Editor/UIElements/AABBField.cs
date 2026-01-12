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
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Wjybxx.BigCat.Core;
using Wjybxx.Commons;

namespace Wjybxx.BigCat.Editor.UIElements
{
/// <summary>
/// 注意：不要手动修改xml中变量的label
/// </summary>
public class AABBField : BindableElement, INotifyValueChanged<MinMaxAABB>, IPrefixLabel
{
    private const int MODE_MIN_MAX = 0;
    private const int MODE_MIN_SIZE = 1;
    private const int MODE_CENTER_SIZE = 2;
    private const int MODE_BOTTOM_SIZE = 3;

    private static readonly string[] _modeDisplays = { "Min + Max", "Min + Size", "Center + Size", "Bottom + Size" };
    private static int _mode = MODE_MIN_SIZE;

    public bool isInteger { get; set; } // 属性才可以保存到UXML
    private Vector3Field _minField;
    private Vector3Field _maxField;

    private Foldout _foldout;
    private ToolbarMenu _modeMune;
    private ToolbarButton _repairButton;

    private MinMaxAABB _value;
    private bool _rebuildingValue;

    public AABBField() {
    }

    public string label {
        get {
            EnsureInited();
            return _foldout.text;
        }
        set {
            EnsureInited();
            _foldout.text = value;
        }
    }

    public MinMaxAABB value {
        get {
            EnsureInited();
            return _value;
        }
        set {
            EnsureInited();
            if (isInteger) value.Truncate();
            if (_value == value) {
                return;
            }
            if (this.panel == null) {
                this.SetValueWithoutNotify(value);
                return;
            }
            using (ChangeEvent<MinMaxAABB> pooled = ChangeEvent<MinMaxAABB>.GetPooled(_value, value)) {
                pooled.target = this;
                this.SetValueWithoutNotify(value);
                this.SendEvent(pooled);
            }
        }
    }

    public void SetValueWithoutNotify(MinMaxAABB newValue) {
        EnsureInited();
        if (isInteger) {
            newValue.Truncate();
        }
        _value = newValue;
        if (_rebuildingValue) {
            return;
        }
        switch (_minField.label) {
            case "Min": {
                _minField.SetValueWithoutNotify(newValue.min);
                _maxField.SetValueWithoutNotify(_maxField.label == "Max" ? newValue.max : newValue.Size);
                break;
            }
            case "Center": {
                _minField.SetValueWithoutNotify(newValue.Center);
                _maxField.SetValueWithoutNotify(newValue.Size);
                break;
            }
            case "Bottom": {
                _minField.SetValueWithoutNotify(newValue.Bottom);
                _maxField.SetValueWithoutNotify(newValue.Size);
                break;
            }
            default:
                throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// 获取实时值
    /// </summary>
    public MinMaxAABB GetRealtimeValue() {
        EnsureInited();
        // 我们这里通过变量的Label来判断数据的存储模式，会比在XML上再存储一个变量更安全
        switch (_minField.label) {
            case "Min": {
                Vector3 min = _minField.value;
                if (_maxField.label == "Max") {
                    return new MinMaxAABB(min, _maxField.value);
                }
                Vector3 size = _maxField.value;
                return new MinMaxAABB(min, min + size);
            }
            case "Center": {
                Vector3 center = _minField.value;
                Vector3 size = _maxField.value;
                return MinMaxAABB.OfCenter(center, size);
            }
            case "Bottom": {
                Vector3 bottom = _minField.value;
                Vector3 size = _maxField.value;
                return MinMaxAABB.OfBottom(bottom, size);
            }
            default:
                throw new InvalidOperationException();
        }
    }

    private void ShowAsMode() {
        MinMaxAABB aabb = GetRealtimeValue();
        switch (_mode) {
            default: {
                _modeMune.text = _modeDisplays[MODE_MIN_MAX];
                _minField.label = "Min";
                _maxField.label = "Max";
                break;
            }
            case MODE_MIN_SIZE: {
                _modeMune.text = _modeDisplays[MODE_MIN_SIZE];
                _minField.label = "Min";
                _maxField.label = "Size";
                break;
            }
            case MODE_CENTER_SIZE: {
                _modeMune.text = _modeDisplays[MODE_CENTER_SIZE];
                _minField.label = "Center";
                _maxField.label = "Size";
                break;
            }
            case MODE_BOTTOM_SIZE: {
                _modeMune.text = _modeDisplays[MODE_BOTTOM_SIZE];
                _minField.label = "Bottom";
                _maxField.label = "Size";
                break;
            }
        }
        SetValueWithoutNotify(aabb);
        // 刷新菜单
        _modeMune.menu.MenuItems().Clear();
        for (int mode = MODE_MIN_MAX; mode <= MODE_BOTTOM_SIZE; mode++) {
            int tempMode = mode;
            _modeMune.menu.InsertAction(mode, _modeDisplays[mode],
                _ => SwitchMode(tempMode),
                _mode == mode ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        }
    }

    /// <summary>
    /// 重新构建值（刷新缓存）
    /// </summary>
    /// <param name="notify">是否触发变化事件</param>
    private void RebuildValue(bool notify = true) {
        _rebuildingValue = true;
        try {
            if (notify) {
                value = GetRealtimeValue();
            } else {
                SetValueWithoutNotify(GetRealtimeValue());
            }
        }
        finally {
            _rebuildingValue = false;
        }
    }

    private void EnsureInited() {
        if (childCount == 0 || _foldout != null) {
            return; // Tip创建的临时对象或已初始化
        }
        _foldout = this.Q<Foldout>();
        _modeMune = this.Q<ToolbarMenu>("mode");
        _repairButton = this.Q<ToolbarButton>("repair");
        _minField = this.Q<Vector3Field>("min");
        _maxField = this.Q<Vector3Field>("max");
        //
        _repairButton.RegisterCallback<ClickEvent>(evt => {
            evt.StopPropagation();
            RepairValue();
        });
        _minField.RegisterValueChangedCallback(evt => {
            evt.StopPropagation();
            if (isInteger) {
                Vector3 newValue = evt.newValue;
                UnityEditorUtil.Truncate(ref newValue);
                _minField.SetValueWithoutNotify(newValue);
            }
            RebuildValue();
        });
        _maxField.RegisterValueChangedCallback(evt => {
            evt.StopPropagation();
            if (isInteger) {
                Vector3 newValue = evt.newValue;
                UnityEditorUtil.Truncate(ref newValue);
                _maxField.SetValueWithoutNotify(newValue);
            }
            RebuildValue();
        });
        //
        RebuildValue(false);
        ShowAsMode();
    }

    private void RepairValue() {
        MinMaxAABB aabb = GetRealtimeValue();
        aabb.Repair();
        value = aabb;
    }

    private void SwitchMode(int mode) {
        if (_mode == mode) {
            return;
        }
        _mode = mode;
        ShowAsMode();
        MarkDirtyRepaint();
    }

    #region uxml

    private const string UXML_PATH = "Assets/Scripts/Core/Editor/UIElements/AABBField.uxml";

    public static AABBField Create(bool isInteger = false) {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML_PATH);
        AABBField field = (AABBField)visualTree.CloneTree()[0];
        field.isInteger = isInteger;
        field.SetValueWithoutNotify(default); // xml中可能有默认值
        return field;
    }

    public new class UxmlFactory : UxmlFactory<AABBField, UxmlTraits>
    {
    }

    public new class UxmlTraits : BindableElement.UxmlTraits
    {
        private readonly UxmlBoolAttributeDescription isInteger = new()
        {
            name = "isInteger"
        };

        // 初始化方法：将 UXML 属性值赋给元素实例
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
            base.Init(ve, bag, cc);
            var myView = (AABBField)ve;
            myView.isInteger = isInteger.GetValueFromBag(bag, cc);
            ve.schedule.Execute(() => { myView.EnsureInited(); }).StartingIn(0);
        }
    }

    #endregion
}
}