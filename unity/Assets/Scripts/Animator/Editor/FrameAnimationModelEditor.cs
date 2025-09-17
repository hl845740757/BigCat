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
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Wjybxx.BigCat.Animator;
using Wjybxx.BigCat.Util;
using Wjybxx.Commons.Collections;

namespace Wjybxx.BigCat.AnimatorEditor
{
/// <summary>
/// 2D帧动画模型编辑器
/// </summary>
public class FrameAnimationModelEditor : EditorWindow
{
    /// <summary>
    /// 逻辑层数据
    /// (本计划拆分多个类绘制的，好像也没有那么复杂)
    /// </summary>
    private DataModel _dataModel = new DataModel();
    /// <summary>
    /// 上次的工作目录，使用静态字段模拟持久化
    /// </summary>
    private static string _lastWorkDir;

    // 垂直和水平分隔选项
    private GUILayoutOption[] _vSpaceOptions;
    private GUILayoutOption[] _hSpaceOptions;

    // 文件列表区
    private GUILayoutOption[] _fileListAreaOptions;
    private GUILayoutOption[] _fileListOptions;
    private GUILayoutOption[] _playListOptions;
    private Vector2 _fileListScrollPos;
    private Vector2 _playListScrollPos;
    private bool _fileListFoldout = true;
    private bool _playListFoldout = true;

    // 右侧属性区
    private GUILayoutOption[] _propertyAreaOptions;
    private GUILayoutOption[] _width100;
    private GUILayoutOption[] _width50;
    private readonly GUIContent _pooledLabel = new GUIContent();

    private Vector2 _actionListScrollPos;
    private Vector2 _mixerListScrollPos;
    private bool _modelActionFoldOut = true;
    private bool _modelMixerFoldOut = true;

    private GameObject _rootObject; // 模型挂载的父节点
    private FrameAnimationPreviewer _rootPreviewer;


    [MenuItem("Window/BigCat/FAnimModelEditor")]
    private static void OpenWindow() {
        FrameAnimationModelEditor win = GetWindow<FrameAnimationModelEditor>("模型编辑器");
        win.minSize = new Vector2(300, 300);
        win.Show();
        // win.Init();
    }

    private GUIContent PooledLabel() => _pooledLabel.Reset();

    private void Awake() {
        _vSpaceOptions = new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(10) };
        _hSpaceOptions = new GUILayoutOption[] { GUILayout.ExpandHeight(true), GUILayout.Width(10) };

        _fileListAreaOptions = new GUILayoutOption[] { GUILayout.MinWidth(300), GUILayout.MaxWidth(600) };
        _fileListOptions = new GUILayoutOption[] { GUILayout.MinHeight(300) };
        _playListOptions = new GUILayoutOption[] { GUILayout.MinHeight(300) };

        _propertyAreaOptions = new GUILayoutOption[] { GUILayout.MinWidth(300), GUILayout.MaxWidth(800) };
        _width100 = new GUILayoutOption[] { GUILayout.MaxWidth(100) };
        _width50 = new GUILayoutOption[] { GUILayout.MaxWidth(50) };
    }

    private void OnEnable() {
        // 放在OnEnable方便Debug
        _dataModel.workDir = _lastWorkDir ?? Application.dataPath + "/Resources/";
        _rootPreviewer = new FrameAnimationPreviewer(null);
        RefreshWorkDir();
    }

    private void OnDisable() {
        _rootObject = null;
    }

    private void Update() {
    }

    private void OnGUI() {
        EditorGUILayout.BeginHorizontal();
        // 左侧列表
        EditorGUILayout.BeginVertical(_fileListAreaOptions);
        DrawFileListArea();
        GUILayout.Box("", _vSpaceOptions);
        // EditorGUILayout.Space(10);
        DrawPlayListArea();
        EditorGUILayout.EndVertical();
        GUILayout.Box("", _hSpaceOptions);
        // EditorGUILayout.Space(10);
        // 中部预览区
        // 右侧属性区
        EditorGUILayout.BeginVertical(_propertyAreaOptions);
        DrawPropertyArea();
        // GUILayout.Box("", _hSpaceOptions);
        DrawPreviewerArea();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawSeparator() {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, Color.gray);
        // EditorGUILayout.LabelField(SEPARATOR);
    }

    private static void DrawSeparator(Color color) {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, color);
    }

    #region draw-file-list

    /// <summary>
    /// 绘制文件列表
    /// </summary>
    private void DrawFileListArea() {
        EditorGUILayout.HelpBox(PooledLabel().WithText("文件列表区"));
        EditorGUILayout.BeginVertical(_fileListOptions);
        // 工作目录
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("工作目录:");
        if (GUILayout.Button("选择") && Event.current.button == 0) {
            string folderPath = EditorUtility.OpenFolderPanel("选择工作目录", _dataModel.workDir, "");
            if (!string.IsNullOrEmpty(folderPath)) {
                ChangeWorkDir(folderPath);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.SelectableLabel(_dataModel.workDir);
        EditorGUILayout.Space(10, true);

        // 创建新资产
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = !string.IsNullOrEmpty(_dataModel.workDir);
        _dataModel.newAssetName = EditorGUILayout.TextField(_dataModel.newAssetName);
        if (GUILayout.Button("创建")) {
            CreateNewModel();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10, true);

        // 模型列表
        EditorGUILayout.BeginHorizontal();
        _fileListFoldout = EditorGUILayout.Foldout(_fileListFoldout, _fileListFoldout ? "收起" : "展开");
        if (GUILayout.Button("刷新") && Event.current.button == 0) {
            RefreshWorkDir();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10, true);

        if (_fileListFoldout) {
            _fileListScrollPos = EditorGUILayout.BeginScrollView(_fileListScrollPos, false, false);
            DrawWorkList();
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawWorkList() {
        int deleteIndex = -1;
        List<FrameAnimationModel> modelList = _dataModel.workModelList;
        for (int index = 0; index < modelList.Count; index++) {
            FrameAnimationModel model = modelList[index];
            EditorGUILayout.BeginHorizontal();
            // 只读
            EditorGUILayout.ObjectField(model, typeof(FrameAnimationModel), false);
            if (GUILayout.Button("删除") && Event.current.button == 0) {
                deleteIndex = index;
            }
            //
            if (model && model == _dataModel.selectedModel) {
                if (GUILayout.Button("关闭")) {
                    _dataModel.selectedModel = null;
                }
            } else {
                if (GUILayout.Button("编辑")) {
                    _dataModel.selectedModel = model;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (model && model == _dataModel.selectedModel) {
                DrawSeparator(Color.yellow);
            }
        }
        // 循环外处理删除
        if (deleteIndex >= 0
            && EditorUtility.DisplayDialog("", "确定删除?", "确定", "取消")) {
            FrameAnimationModel model = modelList[deleteIndex];
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(model));
            modelList.RemoveAt(deleteIndex);
            Repaint();
        }
    }

    private void CreateNewModel() {
        if (string.IsNullOrEmpty(_dataModel.workDir)) {
            return;
        }
        string assetName = _dataModel.newAssetName;
        if (string.IsNullOrWhiteSpace(assetName)) {
            return;
        }
        assetName = assetName.Trim();
        string assetPath = SystemExtensions.ConvertToAssetPath(_dataModel.workDir) + "/" + assetName + ".asset";
        if (AssetDatabase.LoadAssetAtPath<FrameAnimationModel>(assetPath)) {
            EditorUtility.DisplayDialog("错误", $"资产{assetName}已存在", "关闭");
            return;
        }
        // _dataModel.newAssetName = "";
        try {
            FrameAnimationModel model = CreateInstance<FrameAnimationModel>();
            AssetDatabase.CreateAsset(model, assetPath);
            _dataModel.workModelList.Add(model);
            Repaint();
        }
        catch (Exception ex) {
            Debug.LogError(ex);
        }
    }

    private void ChangeWorkDir(string folderPath) {
        if (folderPath == _dataModel.workDir) {
            return;
        }
        _dataModel.workDir = folderPath;
        _lastWorkDir = folderPath;
        RefreshWorkDir();
    }

    private void RefreshWorkDir() {
        if (string.IsNullOrWhiteSpace(_dataModel.workDir)) {
            return;
        }
        _dataModel.workModelList.Clear();
        foreach (string filePath in Directory.GetFiles(_dataModel.workDir)) {
            if (!filePath.EndsWith(".asset")) {
                continue;
            }
            string assetPath = SystemExtensions.ConvertToAssetPath(filePath);
            if (AssetDatabase.LoadAssetAtPath(assetPath, typeof(FrameAnimationModel)) is FrameAnimationModel model) {
                _dataModel.workModelList.Add(model);
            }
        }
    }

    #endregion

    #region draw-play-list

    private void DrawPlayListArea() {
        // 顶部条
        EditorGUILayout.HelpBox(PooledLabel().WithText("多模型工具区：(拖拽到列表区添加)"));
        EditorGUILayout.BeginVertical(_playListOptions);
        //
        EditorGUILayout.BeginHorizontal();
        _playListFoldout = EditorGUILayout.Foldout(_playListFoldout, _playListFoldout ? "收起" : "展开");
        if (GUILayout.Button("清空") && Event.current.button == 0) {
            _dataModel.playModelList.Clear();
            _rootPreviewer.Stop();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10, true);

        // 模型列表
        if (_playListFoldout) {
            _playListScrollPos = EditorGUILayout.BeginScrollView(_playListScrollPos, false, false);
            DrawPlayList();
            EditorGUILayout.EndScrollView();
        }
        Rect controlRect = GUILayoutUtility.GetLastRect();

        // 功能列表
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("同步Action") && Event.current.button == 0) {
            SyncActionList();
        }
        if (GUILayout.Button("同步Action偏移") && Event.current.button == 0) {
            SyncActionOffset();
        }
        if (GUILayout.Button("同步MixCfg") && Event.current.button == 0) {
            SyncMixCfg();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        //
        CheckDragAddEvent(controlRect);
    }

    private void DrawPlayList() {
        int deleteIndex = -1;
        int moveTopIndex = -1;
        List<FrameAnimationModel> modelList = _dataModel.playModelList;
        for (int index = 0; index < modelList.Count; index++) {
            FrameAnimationModel model = modelList[index];
            EditorGUILayout.BeginHorizontal();
            model = EditorGUILayout.ObjectField(model, typeof(FrameAnimationModel), false) as FrameAnimationModel;
            modelList[index] = model;
            //
            if (GUILayout.Button("删除") && Event.current.button == 0) {
                deleteIndex = index;
            }
            GUI.enabled = index > 0;
            if (GUILayout.Button("置顶")) {
                moveTopIndex = index;
            }
            GUI.enabled = true;
            //
            if (model && model == _dataModel.selectedModel) {
                if (GUILayout.Button("关闭")) {
                    _dataModel.selectedModel = null;
                }
            } else {
                if (GUILayout.Button("编辑")) {
                    _dataModel.selectedModel = model;
                }
            }
            EditorGUILayout.EndHorizontal();
            if (model && model == _dataModel.selectedModel) {
                DrawSeparator(Color.yellow);
            }
        }
        // 循环外处理删除
        if (deleteIndex >= 0) {
            modelList.RemoveAt(deleteIndex);
            Repaint();
        }
        if (moveTopIndex >= 0) {
            FrameAnimationModel model = modelList[moveTopIndex];
            modelList.RemoveAt(moveTopIndex);
            modelList.Insert(0, model);
            Repaint();
        }
    }

    private void CheckDragAddEvent(Rect controlRect) {
        Event evt = Event.current;
        if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;
        if (!controlRect.Contains(evt.mousePosition)) return;
        //
        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
        if (evt.type != EventType.DragPerform) return;
        // 拖拽结束 - path是文件全路径
        foreach (string filePath in DragAndDrop.paths) {
            string assetPath = SystemExtensions.ConvertToAssetPath(filePath);
            if (AssetDatabase.LoadAssetAtPath(assetPath, typeof(FrameAnimationModel)) is FrameAnimationModel model
                && !_dataModel.playModelList.Contains(model)) {
                _dataModel.playModelList.Add(model);
            }
        }
    }

    private void SyncActionList() {
        if (_dataModel.playModelList.Count <= 1) {
            return;
        }
        const string message = "该操作将同步第一个模型的Action设置到其它模型，只有Action都使用模型Clip时才可使用，确定同步吗？";
        if (!EditorUtility.DisplayDialog("二次确认", message, "确定", "取消")) {
            return;
        }
        FrameAnimationModel baseModel = _dataModel.playModelList[0];
        for (int index = 1; index < _dataModel.playModelList.Count; index++) {
            FrameAnimationModel animModel = _dataModel.playModelList[index];
            if (animModel == baseModel) continue;
            // 拷贝Action
            animModel.actionList.Clear();
            for (int i = 0; i < baseModel.actionList.Count; i++) {
                FrameAnimationAction copiedAction = new FrameAnimationAction(baseModel.actionList[i])
                {
                    clip = animModel.modelClip
                };
                animModel.actionList.Add(copiedAction);
            }
            EditorUtility.SetDirty(animModel);
        }
    }

    private void SyncActionOffset() {
        if (_dataModel.playModelList.Count <= 1) {
            return;
        }
        const string message = "该操作将同步第一个模型的Action偏移到其它模型，只有所有模型属于同一部件时才应该使用，确定同步吗？";
        if (!EditorUtility.DisplayDialog("二次确认", message, "确定", "取消")) {
            return;
        }
        FrameAnimationModel baseModel = _dataModel.playModelList[0];
        for (int index = 1; index < _dataModel.playModelList.Count; index++) {
            FrameAnimationModel animModel = _dataModel.playModelList[index];
            if (animModel == baseModel) continue;
            // 只拷贝Offset
            foreach (FrameAnimationAction baseAction in baseModel.actionList) {
                FrameAnimationAction targetAction = animModel.FindAction(baseAction.name);
                if (targetAction != null) {
                    targetAction.offset = baseAction.offset;
                }
            }
            EditorUtility.SetDirty(animModel);
        }
    }

    private void SyncMixCfg() {
        if (_dataModel.playModelList.Count <= 1) {
            return;
        }
        const string message = "该操作将同步第一个模型的MixCfg偏移到其它模型，这会覆盖其它模型的MixCfg，确定同步吗？";
        if (!EditorUtility.DisplayDialog("二次确认", message, "确定", "取消")) {
            return;
        }
        FrameAnimationModel baseModel = _dataModel.playModelList[0];
        for (int index = 1; index < _dataModel.playModelList.Count; index++) {
            FrameAnimationModel animModel = _dataModel.playModelList[index];
            if (animModel == baseModel) continue;
            // 拷贝MixCfg
            animModel.actionMixCfgList.Clear();
            for (int i = 0; i < baseModel.actionMixCfgList.Count; i++) {
                AnimationMixCfg copiedAction = new AnimationMixCfg(baseModel.actionMixCfgList[i]);
                animModel.actionMixCfgList.Add(copiedAction);
            }
            EditorUtility.SetDirty(animModel);
        }
    }

    #endregion

    #region draw-properties

    private void DrawPropertyArea() {
        EditorGUILayout.HelpBox(PooledLabel().WithText("模型编辑区:"));
        EditorGUILayout.BeginVertical();
        if (_dataModel.selectedModel) {
            EditorGUI.BeginChangeCheck();
            DrawProperty();
            if (EditorGUI.EndChangeCheck() && _dataModel.selectedModel) { // 可能被关闭
                EditorUtility.SetDirty(_dataModel.selectedModel);
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawProperty() {
        FrameAnimationModel model = _dataModel.selectedModel;
        bool closed = false;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.ObjectField(model, typeof(FrameAnimationModel), false);
        if (GUILayout.Button("关闭") && Event.current.button == 0) {
            closed = true;
        }
        EditorGUILayout.EndHorizontal();
        // 在绘制外处理事件
        if (closed) {
            _dataModel.selectedModel = null;
            return;
        }
        model.partId = EditorGUILayout.TextField(PooledLabel().WithText("PartId", "部件Id"), model.partId);
        model.partGroupId = EditorGUILayout.IntField(PooledLabel().WithText("PartGroupId", "部件组Id"), model.partGroupId);
        model.orderInLayer = EditorGUILayout.IntField(PooledLabel().WithText("OrderInLayer", "渲染顺序，越大越上层"), model.orderInLayer);
        model.modelClip = EditorGUILayout.ObjectField(PooledLabel().WithText("ModelClip", "模型完整动画，可选"), model.modelClip,
            typeof(FrameAnimationClip), false) as FrameAnimationClip;
        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("ActionList: (Name匹配时有彩蛋)");
        EditorGUILayout.BeginHorizontal();
        _modelActionFoldOut = EditorGUILayout.Foldout(_modelActionFoldOut, _modelActionFoldOut ? "收起" : "展开");
        EditorGUILayout.IntField(model.actionList.Count, _width100);

        _dataModel.newActionName = EditorGUILayout.TextField(_dataModel.newActionName);
        if (GUILayout.Button("创建") && Event.current.button == 0) CreateAction();
        if (GUILayout.Button("排序 ↑") && Event.current.button == 0) SortAction(1);
        if (GUILayout.Button("排序 ↓") && Event.current.button == 0) SortAction(-1);
        EditorGUILayout.EndHorizontal();
        DrawSeparator();
        EditorGUILayout.Space(5);

        if (_modelActionFoldOut) {
            _actionListScrollPos = EditorGUILayout.BeginScrollView(_actionListScrollPos, false, false);
            DrawActionList();
            EditorGUILayout.EndScrollView();

            DrawSeparator();
            EditorGUILayout.Space(5);
        }

        EditorGUILayout.LabelField("ActionMixCfgList: (Name匹配时有彩蛋)");
        EditorGUILayout.BeginHorizontal();
        _modelMixerFoldOut = EditorGUILayout.Foldout(_modelMixerFoldOut, _modelMixerFoldOut ? "收起" : "展开");
        EditorGUILayout.IntField(model.actionMixCfgList.Count, _width100);

        _dataModel.mixActionA = EditorGUILayout.TextField(_dataModel.mixActionA);
        _dataModel.mixActionB = EditorGUILayout.TextField(_dataModel.mixActionB);
        if (GUILayout.Button("创建") && Event.current.button == 0) CreateMixCfg();
        if (GUILayout.Button("排序 ↑") && Event.current.button == 0) SortMixCfg(1);
        if (GUILayout.Button("排序 ↓") && Event.current.button == 0) SortMixCfg(-1);
        EditorGUILayout.EndHorizontal();
        DrawSeparator();
        EditorGUILayout.Space(5);

        if (_modelMixerFoldOut) {
            _mixerListScrollPos = EditorGUILayout.BeginScrollView(_mixerListScrollPos, false, false);
            DrawMixerList();
            EditorGUILayout.EndScrollView();

            DrawSeparator();
            EditorGUILayout.Space(5);
        }
    }

    private void DrawActionList() {
        FrameAnimationModel model = _dataModel.selectedModel;
        int deleteIndex = -1;
        for (int index = 0; index < model.actionList.Count; index++) {
            if (index > 0) {
                DrawSeparator();
            }
            FrameAnimationAction modelAction = model.actionList[index];
            if (modelAction.name == _dataModel.newActionName) { // 醒目条
                Rect rect = EditorGUILayout.GetControlRect(false, 1);
                EditorGUI.DrawRect(rect, Color.yellow);
            }
            EditorGUILayout.BeginHorizontal();
            modelAction.name = EditorGUILayout.TextField("Name", modelAction.name);
            if (GUILayout.Button("Delete", _width100) && Event.current.button == 0) {
                deleteIndex = index;
            }
            EditorGUILayout.EndHorizontal();

            modelAction.clip = EditorGUILayout.ObjectField("Clip", modelAction.clip, typeof(FrameAnimationClip), false) as FrameAnimationClip;
            modelAction.startFrame = EditorGUILayout.IntField(PooledLabel().WithText("StartFrame", "动画起始帧，包含"), modelAction.startFrame);
            modelAction.endFrame = EditorGUILayout.IntField(PooledLabel().WithText("EndFrame", "动画结束帧，包含"), modelAction.endFrame);
            modelAction.weight = EditorGUILayout.FloatField(PooledLabel().WithText("Weight", "动画融合权重"), modelAction.weight);
            modelAction.offset = EditorGUILayout.Vector2Field(PooledLabel().WithText("Offset"), modelAction.offset);
        }
        // 循环外处理删除
        if (deleteIndex >= 0
            && EditorUtility.DisplayDialog("", "确定删除?", "确定", "取消")) {
            model.actionList.RemoveAt(deleteIndex);
            Repaint();
        }
    }

    private void CreateAction() {
        string actionName = _dataModel.newActionName;
        if (string.IsNullOrWhiteSpace(actionName)) {
            return;
        }
        actionName = actionName.Trim();
        FrameAnimationModel model = _dataModel.selectedModel;
        if (model.actionList.Any(e => e.name == actionName)) {
            EditorUtility.DisplayDialog("错误", "Action已存在", "关闭");
            return;
        }
        FrameAnimationAction action = new FrameAnimationAction();
        action.name = actionName;
        action.clip = model.modelClip;
        model.actionList.Add(action);
    }

    private void SortAction(int sign) {
        FrameAnimationModel model = _dataModel.selectedModel;
        model.actionList.Sort((actionA, actionB) => sign * string.Compare(actionA.name, actionB.name, StringComparison.OrdinalIgnoreCase));
    }

    private void DrawMixerList() {
        FrameAnimationModel model = _dataModel.selectedModel;
        int deleteIndex = -1;
        for (int index = 0; index < model.actionMixCfgList.Count; index++) {
            if (index > 0) {
                DrawSeparator();
            }
            AnimationMixCfg mixCfg = model.actionMixCfgList[index];
            if (!string.IsNullOrEmpty(mixCfg.actionA) && !string.IsNullOrEmpty(mixCfg.actionB)
                                                      && mixCfg.actionA == _dataModel.mixActionA
                                                      && mixCfg.actionB == _dataModel.mixActionB) {
                Rect rect = EditorGUILayout.GetControlRect(false, 1);
                EditorGUI.DrawRect(rect, Color.yellow); // 醒目条
            }
            EditorGUILayout.BeginHorizontal();
            mixCfg.actionA = EditorGUILayout.TextField("ActionA", mixCfg.actionA);
            mixCfg.weightA = EditorGUILayout.FloatField("Weight", mixCfg.weightA);
            if (GUILayout.Button("选择")) {
                ShowPickActionMenu(model, index, 0);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            mixCfg.actionB = EditorGUILayout.TextField("ActionB", mixCfg.actionB);
            mixCfg.weightB = EditorGUILayout.FloatField("Weight", mixCfg.weightB);
            if (GUILayout.Button("选择")) {
                ShowPickActionMenu(model, index, 1);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            mixCfg.crossFadeTime = EditorGUILayout.FloatField("CrossFadeTime", mixCfg.crossFadeTime);
            if (GUILayout.Button("Delete", _width100)) {
                deleteIndex = index;
            }
            EditorGUILayout.EndHorizontal();
        }
        // 循环外处理删除
        if (deleteIndex >= 0
            && EditorUtility.DisplayDialog("", "确定删除?", "确定", "取消")) {
            model.actionMixCfgList.RemoveAt(deleteIndex);
            Repaint();
        }
    }

    private void ShowPickActionMenu(FrameAnimationModel model, int index, int fieldIndex) {
        AnimationMixCfg mixCfg = model.actionMixCfgList[index];
        string currentActionName = fieldIndex == 0 ? mixCfg.actionA : mixCfg.actionB;
        // Mune不能使用池化的Label，否则会出异常
        GenericMenu menu = new GenericMenu();
        foreach (FrameAnimationAction action in model.actionList) {
            PickActionContext ctx = new PickActionContext(index, fieldIndex, action.name);
            menu.AddItem(new GUIContent(action.name), action.name == currentActionName, PickActionCallback, ctx);
        }
        menu.ShowAsContext();
    }

    private class PickActionContext
    {
        public readonly int index;
        public readonly int fieldIndex;
        public readonly string actionName;

        public PickActionContext(int index, int fieldIndex, string actionName) {
            this.index = index;
            this.fieldIndex = fieldIndex;
            this.actionName = actionName;
        }
    }

    private void PickActionCallback(object obj) {
        PickActionContext ctx = (PickActionContext)obj;
        AnimationMixCfg mixCfg = _dataModel.selectedModel.actionMixCfgList[ctx.index];
        if (ctx.fieldIndex == 0) {
            mixCfg.actionA = ctx.actionName;
        } else {
            mixCfg.actionB = ctx.actionName;
        }
    }

    private void CreateMixCfg() {
        FrameAnimationModel model = _dataModel.selectedModel;
        if (string.IsNullOrWhiteSpace(_dataModel.mixActionA) || string.IsNullOrWhiteSpace(_dataModel.mixActionB)) {
            model.actionMixCfgList.Add(new AnimationMixCfg());
            return;
        }
        // 有输入的情况下进行去重检测
        if (model.actionMixCfgList.Any(e => e.actionA == _dataModel.mixActionA && e.actionB == _dataModel.mixActionB)) {
            EditorUtility.DisplayDialog("通知", "目标配置已存在", "关闭");
            return;
        }
        model.actionMixCfgList.Add(new AnimationMixCfg()
        {
            actionA = _dataModel.mixActionA,
            actionB = _dataModel.mixActionB
        });
    }

    private void SortMixCfg(int sign) {
        FrameAnimationModel model = _dataModel.selectedModel;
        model.actionMixCfgList.Sort((a, b) => {
            int r = string.Compare(a.actionA, b.actionA, StringComparison.OrdinalIgnoreCase);
            if (r != 0) return sign * r;
            return sign * string.Compare(a.actionB, b.actionB, StringComparison.OrdinalIgnoreCase);
        });
    }

    #endregion

    #region draw-previewer

    private void DrawPreviewerArea() {

    }

    #endregion

    #region data-model

    private sealed class DataModel
    {
        /// <summary>
        /// 工作目录
        /// </summary>
        public string workDir;
        /// <summary>
        /// 当前编辑（选中）的动作模型，可能是工作目录下的，也可能协同播放目录下
        /// </summary>
        public FrameAnimationModel selectedModel;

        /// <summary>
        /// 创建新模型资产的名字
        /// </summary>
        public string newAssetName;
        /// <summary>
        /// 新动作名
        /// </summary>
        public string newActionName;

        public string mixActionA;
        public string mixActionB;
        /// <summary>
        /// 当前工作目录下的动画模型
        /// </summary>
        public List<FrameAnimationModel> workModelList = new List<FrameAnimationModel>();
        /// <summary>
        /// 协同播放的动画列表
        /// </summary>
        public List<FrameAnimationModel> playModelList = new List<FrameAnimationModel>();
        /// <summary>
        /// 当前要播放的动作
        /// 按模型分组播放
        /// </summary>
        public ArrayDictionary<int, string> group2Actions = new ArrayDictionary<int, string>(4);
    }

    private enum PlayMode
    {
        SelectModel, // 播放选中的模型动画
        SelectAction, // 播放选中的Action动画
        MultiModel, // 同步播放play列表中所有模型
        MultiAction // 同步播放play列表中所有模型的指定action
    }

    #endregion
}
}