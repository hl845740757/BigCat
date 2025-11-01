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

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Wjybxx.BigCat.Core;

namespace Wjybxx.BigCat.CoreEditor.UIElements
{
public class Euler32Field : BindableElement, INotifyValueChanged<Euler32>, IPrefixLabel
{
    private Vector3IntField _field;
    private Euler32 _value;
    private bool _rebuildingValue;

    public Euler32Field() {
    }

    public string label {
        get {
            EnsureInited();
            return _field.label;
        }
        set {
            EnsureInited();
            _field.label = value;
        }
    }

    public Euler32 value {
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
            using (ChangeEvent<Euler32> pooled = ChangeEvent<Euler32>.GetPooled(_value, value)) {
                pooled.target = this;
                this.SetValueWithoutNotify(value);
                this.SendEvent(pooled);
            }
        }
    }

    public void SetValueWithoutNotify(Euler32 newValue) {
        EnsureInited();
        _value = newValue;
        if (_rebuildingValue) {
            return;
        }
        _field.SetValueWithoutNotify(newValue);
    }

    /// <summary>
    /// 获取实时值
    /// </summary>
    public Euler32 GetRealtimeValue() {
        EnsureInited();
        Vector3Int newValue = _field.value;
        return new Euler32(newValue.x, newValue.y, newValue.z);
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
        if (childCount == 0 || _field != null) {
            return; // Tip创建的临时对象或已初始化
        }
        _field = this.Q<Vector3IntField>();
        _field.RegisterValueChangedCallback(OnFieldValueChanged);
        //
        RebuildValue(false);
    }

    private void OnFieldValueChanged(ChangeEvent<Vector3Int> evt) {
        evt.StopPropagation();
        Vector3Int newValue = _field.value;
        newValue.x = (int)Mathf.Repeat(newValue.x, 360);
        newValue.y = (int)Mathf.Repeat(newValue.y, 360);
        newValue.z = (int)Mathf.Repeat(newValue.z, 360);
        _field.SetValueWithoutNotify(newValue);
        RebuildValue();
    }

    #region uxml

    private const string UXML_PATH = "Assets/Scripts/Core/Editor/UIElements/Euler32Field.uxml";

    public static Euler32Field Create() {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML_PATH);
        Euler32Field field = (Euler32Field)visualTree.CloneTree()[0];
        field.SetValueWithoutNotify(default); // xml中可能有默认值
        return field;
    }

    public new class UxmlFactory : UxmlFactory<Euler32Field, UxmlTraits>
    {
    }

    public new class UxmlTraits : BindableElement.UxmlTraits
    {
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
            base.Init(ve, bag, cc);
            var myView = (Euler32Field)ve;
            myView.schedule.Execute(() => myView.EnsureInited());
        }
    }

    #endregion
}
}