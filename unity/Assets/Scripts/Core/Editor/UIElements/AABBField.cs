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
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Wjybxx.BigCat.Core;
using Wjybxx.Commons;

namespace Wjybxx.BigCat.CoreEditor.UIElements
{
/// <summary>
/// 注意：不要手动修改xml中变量的label
/// </summary>
public class AABBField : BindableElement, INotifyValueChanged<MinMaxAABB>
{
    private const int MODE_MIN_MAX = 0;
    private const int MODE_MIN_SIZE = 1;
    private const int MODE_CENTER_SIZE = 2;
    private const int MODE_BOTTOM_SIZE = 3;

    private static readonly string[] _display = { "Min + Max", "Min + Size", "Center + Size", "Bottom + Size" };
    private static readonly string[] _selectedDisplay = { "√ Min + Max", "√ Min + Size", "√ Center + Size", "√ Bottom + Size" };
    private static int _mode = MODE_MIN_SIZE;

    private Foldout _foldout;
    private ToolbarMenu _modeMune;
    private ToolbarButton _repairButton;
    private Vector3Field _minField;
    private Vector3Field _maxField;
    private bool isInteger;

    private MinMaxAABB _value;
    private bool _valueInited;
    private bool _rebuildingValue;

    public MinMaxAABB value {
        get => _value;
        set {
            _valueInited = true;
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
        if (isInteger) {
            newValue.Truncate();
        }
        _valueInited = true;
        _value = newValue;
        // 字段为null表示无效对象或尚未正确初始化
        if (_rebuildingValue || _minField == null) {
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
        if (_minField == null) {
            throw new InvalidOperationException();
        }
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
                _modeMune.text = _display[MODE_MIN_MAX];
                _minField.label = "Min";
                _maxField.label = "Max";
                break;
            }
            case MODE_MIN_SIZE: {
                _modeMune.text = _display[MODE_MIN_SIZE];
                _minField.label = "Min";
                _maxField.label = "Size";
                break;
            }
            case MODE_CENTER_SIZE: {
                _modeMune.text = _display[MODE_CENTER_SIZE];
                _minField.label = "Center";
                _maxField.label = "Size";
                break;
            }
            case MODE_BOTTOM_SIZE: {
                _modeMune.text = _display[MODE_BOTTOM_SIZE];
                _minField.label = "Bottom";
                _maxField.label = "Size";
                break;
            }
        }
        SetValueWithoutNotify(aabb);
        // 刷新菜单
        _modeMune.menu.MenuItems().Clear();
        for (int mode = MODE_MIN_MAX; mode <= MODE_BOTTOM_SIZE; mode++) {
            string label = mode == _mode ? _selectedDisplay[mode] : _display[mode];
            int tempMode = mode;
            _modeMune.menu.InsertAction(mode, label, _ => SwitchMode(tempMode));
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

    private void OnEnable() {
        if (childCount == 0) {
            return; // 无效实例
        }
        _foldout = this.Q<Foldout>();
        _modeMune = this.Q<ToolbarMenu>("mode");
        _repairButton = this.Q<ToolbarButton>("repair");
        _minField = this.Q<Vector3Field>("min");
        _maxField = this.Q<Vector3Field>("max");
        UnityEditorUtil.SetVectorFieldStyle(_minField, -80);
        UnityEditorUtil.SetVectorFieldStyle(_maxField, -80);
        UnityEditorUtil.SetVectorFieldDelayed(_minField, true);
        UnityEditorUtil.SetVectorFieldDelayed(_maxField, true);
        //
        _foldout.RegisterValueChangedCallback(_ => OnFoldoutChanged());
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
        // 外部初始化以后，不再从文件初始化，而是覆盖文件中的值
        if (_valueInited) {
            SetValueWithoutNotify(_value);
        }
        OnFoldoutChanged();
        ShowAsMode();
    }

    private void OnFoldoutChanged() {
        _minField.SetDisplay(_foldout.value);
        _maxField.SetDisplay(_foldout.value);
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
        field._value = default; // xml中可能有默认值
        field._valueInited = true;
        field.isInteger = isInteger;
        return field;
    }

    public new class UxmlFactory : UxmlFactory<AABBField, UxmlTraits>
    {
    }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        private static readonly UxmlBoolAttributeDescription isIntegerAttribute = new UxmlBoolAttributeDescription
        {
            name = "isInteger"
        };

        public override IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription { get; } = new List<UxmlAttributeDescription>(1)
        {
            isIntegerAttribute
        };

        // 初始化方法：将 UXML 属性值赋给元素实例
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
            base.Init(ve, bag, cc);
            var myView = (AABBField)ve;
            isIntegerAttribute.TryGetValueFromBag(bag, cc, ref myView.isInteger);
            ve.schedule.Execute(() => { myView.OnEnable(); }).StartingIn(0);
        }
    }

    #endregion
}
}