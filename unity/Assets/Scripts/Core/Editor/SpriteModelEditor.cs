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
using Wjybxx.BigCat.UnityCore;
using Object = UnityEngine.Object;

namespace Wjybxx.BigCat.AnimatorEditor
{
/// <summary>
/// 2D帧动画模型编辑器
/// </summary>
public class SpriteModelEditor : EditorWindow
{
    /// <summary>
    /// 逻辑层数据
    /// (本计划拆分多个类绘制的，好像也没有那么复杂)
    /// </summary>
    private readonly DataModel _dataModel = new DataModel();
    private static string _lastWorkDir; // 静态字段保留历史记录

    // 垂直和水平分隔选项
    private GUILayoutOption[] _vSpaceOptions;
    private GUILayoutOption[] _hSpaceOptions;

    // 文件列表区
    private GUILayoutOption[] _fileListAreaOptions;
    private GUILayoutOption[] _fileListOptions;
    private GUILayoutOption[] _syncListOptions;
    private Vector2 _fileListScrollPos;
    private Vector2 _syncListScrollPos;
    private bool _fileListFoldout = true;
    private bool _syncListFoldout = true;

    // 右侧属性区
    private GUILayoutOption[] _propertyAreaOptions;
    private readonly GUIContent _pooledLabel = new GUIContent();
    private GUILayoutOption[] _width150;
    private GUILayoutOption[] _width100;
    private GUILayoutOption[] _width50;

    private Vector2 _motionListScrollPos;
    private Vector2 _mixerListScrollPos;
    private bool _motionListFoldOut = true;
    private bool _mixerListFoldOut = false;

    [MenuItem("Window/BigCat/SpriteModelEditor")]
    private static void OpenWindow() {
        SpriteModelEditor win = GetWindow<SpriteModelEditor>("模型编辑器");
        win.minSize = new Vector2(300, 300);
        win.Show();
        // win.Init();
    }

    private GUIContent PooledLabel() => _pooledLabel.Reset();

    private void Awake() {
        _vSpaceOptions = new[] { GUILayout.Height(10), GUILayout.ExpandWidth(true) };
        _hSpaceOptions = new[] { GUILayout.Width(10), GUILayout.ExpandHeight(true) };

        _fileListAreaOptions = new[] { GUILayout.MinWidth(300), GUILayout.MaxWidth(600) };
        _fileListOptions = new[] { GUILayout.MinHeight(300) };
        _syncListOptions = new[] { GUILayout.MinHeight(300) };

        _propertyAreaOptions = new[] { GUILayout.MinWidth(300), GUILayout.MaxWidth(800) };
        _width150 = new[] { GUILayout.MaxWidth(150) };
        _width100 = new[] { GUILayout.MaxWidth(100) };
        _width50 = new[] { GUILayout.MaxWidth(50) };
    }

    private void OnEnable() {
        // 放在OnEnable方便Debug
        _dataModel.workDir = _lastWorkDir ?? Application.dataPath + "/Resources/";
        RefreshWorkDir();
    }

    // private void OnDisable() {
    // }

    private void OnGUI() {
        EditorGUILayout.BeginHorizontal();
        // 左侧文件列表
        EditorGUILayout.BeginVertical(_fileListAreaOptions);
        DrawFileListArea();
        GUILayout.Box("", _vSpaceOptions);
        DrawSyncListArea();
        EditorGUILayout.EndVertical();
        //
        GUILayout.Box("", _hSpaceOptions);
        // 右侧编辑区
        EditorGUILayout.BeginVertical(_propertyAreaOptions);
        DrawModelPropertyArea();
        EditorGUILayout.EndVertical();
        //
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
            GUIUtility.ExitGUI();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.SelectableLabel(_dataModel.workDir);
        EditorGUILayout.Space(10, true);

        // 创建新资产
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = !string.IsNullOrEmpty(_dataModel.workDir);
        _dataModel.newAssetName = EditorGUILayout.TextField(_dataModel.newAssetName);
        if (GUILayout.Button("创建Model", _width150)) CreateModel();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10, true);

        // 模型列表
        EditorGUILayout.BeginHorizontal();
        _fileListFoldout = EditorGUILayout.Foldout(_fileListFoldout, "");
        if (GUILayout.Button("刷新") && Event.current.button == 0) {
            RefreshWorkDir();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10, true);

        if (_fileListFoldout) {
            _fileListScrollPos = EditorGUILayout.BeginScrollView(_fileListScrollPos, false, false);
            DrawFileList();
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawFileList() {
        int deleteIndex = -1;
        List<SpriteModel> assetList = _dataModel.workModelList;
        for (int index = 0; index < assetList.Count; index++) {
            SpriteModel model = assetList[index];
            // 对象框和功能按钮
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(model, typeof(SpriteModel), false);
            if (GUILayout.Button("删除", _width100)) {
                deleteIndex = index;
            }
            if (model == _dataModel.selectedModel) {
                if (GUILayout.Button("关闭", _width100)) {
                    _dataModel.selectedModel = null;
                }
            } else {
                if (GUILayout.Button("编辑", _width100)) {
                    _dataModel.selectedModel = model;
                }
            }
            EditorGUILayout.EndHorizontal();
            // 选中高亮提示
            if (IsSelectedObject(model)) {
                DrawSeparator(Color.yellow);
            }
        }
        // 循环外处理删除
        if (deleteIndex >= 0
            && EditorUtility.DisplayDialog("", "确定删除？", "确定", "取消")) {
            Object assetObject = assetList[deleteIndex];
            assetList.RemoveAt(deleteIndex);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(assetObject));
            Repaint();
        }
    }

    private bool IsSelectedObject(Object obj) {
        return obj && obj == _dataModel.selectedModel;
    }

    private void CreateModel() {
        if (string.IsNullOrEmpty(_dataModel.workDir)) {
            return;
        }
        string assetName = _dataModel.newAssetName;
        if (string.IsNullOrWhiteSpace(assetName)) {
            return;
        }
        assetName = assetName.Trim();
        string assetPath = UnityHelper.ConvertToAssetPath(_dataModel.workDir) + "/" + assetName + ".asset";
        if (AssetDatabase.LoadAssetAtPath<Object>(assetPath)) {
            EditorUtility.DisplayDialog("错误", $"资产{assetName}已存在", "关闭");
            return;
        }
        // _dataModel.newAssetName = "";
        try {
            SpriteModel assetObject = CreateInstance<SpriteModel>();
            assetObject.name = assetName;
            _dataModel.workModelList.Add(assetObject);
            AssetDatabase.CreateAsset(assetObject, assetPath);
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
        Repaint();
    }

    private void RefreshWorkDir() {
        _dataModel.workModelList.Clear();
        if (string.IsNullOrWhiteSpace(_dataModel.workDir)) {
            return;
        }
        foreach (string filePath in Directory.GetFiles(_dataModel.workDir)) {
            if (!filePath.EndsWith(".asset")) {
                continue;
            }
            string assetPath = UnityHelper.ConvertToAssetPath(filePath);
            if (AssetDatabase.LoadAssetAtPath(assetPath, typeof(SpriteModel)) is SpriteModel model) {
                _dataModel.workModelList.Add(model);
            }
        }
    }

    #endregion

    #region draw-sync-list

    private void DrawSyncListArea() {
        // 顶部条
        EditorGUILayout.HelpBox(PooledLabel().WithText("多模型工具区：(拖拽到列表区添加)"));
        EditorGUILayout.BeginVertical(_syncListOptions);
        //
        EditorGUILayout.BeginHorizontal();
        _syncListFoldout = EditorGUILayout.Foldout(_syncListFoldout, "");
        if (GUILayout.Button("清空") && Event.current.button == 0) {
            _dataModel.syncModelList.Clear();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10, true);

        // 模型列表
        if (_syncListFoldout) {
            _syncListScrollPos = EditorGUILayout.BeginScrollView(_syncListScrollPos, false, false);
            DrawSyncList();
            EditorGUILayout.EndScrollView();
        }
        // 拖拽添加
        Rect controlRect = GUILayoutUtility.GetLastRect();
        CheckAddSyncModel(controlRect);

        // 功能列表
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("同步Motion") && Event.current.button == 0) {
            SyncMotionList();
            GUIUtility.ExitGUI();
        }
        if (GUILayout.Button("同步MixCfg") && Event.current.button == 0) {
            SyncMixCfg();
            GUIUtility.ExitGUI();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawSyncList() {
        int deleteIndex = -1;
        int moveTopIndex = -1;
        List<SpriteModel> modelList = _dataModel.syncModelList;
        for (int index = 0; index < modelList.Count; index++) {
            SpriteModel model = modelList[index];
            EditorGUILayout.BeginHorizontal();
            model = EditorGUILayout.ObjectField(model, typeof(SpriteModel), false) as SpriteModel;
            modelList[index] = model;
            //
            if (GUILayout.Button("删除", _width100)) {
                deleteIndex = index;
            }
            GUI.enabled = index > 0;
            if (GUILayout.Button("置顶", _width100)) {
                moveTopIndex = index;
            }
            GUI.enabled = true;
            //
            if (model && model == _dataModel.selectedModel) {
                if (GUILayout.Button("关闭", _width100)) {
                    _dataModel.selectedModel = null;
                }
            } else {
                if (GUILayout.Button("编辑", _width100)) {
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
            SpriteModel model = modelList[moveTopIndex];
            modelList.RemoveAt(moveTopIndex);
            modelList.Insert(0, model);
            Repaint();
        }
    }

    private void CheckAddSyncModel(Rect controlRect) {
        Event evt = Event.current;
        if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;
        if (!controlRect.Contains(evt.mousePosition)) return;
        //
        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
        if (evt.type != EventType.DragPerform) return;
        // 拖拽结束 - path是文件全路径
        foreach (string filePath in DragAndDrop.paths) {
            string assetPath = UnityHelper.ConvertToAssetPath(filePath);
            if (AssetDatabase.LoadAssetAtPath(assetPath, typeof(SpriteModel)) is SpriteModel model
                && !_dataModel.syncModelList.Contains(model)) {
                _dataModel.syncModelList.Add(model);
            }
        }
    }

    private void SyncMotionList() {
        List<SpriteModel> modelList = _dataModel.syncModelList;
        if (modelList.Count <= 1) {
            return;
        }
        const string message = "该操作将同步第一个模型的Motion映射信息到其它模型，确定同步吗？";
        if (!EditorUtility.DisplayDialog("二次确认", message, "确定", "取消")) {
            return;
        }
        // 需要指定模型的动画资源目录
        SpriteModel baseModel = modelList[0];
        for (int index = 1; index < modelList.Count; index++) {
            SpriteModel animModel = modelList[index];
            string animDir = EditorUtility.OpenFolderPanel("选择动画目录：" + animModel.name,
                UnityHelper.GetAssetFolderPath(animModel), "");
            if (string.IsNullOrWhiteSpace(animDir)) {
                continue;
            }
            animModel.motionList.Clear();
            animDir = UnityHelper.ConvertToAssetPath(animDir);
            for (int j = 0; j < baseModel.motionList.Count; j++) {
                SpriteMotionRedir motionRedir = baseModel.motionList[j];
                if (motionRedir.clip) {
                    string clipAssetPath = animDir + "/" + motionRedir.clip.name + ".asset";
                    motionRedir.clip = AssetDatabase.LoadAssetAtPath<SpriteAnimationClip>(clipAssetPath);
                }
                animModel.motionList.Add(motionRedir);
            }
            EditorUtility.SetDirty(animModel);
        }
    }

    private void SyncMixCfg() {
        List<SpriteModel> modelList = _dataModel.syncModelList;
        if (modelList.Count <= 1) {
            return;
        }
        const string message = "该操作将同步第一个模型的MixCfg偏移到其它模型，这会覆盖其它模型的MixCfg，确定同步吗？";
        if (!EditorUtility.DisplayDialog("二次确认", message, "确定", "取消")) {
            return;
        }
        SpriteModel baseModel = modelList[0];
        for (int index = 1; index < modelList.Count; index++) {
            SpriteModel animModel = modelList[index];
            if (animModel == baseModel) continue;
            // 拷贝MixCfg
            animModel.motionMixCfgList.Clear();
            for (int i = 0; i < baseModel.motionMixCfgList.Count; i++) {
                AnimationMixCfg copiedMixCfg = new AnimationMixCfg(baseModel.motionMixCfgList[i]);
                animModel.motionMixCfgList.Add(copiedMixCfg);
            }
            EditorUtility.SetDirty(animModel);
        }
    }

    #endregion

    #region draw-model-properties

    private void DrawModelPropertyArea() {
        EditorGUILayout.HelpBox(PooledLabel().WithText("模型编辑区:"));
        EditorGUILayout.BeginVertical();
        if (_dataModel.selectedModel) {
            EditorGUI.BeginChangeCheck();
            DrawModelProperty();
            if (EditorGUI.EndChangeCheck() && _dataModel.selectedModel) { // 可能被关闭
                EditorUtility.SetDirty(_dataModel.selectedModel);
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawModelProperty() {
        SpriteModel model = _dataModel.selectedModel;
        bool closed = false;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.ObjectField(model, typeof(SpriteModel), false);
        if (GUILayout.Button("关闭")) {
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
        model.spriteGroup = EditorGUILayout.ObjectField(PooledLabel().WithText("SpriteGroup", "默认贴图"), model.spriteGroup, typeof(SpriteGroup), false) as SpriteGroup;
        EditorGUILayout.Space(5);

        // MotionList
        EditorGUILayout.LabelField("MotionList:");
        EditorGUILayout.BeginHorizontal();
        _motionListFoldOut = EditorGUILayout.Foldout(_motionListFoldOut, "");
        EditorGUILayout.IntField(model.motionList.Count, _width100);

        _dataModel.tempMotionName = EditorGUILayout.TextField(_dataModel.tempMotionName);
        if (GUILayout.Button("创建") && Event.current.button == 0) model.motionList.Add(default);
        if (GUILayout.Button("排序 ↑") && Event.current.button == 0) SortMotion(1);
        if (GUILayout.Button("排序 ↓") && Event.current.button == 0) SortMotion(-1);
        EditorGUILayout.EndHorizontal();
        DrawSeparator();
        EditorGUILayout.Space(5);
        //
        if (_motionListFoldOut) {
            _motionListScrollPos = EditorGUILayout.BeginScrollView(_motionListScrollPos, false, false);
            DrawMotionList();
            EditorGUILayout.EndScrollView();

            DrawSeparator();
            EditorGUILayout.Space(5);
        }
        // MixConfigList
        EditorGUILayout.LabelField("MixConfigList:");
        EditorGUILayout.BeginHorizontal();
        _mixerListFoldOut = EditorGUILayout.Foldout(_mixerListFoldOut, "");
        EditorGUILayout.IntField(model.motionMixCfgList.Count, _width100);

        _dataModel.mixMotionA = EditorGUILayout.TextField(_dataModel.mixMotionA);
        _dataModel.mixMotionB = EditorGUILayout.TextField(_dataModel.mixMotionB);
        if (GUILayout.Button("创建") && Event.current.button == 0) CreateMixCfg();
        if (GUILayout.Button("排序 ↑") && Event.current.button == 0) SortMixCfg(1);
        if (GUILayout.Button("排序 ↓") && Event.current.button == 0) SortMixCfg(-1);
        EditorGUILayout.EndHorizontal();
        DrawSeparator();
        EditorGUILayout.Space(5);
        //
        if (_mixerListFoldOut) {
            _mixerListScrollPos = EditorGUILayout.BeginScrollView(_mixerListScrollPos, false, false);
            DrawMixCfgList();
            EditorGUILayout.EndScrollView();

            DrawSeparator();
            EditorGUILayout.Space(5);
        }
    }

    /// <summary>
    /// 模型动作列表
    /// </summary>
    private void DrawMotionList() {
        SpriteModel model = _dataModel.selectedModel;
        int deleteIndex = -1;
        for (int index = 0; index < model.motionList.Count; index++) {
            if (index > 0) {
                DrawSeparator();
            }
            SpriteMotionRedir motionRedir = model.motionList[index];
            if (IsSelectedMotion(motionRedir)) { // 醒目条
                Rect rect = EditorGUILayout.GetControlRect(false, 1);
                EditorGUI.DrawRect(rect, Color.yellow);
            }
            EditorGUILayout.BeginHorizontal();
            motionRedir.name = EditorGUILayout.TextField(motionRedir.name);
            motionRedir.clip = EditorGUILayout.ObjectField(motionRedir.clip, typeof(SpriteAnimationClip), false) as SpriteAnimationClip;
            // 自动使用动画名
            if (motionRedir.clip && string.IsNullOrEmpty(motionRedir.name)) {
                motionRedir.name = motionRedir.clip.name;
            }
            model.motionList[index] = motionRedir;
            if (GUILayout.Button("删除", _width100) && Event.current.button == 0) {
                deleteIndex = index;
            }
            EditorGUILayout.EndHorizontal();
        }
        // 循环外处理删除
        if (deleteIndex >= 0
            && EditorUtility.DisplayDialog("", $"确定删除？", "确定", "取消")) {
            model.motionList.RemoveAt(deleteIndex);
            Repaint();
        }
    }

    private bool IsSelectedMotion(SpriteMotionRedir motionRedir) {
        return !string.IsNullOrEmpty(motionRedir.name)
               && motionRedir.name == _dataModel.tempMotionName;
    }

    private void SortMotion(int sign) {
        SpriteModel model = _dataModel.selectedModel;
        model.motionList.Sort((a, b) => sign * string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 动作融合列表
    /// </summary>
    private void DrawMixCfgList() {
        SpriteModel model = _dataModel.selectedModel;
        int deleteIndex = -1;
        for (int index = 0; index < model.motionMixCfgList.Count; index++) {
            if (index > 0) {
                DrawSeparator();
            }
            AnimationMixCfg mixCfg = model.motionMixCfgList[index];
            if (IsSelectedMixCfg(mixCfg)) {
                Rect rect = EditorGUILayout.GetControlRect(false, 1);
                EditorGUI.DrawRect(rect, Color.yellow); // 醒目条
            }
            EditorGUILayout.BeginHorizontal();
            mixCfg.motionA = EditorGUILayout.TextField("MotionA", mixCfg.motionA);
            mixCfg.weightA = EditorGUILayout.FloatField("Weight", mixCfg.weightA);
            if (GUILayout.Button("选择")) {
                ShowPickMotionMenu(index, 0);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            mixCfg.motionB = EditorGUILayout.TextField("MotionB", mixCfg.motionB);
            mixCfg.weightB = EditorGUILayout.FloatField("Weight", mixCfg.weightB);
            if (GUILayout.Button("选择", _width100)) {
                ShowPickMotionMenu(index, 1);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            mixCfg.fadeTimeA = EditorGUILayout.FloatField("FadeTimeA", mixCfg.fadeTimeA);
            mixCfg.fadeTimeB = EditorGUILayout.FloatField("FadeTimeB", mixCfg.fadeTimeB);
            if (GUILayout.Button("删除", _width100)) {
                deleteIndex = index;
            }
            EditorGUILayout.EndHorizontal();
        }
        // 循环外处理删除
        if (deleteIndex >= 0
            && EditorUtility.DisplayDialog("", "确定删除？", "确定", "取消")) {
            model.motionMixCfgList.RemoveAt(deleteIndex);
            Repaint();
        }
    }

    private bool IsSelectedMixCfg(AnimationMixCfg mixCfg) {
        if (string.IsNullOrEmpty(mixCfg.motionA) || string.IsNullOrEmpty(mixCfg.motionB)) return false;
        return mixCfg.motionA == _dataModel.mixMotionA && mixCfg.motionB == _dataModel.mixMotionB;
    }

    private void CreateMixCfg() {
        SpriteModel model = _dataModel.selectedModel;
        // 无效输入忽略
        if (string.IsNullOrWhiteSpace(_dataModel.mixMotionA)
            || string.IsNullOrWhiteSpace(_dataModel.mixMotionB)) {
            EditorUtility.DisplayDialog("通知", "输入无效", "关闭");
            return;
        }
        // 有输入的情况下进行去重检测
        if (model.motionMixCfgList.Any(IsSelectedMixCfg)) {
            EditorUtility.DisplayDialog("通知", "目标配置已存在", "关闭");
            return;
        }
        model.motionMixCfgList.Add(new AnimationMixCfg()
        {
            motionA = _dataModel.mixMotionA,
            motionB = _dataModel.mixMotionB
        });
    }

    private void SortMixCfg(int sign) {
        SpriteModel model = _dataModel.selectedModel;
        model.motionMixCfgList.Sort((a, b) => {
            int r = string.Compare(a.motionA, b.motionA, StringComparison.OrdinalIgnoreCase);
            if (r != 0) return sign * r;
            return sign * string.Compare(a.motionB, b.motionB, StringComparison.OrdinalIgnoreCase);
        });
    }

    private void ShowPickMotionMenu(int index, int fieldIndex) {
        SpriteModel model = _dataModel.selectedModel;
        AnimationMixCfg mixCfg = model.motionMixCfgList[index];
        // 回调
        GenericMenu.MenuFunction2 callback = obj => {
            string motionName = (string)obj;
            if (fieldIndex == 0) {
                mixCfg.motionA = motionName;
            } else {
                mixCfg.motionB = motionName;
            }
        };
        // Mune不能使用池化的Label，否则会出异常
        GenericMenu menu = new GenericMenu();
        string motionName = fieldIndex == 0 ? mixCfg.motionA : mixCfg.motionB;
        foreach (var motionRedir in model.motionList) {
            menu.AddItem(new GUIContent(motionRedir.name), motionRedir.name == motionName, callback, motionRedir.name);
        }
        menu.ShowAsContext();
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
        /// 创建新模型资产的名字
        /// </summary>
        public string newAssetName;
        /// <summary>
        /// 当前编辑（选中）的模型
        /// </summary>
        public SpriteModel selectedModel;

        public string tempMotionName;
        public string mixMotionA;
        public string mixMotionB;

        /// <summary>
        /// 当前工作目录下的模型
        /// </summary>
        public List<SpriteModel> workModelList = new List<SpriteModel>();
        /// <summary>
        /// 模型同步列表
        /// </summary>
        public List<SpriteModel> syncModelList = new List<SpriteModel>();
    }

    #endregion
}
}