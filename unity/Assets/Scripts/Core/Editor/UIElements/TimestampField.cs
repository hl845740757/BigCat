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
using Wjybxx.Dson.Types;

namespace Wjybxx.BigCat.CoreEditor.UIElements
{
/// <summary>
/// TODO 预览功能
/// </summary>
public class TimestampField : BindableElement, INotifyValueChanged<Timestamp>, IPrefixLabel
{
    private Label labelElement;
    private LongField _secondsField;
    private IntegerField _millisField;

    private Timestamp _value;
    private bool _rebuildingValue;

    public string label {
        get {
            EnsureInited();
            return labelElement.text;
        }
        set {
            EnsureInited();
            if (labelElement.text == value) {
                return;
            }
            this.labelElement.text = value;
            if (string.IsNullOrEmpty(this.labelElement.text)) {
                this.AddToClassList(BaseField<int>.noLabelVariantUssClassName);
                this.labelElement.RemoveFromHierarchy();
            } else if (!this.Contains(this.labelElement)) {
                this.Insert(0, this.labelElement);
                this.RemoveFromClassList(BaseField<int>.noLabelVariantUssClassName);
            }
        }
    }

    public Timestamp value {
        get {
            EnsureInited();
            return _value;
        }
        set {
            EnsureInited();
            if (_value == value) {
                return;
            }
            if (this.panel == null) {
                this.SetValueWithoutNotify(value);
                return;
            }
            using (ChangeEvent<Timestamp> pooled = ChangeEvent<Timestamp>.GetPooled(_value, value)) {
                pooled.target = this;
                this.SetValueWithoutNotify(value);
                this.SendEvent(pooled);
            }
        }
    }

    public void SetValueWithoutNotify(Timestamp newValue) {
        EnsureInited();
        _value = newValue;
        if (_rebuildingValue) {
            return;
        }
        _secondsField.SetValueWithoutNotify(newValue.Seconds);
        _millisField.SetValueWithoutNotify(newValue.ConvertNanosToMillis());
    }

    /// <summary>
    /// 获取实时值
    /// </summary>
    public Timestamp GetRealtimeValue() {
        EnsureInited();
        return Timestamp.OfEpochMillis(_secondsField.value * 1000 + _millisField.value);
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
        if (childCount == 0 || labelElement != null) {
            return; // Tip创建的临时对象或已初始化
        }
        labelElement = this.Q<Label>();
        _secondsField = this.Q<LongField>("seconds");
        _millisField = this.Q<IntegerField>("millis");

        _secondsField.RegisterValueChangedCallback(OnSecondsFieldValueChanged);
        _millisField.RegisterValueChangedCallback(OnMilliFieldValueChanged);
        //
        RebuildValue(false);
    }

    private void OnSecondsFieldValueChanged(ChangeEvent<long> evt) {
        evt.StopPropagation();
        RebuildValue();
    }

    private void OnMilliFieldValueChanged(ChangeEvent<int> evt) {
        evt.StopPropagation();
        RebuildValue();
    }

    #region uxml

    private const string UXML_PATH = "Assets/Scripts/Core/Editor/UIElements/TimestampField.uxml";

    public static TimestampField Create() {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML_PATH);
        TimestampField field = (TimestampField)visualTree.CloneTree()[0];
        field.SetValueWithoutNotify(default); // xml中可能有默认值
        return field;
    }

    public new class UxmlFactory : UxmlFactory<TimestampField, UxmlTraits>
    {
    }

    public new class UxmlTraits : BindableElement.UxmlTraits
    {
        // 初始化方法：将 UXML 属性值赋给元素实例
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
            base.Init(ve, bag, cc);
            var myView = (TimestampField)ve;
            ve.schedule.Execute(() => { myView.EnsureInited(); }).StartingIn(0);
        }
    }

    #endregion
}
}