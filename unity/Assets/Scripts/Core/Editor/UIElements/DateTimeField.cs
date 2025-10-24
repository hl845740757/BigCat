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

namespace Wjybxx.BigCat.CoreEditor.UIElements
{
/// <summary>
/// 
/// </summary>
public class DateTimeField : BindableElement, INotifyValueChanged<DateTime>, IField
{
    private Foldout _foldout;
    private Vector3IntField _dateField;
    private Vector3IntField _timeField;

    private DateTime _value;
    private bool _rebuildingValue;

    public DateTimeField() {
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

    public DateTime value {
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
            using (ChangeEvent<DateTime> pooled = ChangeEvent<DateTime>.GetPooled(_value, value)) {
                pooled.target = this;
                this.SetValueWithoutNotify(value);
                this.SendEvent(pooled);
            }
        }
    }

    public void SetValueWithoutNotify(DateTime newValue) {
        EnsureInited();
        _value = newValue;
        // 字段为null表示无效对象或尚未正确初始化
        if (_rebuildingValue) {
            return;
        }
        _dateField.SetValueWithoutNotify(new Vector3Int(newValue.Year, newValue.Month, newValue.Day));
        _timeField.SetValueWithoutNotify(new Vector3Int(newValue.Hour, newValue.Minute, newValue.Second));
    }

    /// <summary>
    /// 获取实时值
    /// </summary>
    public DateTime GetRealtimeValue() {
        EnsureInited();
        int year = _dateField.value.x;
        int month = _dateField.value.y;
        int day = _dateField.value.z;
        int hour = _timeField.value.x;
        int minute = _timeField.value.y;
        int second = _timeField.value.z;
        return new DateTime(year, month, day, hour, minute, second);
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
        _dateField = this.Q<Vector3IntField>("date");
        _timeField = this.Q<Vector3IntField>("time");
        _dateField.RegisterValueChangedCallback(OnDateFieldValueChanged);
        _timeField.RegisterValueChangedCallback(OnTimeFieldValueChanged);
        //
        RebuildValue(false);
    }

    private void OnDateFieldValueChanged(ChangeEvent<Vector3Int> evt) {
        evt.StopPropagation();
        Vector3Int newValue = evt.newValue;
        int year = newValue.x;
        int month = Mathf.Clamp(newValue.y, 1, 12);
        int daysInMonth = DateTime.DaysInMonth(year, month); // 动态修正Day范围
        int day = Math.Clamp(newValue.z, 0, daysInMonth);
        _dateField.SetValueWithoutNotify(new Vector3Int(year, month, day));
        RebuildValue();
    }

    private void OnTimeFieldValueChanged(ChangeEvent<Vector3Int> evt) {
        evt.StopPropagation();
        Vector3Int newValue = evt.newValue;
        int hour = Mathf.Clamp(newValue.x, 0, 23);
        int minute = Mathf.Clamp(newValue.y, 0, 59);
        int second = Mathf.Clamp(newValue.z, 0, 59);
        _timeField.SetValueWithoutNotify(new Vector3Int(hour, minute, second));
        RebuildValue();
    }

    #region uxml

    private const string UXML_PATH = "Assets/Scripts/Core/Editor/UIElements/DateTimeField.uxml";

    public static DateTimeField Create() {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML_PATH);
        DateTimeField field = (DateTimeField)visualTree.CloneTree()[0];
        field.SetValueWithoutNotify(default); // xml中可能有默认值
        return field;
    }

    public new class UxmlFactory : UxmlFactory<DateTimeField, UxmlTraits>
    {
    }

    public new class UxmlTraits : BindableElement.UxmlTraits
    {
        // 初始化方法：将 UXML 属性值赋给元素实例
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
            base.Init(ve, bag, cc);
            var myView = (DateTimeField)ve;
            ve.schedule.Execute(() => { myView.EnsureInited(); }).StartingIn(0);
        }
    }

    #endregion
}
}