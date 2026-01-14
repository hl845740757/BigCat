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

using System.IO;
using UnityEngine.UIElements;

namespace Wjybxx.BigCat.Editor.UIElements
{
/// <summary>
/// 资产路径字段
///
/// 1.包含两个小功能：对象定位提示，斜杠转换。
/// 2.更建议使用<see cref="ObjectPathField"/>。
/// </summary>
public class AssetPathField : TextField, IPrefixLabel
{
    private bool _isFolder;

    public AssetPathField() {
        RegisterCallback<MouseDownEvent>(OnMouseDown);
    }

    public AssetPathField(string label) : base(label) {
        RegisterCallback<MouseDownEvent>(OnMouseDown);
    }

    private void OnMouseDown(MouseDownEvent evt) {
        if (evt.localMousePosition.x > 80) { // 只响应标签部分
            return;
        }
        if (evt.button == 0) {
            UnityEditorUtil.PingObject(value);
            return;
        }
        if (evt.button == 1) {
            string tempPath;
            if (isFolder) {
                tempPath = UnityEditorUtil.OpenFolderPanel("选择", value);
            } else {
                string directory = value.LastIndexOf('/') > 0 ? Path.GetDirectoryName(value) : value;
                tempPath = UnityEditorUtil.OpenFilePanel("选择", directory);
            }
            if (!string.IsNullOrEmpty(tempPath)) {
                value = UnityEditorUtil.ConvertToAssetPath(tempPath);
            }
        }
    }

    public override string value {
        get => base.value;
        set => base.value = RepairValue(value);
    }

    // 不处理SetValueWithoutNotify，因为调用SetValueWithoutNotify的通常是受信结果
    private static string RepairValue(string newValue) {
        return string.IsNullOrWhiteSpace(newValue) ? "" : newValue.Replace('\\', '/');
    }

    public bool isFolder {
        get => _isFolder;
        set => _isFolder = value;
    }

    public float labelMargin {
        get => labelElement.style.marginRight.value.value;
        set => labelElement.style.marginRight = new Length(value);
    }

    public new class UxmlFactory : UxmlFactory<AssetPathField, UxmlTraits>
    {
    }

    public new class UxmlTraits : TextField.UxmlTraits
    {
        private readonly UxmlBoolAttributeDescription isFolderAttribute = new()
        {
            name = "isFolder",
            defaultValue = false,
        };
        private readonly UxmlFloatAttributeDescription labelMargin = new()
        {
            name = "label-margin",
            defaultValue = 0,
        };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
            base.Init(ve, bag, cc);
            var myView = (AssetPathField)ve;
            myView.isFolder = isFolderAttribute.GetValueFromBag(bag, cc);
            myView.labelMargin = labelMargin.GetValueFromBag(bag, cc);
        }
    }
}
}