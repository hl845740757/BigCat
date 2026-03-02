#region LICENSE

// Copyright 2026 wjybxx(845740757@qq.com)
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

namespace Wjybxx.BigCat.Editor.UIElements
{
public class MRectField : RectField, IPrefixLabel
{
    private bool _isReadOnly;
    private bool _isDelayed;

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

    public new class UxmlFactory : UxmlFactory<MRectField, UxmlTraits>
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

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
            base.Init(ve, bag, cc);
            var myView = (MRectField)ve;
            myView.isReadOnly = isReadonly.GetValueFromBag(bag, cc);
            myView.isDelayed = isDelayed.GetValueFromBag(bag, cc);
        }
    }
}
}