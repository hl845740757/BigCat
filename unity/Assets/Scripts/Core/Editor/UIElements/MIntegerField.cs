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

using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Wjybxx.BigCat.CoreEditor.UIElements
{
public class MIntegerField : IntegerField, IPrefixLabel
{
    public MIntegerField() {
    }

    public MIntegerField(string label) : base(label) {
    }

    public int min { get; set; }
    public int max { get; set; }
    public bool hasMin { get; set; }
    public bool hasMax { get; set; }
    public float labelMargin {
        get => labelElement.style.marginRight.value.value;
        set => labelElement.style.marginRight = new Length(value);
    }

    public override int value {
        get => base.value;
        set {
            value = Clamp(value);
            base.value = value;
        }
    }

    public override void SetValueWithoutNotify(int newValue) {
        newValue = Clamp(newValue);
        base.SetValueWithoutNotify(newValue);
    }

    private int Clamp(int newValue) {
        if (hasMin && newValue < min) return min;
        if (hasMax && newValue > max) return max;
        return newValue;
    }

    public new class UxmlFactory : UxmlFactory<MIntegerField, UxmlTraits>
    {
    }

    public new class UxmlTraits : IntegerField.UxmlTraits
    {
        // 会自动扫描UxmlAttributeDescription字段，不能是静态属性
        private readonly UxmlIntAttributeDescription minAttribute = new()
        {
            name = "min",
            defaultValue = 0,
        };
        private readonly UxmlIntAttributeDescription maxAttribute = new()
        {
            name = "max",
            defaultValue = 0,
        };
        private readonly UxmlBoolAttributeDescription hasMinAttribute = new()
        {
            name = "hasMin",
            defaultValue = false,
        };
        private readonly UxmlBoolAttributeDescription hasMaxAttribute = new()
        {
            name = "hasMax",
            defaultValue = false,
        };
        private readonly UxmlFloatAttributeDescription labelMargin = new()
        {
            name = "label-margin",
            defaultValue = 0,
        };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
            base.Init(ve, bag, cc);
            var myView = (MIntegerField)ve;
            myView.min = minAttribute.GetValueFromBag(bag, cc);
            myView.max = maxAttribute.GetValueFromBag(bag, cc);
            myView.hasMin = hasMinAttribute.GetValueFromBag(bag, cc);
            myView.hasMax = hasMaxAttribute.GetValueFromBag(bag, cc);
            myView.labelMargin = labelMargin.GetValueFromBag(bag, cc);
        }
    }
}
}