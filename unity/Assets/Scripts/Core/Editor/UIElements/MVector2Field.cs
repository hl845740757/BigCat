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
using Wjybxx.BigCat.Core;

namespace Wjybxx.BigCat.Editor.UIElements
{
public class MVector2Field : Vector2Field, IPrefixLabel
{
    private bool _isReadOnly;
    private bool _isDelayed;
    private float _xyzFlexBasis;

    public MVector2Field() {
    }

    public MVector2Field(string label) : base(label) {
    }

    public bool isReadOnly {
        get => _isReadOnly;
        set {
            _isReadOnly = value;
            UnityEditorUtil.SetVectorFieldReadonly(this, value);
        }
    }

    public bool isDelayed {
        get => _isDelayed;
        set {
            _isDelayed = value;
            UnityEditorUtil.SetVectorFieldDelayed(this, value);
        }
    }

    public string xLabel {
        get => xField.label;
        set => xField.label = value;
    }
    public string yLabel {
        get => yField.label;
        set => yField.label = value;
    }

    public float labelMargin {
        get => labelElement.style.marginRight.value.value;
        set => labelElement.style.marginRight = new Length(value);
    }
    public float xLabelMargin {
        get => xField.labelElement.style.marginRight.value.value;
        set => xField.labelElement.style.marginRight = new Length(value);
    }
    public float yLabelMargin {
        get => yField.labelElement.style.marginRight.value.value;
        set => yField.labelElement.style.marginRight = new Length(value);
    }
    public float xyzFlexBasis {
        get => _xyzFlexBasis;
        set {
            _xyzFlexBasis = value;
            UnityEditorUtil.SetVectorFieldFlexBasis(this, value);
        }
    }

    private VisualElement inputContent => childCount == 1 ? this[0] : this[1];
    private FloatField xField => (FloatField)inputContent[0];
    private FloatField yField => (FloatField)inputContent[1];

    public new class UxmlFactory : UxmlFactory<MVector2Field, UxmlTraits>
    {
    }

    public new class UxmlTraits : Vector2Field.UxmlTraits
    {
        private readonly UxmlBoolAttributeDescription isReadonly = new()
        {
            name = "isReadonly",
            defaultValue = false
        };
        private readonly UxmlBoolAttributeDescription isDelayed = new()
        {
            name = "isDelayed",
            defaultValue = false
        };
        private readonly UxmlStringAttributeDescription xLabel = new()
        {
            name = "x-label",
            defaultValue = "X"
        };
        private readonly UxmlStringAttributeDescription yLabel = new()
        {
            name = "y-label",
            defaultValue = "Y"
        };

        private readonly UxmlFloatAttributeDescription labelMargin = new()
        {
            name = "label-margin",
            defaultValue = 0,
        };
        private readonly UxmlFloatAttributeDescription xLabelMargin = new()
        {
            name = "x-label-margin",
            defaultValue = 0,
        };
        private readonly UxmlFloatAttributeDescription yLabelMargin = new()
        {
            name = "y-label-margin",
            defaultValue = 0,
        };
        private readonly UxmlFloatAttributeDescription xyzFlexBasis = new()
        {
            name = "xyz-flex-basis",
            defaultValue = 120,
        };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
            base.Init(ve, bag, cc);
            var myView = (MVector2Field)ve;
            myView.isReadOnly = isReadonly.GetValueFromBag(bag, cc);
            myView.isDelayed = isDelayed.GetValueFromBag(bag, cc);
            myView.xLabel = xLabel.GetValueFromBag(bag, cc);
            myView.yLabel = yLabel.GetValueFromBag(bag, cc);
            //
            myView.labelMargin = labelMargin.GetValueFromBag(bag, cc);
            myView.xLabelMargin = xLabelMargin.GetValueFromBag(bag, cc);
            myView.yLabelMargin = yLabelMargin.GetValueFromBag(bag, cc);
            myView.xyzFlexBasis = xyzFlexBasis.GetValueFromBag(bag, cc);
        }
    }
}
}