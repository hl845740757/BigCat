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
public class MColorField : ColorField, IPrefixLabel
{
    public MColorField() {
    }

    public MColorField(string label) : base(label) {
    }

    public float labelMargin {
        get => labelElement.style.marginRight.value.value;
        set => labelElement.style.marginRight = new Length(value);
    }

    public new class UxmlFactory : UxmlFactory<MColorField, UxmlTraits>
    {
    }

    public new class UxmlTraits : ColorField.UxmlTraits
    {
        private readonly UxmlFloatAttributeDescription labelMargin = new()
        {
            name = "label-margin",
            defaultValue = 0,
        };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
            base.Init(ve, bag, cc);
            var myView = (MColorField)ve;
            myView.labelMargin = labelMargin.GetValueFromBag(bag, cc);
        }
    }
}
}