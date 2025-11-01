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
/// 
/// </summary>
public class ObjectPathField : BindableElement, INotifyValueChanged<ObjectPath>, IPrefixLabel
{
    private TextField _collectionField;
    private TextField _localPathField;
    private LongField _localIdField;
    private IntegerField _typeField;
    private Toggle _lockTypeField;

    private Foldout _foldout;
    private Button _selectCollectionButton;
    private Button _selectLocalPathButton;
    private Button _selectLocalIdButton;

    private ObjectPath _value;
    private bool _rebuildingValue;
    private ObjectPathHandler _handler; // 请通过属性访问

    public ObjectPathField() {

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

    /// <summary>
    /// 注意：由于Unity的初始化顺序限制，在刚创建的时候不能读取正确的值。
    /// </summary>
    public ObjectPath value {
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
            using (ChangeEvent<ObjectPath> pooled = ChangeEvent<ObjectPath>.GetPooled(_value, value)) {
                pooled.target = this;
                this.SetValueWithoutNotify(value);
                this.SendEvent(pooled);
            }
        }
    }

    public void SetValueWithoutNotify(ObjectPath newValue) {
        EnsureInited();
        // Type变化时需要重建Handler
        if (_value.type != newValue.type) {
            _handler = CreateHandler(newValue.type);
            _handler.InitView();
            // MarkDirtyRepaint();
        }
        _value = newValue;
        if (_rebuildingValue) {
            return;
        }
        _collectionField.SetValueWithoutNotify(value.collection);
        _localPathField.SetValueWithoutNotify(value.localPath);
        _localIdField.SetValueWithoutNotify(value.localId);
        _typeField.SetValueWithoutNotify(value.type);
    }

    /// <summary>
    /// 获取实时值
    /// </summary>
    public ObjectPath GetRealtimeValue() {
        EnsureInited();
        return new ObjectPath(_collectionField.value,
            _localPathField.value,
            _localIdField.value,
            _typeField.value);
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

    private ObjectPathHandler handler => _handler;

    private void EnsureInited() {
        if (childCount == 0 || _foldout != null) {
            return; // Tip创建的临时对象或已初始化
        }
        _foldout = this.Q<Foldout>();
        _collectionField = this.Q<TextField>("collection");
        _localPathField = this.Q<TextField>("local-path");
        _localIdField = this.Q<LongField>("local-id");
        _typeField = this.Q<IntegerField>("type");

        _selectCollectionButton = this.Q<Button>("select-collection");
        _selectLocalPathButton = this.Q<Button>("select-local-path");
        _selectLocalIdButton = this.Q<Button>("select-local-id");
        _lockTypeField = this.Q<Toggle>("lock-type");
        //
        _typeField.SetEnabled(!_lockTypeField.value);
        _handler = CreateHandler(_typeField.value);
        _handler.InitView();
        // 数据变化事件
        _collectionField.RegisterValueChangedCallback(OnFieldValueChanged);
        _localPathField.RegisterValueChangedCallback(OnFieldValueChanged);
        _localIdField.RegisterValueChangedCallback(OnFieldValueChanged);
        _typeField.RegisterValueChangedCallback(OnFieldValueChanged);
        _lockTypeField.RegisterValueChangedCallback(evt => {
            evt.StopPropagation();
            _typeField.SetEnabled(!_lockTypeField.value);
        });
        //
        _collectionField.RegisterCallback<MouseDownEvent>(evt => {
            evt.StopPropagation();
            handler.PingCollection();
        });
        _selectCollectionButton.RegisterCallback<ClickEvent>(evt => {
            evt.StopPropagation();
            handler.OnClickSelectCollection();
        });
        _selectLocalPathButton.RegisterCallback<ClickEvent>(evt => {
            evt.StopPropagation();
            handler.OnClickSelectLocalPath();
        });
        _selectLocalIdButton.RegisterCallback<ClickEvent>(evt => {
            evt.StopPropagation();
            handler.OnClickSelectLocalId();
        });
        //
        RebuildValue(false);
    }

    private void OnFieldValueChanged<T>(ChangeEvent<T> evt) {
        evt.StopPropagation();
        RebuildValue();
    }

    private ObjectPathHandler CreateHandler(int type) {
        ObjectPathType pathType = (ObjectPathType)type;
        return pathType switch
        {
            ObjectPathType.SpriteOfGroup => new SpriteOfGroupHandler(this),
            _ => new DefaultPathHandler(this),
        };
    }

    #region handlers

    private abstract class ObjectPathHandler
    {
        internal readonly ObjectPathField view;

        protected ObjectPathHandler(ObjectPathField view) {
            this.view = view;
        }

        public abstract void InitView();

        public abstract void OnClickSelectCollection();

        public abstract void OnClickSelectLocalPath();

        public abstract void OnClickSelectLocalId();

        public abstract void PingCollection();
    }

    private class DefaultPathHandler : ObjectPathHandler
    {
        public DefaultPathHandler(ObjectPathField view)
            : base(view) {
        }

        public override void InitView() {
            view._localPathField.label = "LocalPath";
            view._localIdField.label = "LocalId";
            //
            view._localPathField.SetEnabled(true);
            view._localIdField.SetEnabled(true);
            view._selectLocalPathButton.SetEnabled(false);
            view._selectLocalIdButton.SetEnabled(false);
        }

        public override void OnClickSelectCollection() {
            string filePath = EditorUtility.OpenFilePanel("选择资产", UnityEditorUtil.lastOpenFolder, "");
            if (string.IsNullOrEmpty(filePath)) {
                return;
            }
            string assetPath = UnityEditorUtil.ConvertToAssetPath(filePath);
            view._collectionField.value = assetPath;
            UnityEditorUtil.lastOpenFolder = UnityEditorUtil.GetAssetFolderPath(assetPath);
        }

        public override void OnClickSelectLocalPath() {
        }

        public override void OnClickSelectLocalId() {
        }

        public override void PingCollection() {
            UnityEditorUtil.PingObject(view._collectionField.value);
        }
    }

    private class SpriteOfGroupHandler : ObjectPathHandler
    {
        public SpriteOfGroupHandler(ObjectPathField view)
            : base(view) {
        }

        public override void InitView() {
            view._localIdField.label = "Index";
            //
            view._localPathField.SetEnabled(false);
            view._localIdField.SetEnabled(true);
            view._selectLocalPathButton.SetEnabled(false);
            view._selectLocalIdButton.SetEnabled(true);
        }

        public override void OnClickSelectCollection() {
            string groupPtah = view._collectionField.value;
            SpriteGroup spriteGroup = UnityEditorUtil.LoadSpriteGroup(groupPtah);
            string groupAssetFolder = spriteGroup ? UnityEditorUtil.GetAssetFolderPath(spriteGroup) : UnityEditorUtil.spriteSearchFolders[0];
            string filePath = EditorUtility.OpenFilePanel("选择SpriteGroup", groupAssetFolder, "asset");
            if (string.IsNullOrEmpty(filePath)) {
                return;
            }
            string assetPath = UnityEditorUtil.ConvertToAssetPath(filePath);
            UnityEditorUtil.lastOpenFolder = UnityEditorUtil.GetAssetFolderPath(assetPath);

            spriteGroup = AssetDatabase.LoadAssetAtPath<SpriteGroup>(assetPath);
            if (spriteGroup) {
                groupPtah = spriteGroup.preferName ? spriteGroup.name : assetPath;
            } else {
                groupPtah = null;
            }
            view._collectionField.value = groupPtah;
        }

        public override void OnClickSelectLocalPath() {
            // 不应该触发
        }

        public override void OnClickSelectLocalId() {
            string groupPtah = view._collectionField.value;
            SpriteGroup spriteGroup = UnityEditorUtil.LoadSpriteGroup(groupPtah);
            if (!spriteGroup) {
                return;
            }
            string groupAssetFolder = UnityEditorUtil.GetAssetFolderPath(spriteGroup);
            string filePath = EditorUtility.OpenFilePanel("选择图片", groupAssetFolder, "png");
            if (string.IsNullOrEmpty(filePath)) {
                return;
            }
            string assetPath = UnityEditorUtil.ConvertToAssetPath(filePath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite) {
                view._localIdField.value = spriteGroup.IndexOf(sprite.name);
            } else {
                view._localIdField.value = 0;
            }
        }

        public override void PingCollection() {
            string groupPtah = view._collectionField.value;
            SpriteGroup spriteGroup = UnityEditorUtil.LoadSpriteGroup(groupPtah);
            if (spriteGroup) {
                EditorGUIUtility.PingObject(spriteGroup);
            }
        }
    }

    #endregion

    #region uxml

    private const string UXML_PATH = "Assets/Scripts/Core/Editor/UIElements/ObjectPathField.uxml";

    public static ObjectPathField Create() {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML_PATH);
        ObjectPathField field = (ObjectPathField)visualTree.CloneTree()[0];
        field.SetValueWithoutNotify(default); // xml中可能有默认值
        return field;
    }

    public new class UxmlFactory : UxmlFactory<ObjectPathField, UxmlTraits>
    {
    }

    public new class UxmlTraits : BindableElement.UxmlTraits
    {
        // 初始化方法：将 UXML 属性值赋给元素实例
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
            base.Init(ve, bag, cc);
            // 这里有大坑：现在还不能访问到子节点，因此发布的对象可能是未完全构造的，不能提供服务
            var myView = (ObjectPathField)ve;
            ve.schedule.Execute(() => { myView.EnsureInited(); }).StartingIn(0);
        }
    }

    #endregion
}
}