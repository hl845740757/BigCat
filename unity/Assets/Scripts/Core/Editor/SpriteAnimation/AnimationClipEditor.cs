using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Wjybxx.BigCat.Animator;
using Wjybxx.BigCat.Core;
using Wjybxx.BigCat.Editor.UIElements;
using Wjybxx.Commons;
using Object = UnityEngine.Object;

namespace Wjybxx.BigCat.Editor.SpriteAnimation
{
/// <summary>
/// 
/// 注：预览模式下，最大每秒30帧的播放频率。
/// </summary>
public partial class AnimationClipEditor : EditorWindow
{
    private Button _pickClipButton;
    private VisualElement _clipListView;
    private Vector2IntField _frameSizeField;
    private Vector2Field _framePivotField;
    private FloatField _frameScaleField;

    private FloatField _clipDurationField;
    private FloatField _clipPpuField;
    private Toggle _clipShadowToggle;
    private IntegerField _frameCountField;

    private Vector2Field _batchPositionField;
    private Vector2Field _batchScaleField;
    private FloatField _batchRotationField;
    private FloatField _batchDurationField;

    private VisualElement _frameAreaElement;
    private IntegerField _frameIndexField;
    private EnumField _showAttackBoxToggle;
    private EnumField _showHurtBoxToggle;
    private EnumField _imageEditModeToggle;
    private Toggle _imagePickIgnoreToggle;
    private ColorField _bgColorField;
    private FloatField _timeScaleField;
    private Vector2IntField _frameRangeField;
    private Toggle _enableRangeToggle;
    private Toggle _playToggle;
    private Slider _playTimeSlider;
    private FloatField _playTimeField;
    private AABBField _aabbField;

    private VisualElement _framePivotIcon;
    private VisualElement _imageStyleElement;
    private VisualElement _attackBoxStyleElement;
    private VisualElement _hurtBoxStyleElement;

    private VisualElement _frameInfoElement;
    private ListView _hurtBoxListView;
    private ListView _attackBoxListView;

    // private int toolBarIndex;
    private readonly List<ClipContext> _clipContextList = new(4); // 当前编辑/播放的所有Clip
    private VisualElement _selectedElement; // 选中的图片和或攻击盒
    private Vector2 _dragStartMousePosition;
    private Vector3 _dragStartItemPosition;
    private Vector3 _dragStartItemSize;

    private IVisualElementScheduledItem _playTimer;
    private double _lastRefreshTime;

    [MenuItem("Window/BigCat/AnimationClipEditor")]
    private static void OpenWindow() {
        AnimationClipEditor wnd = GetWindow<AnimationClipEditor>();
        wnd.titleContent = new GUIContent("AnimationClipEditor");
    }

    private void OnEnable() {
        Undo.undoRedoPerformed += OnUndoExecuted;
    }

    private void OnDisable() {
        Undo.undoRedoPerformed -= OnUndoExecuted;
        foreach (ClipContext context in _clipContextList) {
            context.Dispose();
        }
        _clipContextList.Clear();
        _playTimer?.Pause();
    }

    private void OnUndoExecuted() {
        // 完全不知道撤销了什么，所以全部重新绑定
        foreach (ClipContext context in _clipContextList) {
            context.frameIndex = ClampFrameIndex(context.frameIndex, context.clip.FrameCount);
            context.clip.RefreshDuration();
            BindBoxElements(context, true);
            BindBoxElements(context, false);
        }
        BindFrameInfoElements();
        this.rootVisualElement.schedule.Execute(_ => RefreshPreviewArea(true));
    }

    private ClipContext GetMasterContext() {
        return _clipContextList.Count > 0 ? _clipContextList[0] : null;
    }

    private ClipContext FindContext(SpriteAnimationClip clip) {
        foreach (ClipContext context in _clipContextList) {
            if (context.clip == clip) return context;
        }
        return null;
    }

    private Vector2 pivotPosition => new Vector2(
        _frameSizeField.value.x * _framePivotField.value.x,
        _frameSizeField.value.y * (1 - _framePivotField.value.y));

    private EditMode editMode => (EditMode)_imageEditModeToggle.value.GetHashCode();

    #region init-GUI

    private void InitGUI(VisualElement root) {
        if (root.childCount == 0) {
            return;
        }
        this.rootVisualElement.focusable = true;
        this.rootVisualElement.RegisterCallback<KeyDownEvent>(OnWindowKeyDownEvent);

        root.Q<ToolbarButton>("toolbar0").RegisterCallback<ClickEvent>(OnClickToolbar0);
        root.Q<ToolbarButton>("toolbar1").RegisterCallback<ClickEvent>(OnClickToolbar1);
        _pickClipButton = root.Q<Button>("pick-clip");
        _clipListView = root.Q("clip-list");
        _pickClipButton.RegisterCallback<ClickEvent>(ShowPickClipWindow);
        _clipListView.RegisterCallback<DragEnterEvent>(OnClipListDragEnter);
        //
        _frameSizeField = root.Q<Vector2IntField>("frame-size");
        _frameSizeField.RegisterValueChangedCallback(OnFrameSizeChanged);
        UnityEditorUtil.SetVectorFieldDelayed(_frameSizeField, true);
        //
        _framePivotField = root.Q<Vector2Field>("frame-pivot");
        _framePivotField.RegisterValueChangedCallback(OnFramePivotChanged);
        UnityEditorUtil.SetVectorFieldDelayed(_framePivotField, true);
        //
        _frameScaleField = root.Q<FloatField>("frame-scale");
        _frameScaleField.isDelayed = true;
        _frameScaleField.RegisterValueChangedCallback(OnFrameScaleChanged);

        InitSyncOperateArea(root);
        InitClipInfoArea(root);
        InitBatchOperateArea(root);
        InitFrameInfoArea(root);
        InitFramePreviewArea(root);

        // 初始化展示
        OnMasterClipChanged();
    }

    private void OnWindowKeyDownEvent(KeyDownEvent evt) {
        if (evt.modifiers != 0) return;
        switch (evt.keyCode) {
            case KeyCode.Q: {
                _imageEditModeToggle.SetValueWithoutNotify(EditMode.View);
                break;
            }
            case KeyCode.W: {
                _imageEditModeToggle.SetValueWithoutNotify(EditMode.Move);
                break;
            }
            case KeyCode.E: {
                _imageEditModeToggle.SetValueWithoutNotify(EditMode.Rotation);
                break;
            }
        }
    }

    private void InitClipInfoArea(VisualElement root) {
        _clipDurationField = root.Q<FloatField>("duration");
        _clipShadowToggle = root.Q<Toggle>("shadow");
        _clipPpuField = root.Q<FloatField>("ppu");
        //
        _frameCountField = root.Q<IntegerField>("frame-count");
        _frameCountField.RegisterCallback<FocusOutEvent>(OnFrameCountFocusOut);
        _frameCountField.RegisterCallback<KeyDownEvent>(OnClickFrameCountApply);
        root.Q<Button>("frame-count-add").RegisterCallback<ClickEvent>(OnClickFrameCountAdd);
        root.Q<Button>("frame-count-dec").RegisterCallback<ClickEvent>(OnClickFrameCountDec);
    }

    private void InitSyncOperateArea(VisualElement root) {
        VisualElement syncDiv = root.Q("clip-sync-div");
        syncDiv.Q<Button>("sync-frame-order").RegisterCallback<ClickEvent>(OnClickSyncFrameOrder);
        syncDiv.Q<Button>("sync-frame-duration").RegisterCallback<ClickEvent>(OnClickSyncFrameDuration);
        syncDiv.Q<Button>("sync-frame-position").RegisterCallback<ClickEvent>(OnClickSyncFramePosition);
        // syncDiv.Q<Button>("sync-frame-rotation").RegisterCallback<ClickEvent>(OnClickSyncFrameRotation);
    }

    private void InitBatchOperateArea(VisualElement root) {
        _batchPositionField = root.Q<Vector2Field>("batch-position");
        _batchScaleField = root.Q<Vector2Field>("batch-scale");
        _batchRotationField = root.Q<FloatField>("batch-rotation");
        _batchDurationField = root.Q<FloatField>("batch-duration");
        //
        root.Q<Button>("batch-position-add").RegisterCallback<ClickEvent>(OnClickPositionAdd);
        root.Q<Button>("batch-position-set").RegisterCallback<ClickEvent>(OnClickPositionSet);
        root.Q<Button>("batch-position-lerp").RegisterCallback<ClickEvent>(OnClickPositionLerp);
        //
        root.Q<Button>("batch-scale-add").RegisterCallback<ClickEvent>(OnClickScaleAdd);
        root.Q<Button>("batch-scale-set").RegisterCallback<ClickEvent>(OnClickScaleSet);
        root.Q<Button>("batch-scale-lerp").RegisterCallback<ClickEvent>(OnClickScaleLerp);
        //
        root.Q<Button>("batch-rotation-add").RegisterCallback<ClickEvent>(OnClickRotationAdd);
        root.Q<Button>("batch-rotation-set").RegisterCallback<ClickEvent>(OnClickRotationSet);
        root.Q<Button>("batch-rotation-lerp").RegisterCallback<ClickEvent>(OnClickRotationLerp);
        //
        root.Q<Button>("batch-duration-add").RegisterCallback<ClickEvent>(OnClickDurationAdd);
        root.Q<Button>("batch-duration-set").RegisterCallback<ClickEvent>(OnClickDurationSet);
        root.Q<Button>("batch-duration-lerp").RegisterCallback<ClickEvent>(OnClickDurationLerp);
    }

    private void InitFrameInfoArea(VisualElement root) {
        _frameInfoElement = root.Q<VisualElement>("frame-info-div");
        _hurtBoxListView = root.Q<ListView>("hurt-boxes");
        _attackBoxListView = root.Q<ListView>("attack-boxes");
        _hurtBoxListView.SetFoldout(false);
        _hurtBoxListView.itemsAdded += _ => RebindHurtBoxElements();
        _hurtBoxListView.itemsRemoved += _ => RebindHurtBoxElements();
        _attackBoxListView.SetFoldout(false);
        _attackBoxListView.itemsAdded += _ => RebindAttackBoxElements();
        _attackBoxListView.itemsRemoved += _ => RebindAttackBoxElements();
        // 帧数据变化刷新UI和图片
        ObjectPathField spritePathField = _frameInfoElement.Q<ObjectPathField>();
        spritePathField.label = "SpritePath";
        spritePathField.RegisterValueChangedCallback(OnFrameSpritePathChanged);
        _frameInfoElement.Q<FloatField>("duration").RegisterValueChangedCallback(OnFrameDurationChanged);
        _frameInfoElement.Q<Vector2Field>("position").RegisterValueChangedCallback(OnImagePropertyChanged);
        _frameInfoElement.Q<Vector2Field>("scale").RegisterValueChangedCallback(OnImagePropertyChanged);
        _frameInfoElement.Q<FloatField>("rotation").RegisterValueChangedCallback(OnImagePropertyChanged);
        _frameInfoElement.Q<ColorField>("tint").RegisterValueChangedCallback(OnImagePropertyChanged);
    }

    private void InitFramePreviewArea(VisualElement root) {
        VisualElement previewArea = root.Q("preview-area");
        _frameAreaElement = previewArea.Q<VisualElement>("frame-area");
        _framePivotIcon = previewArea.Q<VisualElement>("frame-pivot-icon");
        _imageStyleElement = previewArea.Q<VisualElement>("image-style");
        _attackBoxStyleElement = previewArea.Q<VisualElement>("attack-box-style");
        _hurtBoxStyleElement = previewArea.Q<VisualElement>("hurt-box-style");
        _frameAreaElement.RegisterCallback<MouseDownEvent>(OnFrameAreaMouseDown);
        _frameAreaElement.RegisterCallback<MouseMoveEvent>(OnFrameAreaMouseMove);
        _frameAreaElement.RegisterCallback<MouseUpEvent>(ReleaseMouse);
        _frameAreaElement.RegisterCallback<WheelEvent>(OnFrameAreaWheelEvent);
        //
        _frameIndexField = previewArea.Q<IntegerField>("frame-index");
        _frameIndexField.SetValueWithoutNotify(0);
        _frameIndexField.RegisterValueChangedCallback(OnFrameIndexChanged);
        previewArea.Q<Button>("frame-index-prev").RegisterCallback<ClickEvent>(OnClickFrameIndexPrev);
        previewArea.Q<Button>("frame-index-next").RegisterCallback<ClickEvent>(OnClickFrameIndexNext);
        // 
        _showAttackBoxToggle = previewArea.Q<EnumField>("show-attack-box");
        _showHurtBoxToggle = previewArea.Q<EnumField>("show-hurt-box");
        _imageEditModeToggle = previewArea.Q<EnumField>("image-edit-mode");
        _imagePickIgnoreToggle = previewArea.Q<Toggle>("image-pick-ignore");
        _bgColorField = previewArea.Q<ColorField>("bg-color");
        _timeScaleField = previewArea.Q<FloatField>("time-scale");
        _frameRangeField = previewArea.Q<Vector2IntField>("frame-range");
        _enableRangeToggle = previewArea.Q<Toggle>("enable-range");
        _playToggle = previewArea.Q<Toggle>("play-toggle");
        _playTimeSlider = previewArea.Q<Slider>("play-time-slider");
        _playTimeField = previewArea.Q<FloatField>("play-time-field");
        _aabbField = previewArea.Q<AABBField>();
        _imageEditModeToggle.Init(EditMode.Move);
        _showAttackBoxToggle.Init(RangeMode.All);
        _showHurtBoxToggle.Init(RangeMode.All);
        _showAttackBoxToggle.RegisterValueChangedCallback(OnShowBoxToggleChanged);
        _showHurtBoxToggle.RegisterValueChangedCallback(OnShowBoxToggleChanged);
        _bgColorField.RegisterValueChangedCallback(OnBackgroundColorChanged);
        _imagePickIgnoreToggle.RegisterValueChangedCallback(OnPickIgnoreChanged);
        _enableRangeToggle.RegisterValueChangedCallback(_ => RefreshPlayTimeSliderRange());
        //
        _playToggle.SetValueWithoutNotify(false);
        _playToggle.RegisterValueChangedCallback(OnPlayToggleChanged);
        _playTimeSlider.SetValueWithoutNotify(0);
        _playTimer = rootVisualElement.schedule.Execute(OnPlayTimerCallback).StartingIn(33).Every(33);
        _playTimer.Pause(); // 初始为暂停状态
        //
        _aabbField.SetEnabled(false);
        _aabbField.RegisterValueChangedCallback(OnGlobalAABBFieldChanged);
    }

    #endregion

    #region clip-list

    private static float GetClipListViewHeight(int childCount) {
        return childCount == 0 ? 40 : (childCount * 20 + 20);
    }

    private void OnClickToolbar0(ClickEvent evt) {
        evt.StopPropagation();
        // toolBarIndex = 0;
        rootVisualElement.Q("clip-info-div").parent.SetDisplay(true);
        rootVisualElement.Q("clip-sync-div").SetDisplay(false);
        RefreshPreviewArea();
    }

    private void OnClickToolbar1(ClickEvent evt) {
        evt.StopPropagation();
        // toolBarIndex = 1;
        rootVisualElement.Q("clip-info-div").parent.SetDisplay(false);
        rootVisualElement.Q("clip-sync-div").SetDisplay(true);
    }

    private void OnFrameScaleChanged(ChangeEvent<float> evt) {
        evt.StopPropagation();
        float value = Mathf.Clamp(evt.newValue, 0.5f, 5f);
        _frameScaleField.SetValueWithoutNotify(value);
        _frameAreaElement.transform.scale = new Vector3(value, value);
    }

    private void OnFrameSizeChanged(ChangeEvent<Vector2Int> evt) {
        evt.StopPropagation();
        RefreshPreviewArea();
    }

    private void OnFramePivotChanged(ChangeEvent<Vector2> evt) {
        evt.StopPropagation();
        RefreshPreviewArea();
    }

    private void ShowPickClipWindow(ClickEvent evt) {
        evt.StopPropagation();
        string filePath = UnityEditorUtil.OpenFilePanel("选择动画资产", UnityEditorUtil.lastOpenFolder, "asset");
        if (string.IsNullOrEmpty(filePath)) return;
        string assetPath = UnityEditorUtil.ConvertToAssetPath(filePath);
        UnityEditorUtil.lastOpenFolder = UnityEditorUtil.GetAssetFolderPath(assetPath);

        SpriteAnimationClip clip = AssetDatabase.LoadAssetAtPath<SpriteAnimationClip>(assetPath);
        if (clip) {
            TryAddClip(clip);
        }
    }

    private void OnClipListDragEnter(DragEnterEvent evt) {
        evt.StopPropagation();
        foreach (Object obj in DragAndDrop.objectReferences) {
            if (obj is SpriteAnimationClip clip) {
                TryAddClip(clip);
            }
        }
    }

    /// <summary>
    /// 开放方法以支持外部打开
    /// </summary>
    /// <param name="clip"></param>
    public void TryAddClip(SpriteAnimationClip clip) {
        if (!clip || FindContext(clip) != null) {
            return;
        }
        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty serializedFrameArray = serializedClip.FindProperty("frames");
        ClipContext context = new ClipContext(clip, serializedClip, serializedFrameArray);
        //
        VisualElement clipElement = CreateClipElement();
        ObjectField objectField = clipElement.Q<ObjectField>();
        objectField.objectType = typeof(SpriteAnimationClip);
        objectField.allowSceneObjects = false;
        objectField.SetValueWithoutNotify(clip);
        objectField.RegisterValueChangedCallback(OnClipElementFieldChanged);
        objectField.label = UnityEditorUtil.GetLastDirectoryName(AssetDatabase.GetAssetPath(clip));
        clipElement.Q<Button>("delete").RegisterCallback<ClickEvent>(OnClickClipElementDelete);
        clipElement.Q<Button>("move-top").RegisterCallback<ClickEvent>(OnClickClipElementMoveTop);
        clipElement.userData = context;
        context.clipElement = clipElement;
        //
        VisualElement imageElement = CreateFromStyle(_imageStyleElement);
        imageElement.RegisterCallback<MouseDownEvent>(OnImageElementMouseDown);
        imageElement.RegisterCallback<MouseMoveEvent>(OnImageElementMouseMove);
        imageElement.RegisterCallback<MouseUpEvent>(ReleaseMouse);
        imageElement.RegisterCallback<WheelEvent>(OnImageElementWheelEvent);
        imageElement.RegisterCallback<ContextClickEvent>(ShowImageElementContextMenu);
        imageElement.pickingMode = _imagePickIgnoreToggle.value ? PickingMode.Ignore : PickingMode.Position;
        imageElement.userData = context;
        context.imageElement = imageElement;
        //
        VisualElement container = new VisualElement();
        container.style.position = Position.Absolute;
        container.transform.position = Vector3.zero;
        container.Add(context.imageElement);
        context.container = container;
        //
        _clipContextList.Add(context);
        _frameAreaElement.Add(context.container);
        _clipListView.Add(clipElement);
        _clipListView.style.height = GetClipListViewHeight(_clipListView.childCount);
        //
        context.frameIndex = ClampFrameIndex(_frameIndexField.value, context.clip.FrameCount);
        BindBoxElements(context, true);
        BindBoxElements(context, false);
        //
        if (_clipContextList.Count == 1) {
            OnMasterClipChanged();
        }
        RefreshPlayTimeSliderRange();
        RefreshPreviewArea(true);
    }

    private void OnClickClipElementMoveTop(ClickEvent evt) {
        evt.StopPropagation();
        VisualElement element = (VisualElement)evt.currentTarget;
        ClipContext context = element.FindUserContextInParent<ClipContext>();
        if (context == null || context == GetMasterContext()) return;
        //
        TryMoveClipToTop(context);
    }

    private void TryMoveClipToTop(ClipContext context) {
        if (context == _clipContextList[0]) return;
        _clipContextList.Remove(context);
        _clipContextList.Insert(0, context);
        _clipListView.Remove(context.clipElement);
        _clipListView.Insert(0, context.clipElement);
        //
        OnMasterClipChanged();
        RefreshPreviewArea(true);
    }

    private void OnClickClipElementDelete(ClickEvent evt) {
        evt.StopPropagation();
        VisualElement element = (VisualElement)evt.currentTarget;
        ClipContext context = element.FindUserContextInParent<ClipContext>();
        if (context == null) return;
        bool isMasterContext = context == GetMasterContext();
        //
        _clipContextList.Remove(context);
        _frameAreaElement.Remove(context.container);
        _clipListView.Remove(context.clipElement);
        _clipListView.style.height = GetClipListViewHeight(_clipListView.childCount);
        context.Dispose();
        //
        if (isMasterContext) {
            OnMasterClipChanged();
        }
        RefreshPlayTimeSliderRange();
    }

    // 禁止用户更改引用，由于不支持Readonly，只能出此下策
    private void OnClipElementFieldChanged(ChangeEvent<Object> evt) {
        evt.StopPropagation();
        ObjectField field = (ObjectField)evt.currentTarget;
        field.SetValueWithoutNotify(evt.previousValue);
    }

    private void OnMasterClipChanged() {
        _playToggle.value = false;
        _playTimeSlider.SetValueWithoutNotify(0);
        _playTimeField.SetValueWithoutNotify(0);
        ClipContext context = GetMasterContext();
        if (context == null) {
            BindFrameInfoElements(); // 绑定到空
            return;
        }
        _frameCountField.SetValueWithoutNotify(context.clip.FrameCount);
        _frameIndexField.SetValueWithoutNotify(context.frameIndex);

        SerializedObject serializedClip = context.serializedClip;
        _clipDurationField.BindProperty(serializedClip.FindProperty("duration"));
        _clipShadowToggle.BindProperty(serializedClip.FindProperty("shadow"));
        _clipPpuField.BindProperty(serializedClip.FindProperty("ppu"));
        //
        BindFrameInfoElements();
        RefreshBoxElementVisible(true);
        RefreshBoxElementVisible(false);
    }

    #endregion

    #region frame-area

    private void RefreshPreviewArea(bool forceReload = false) {
        // 切换帧时会触发所有值变化，导致连续刷新多次，通过时间戳限制一下
        if (!forceReload && Time.realtimeSinceStartup - _lastRefreshTime < 0.05) return;
        _lastRefreshTime = Time.realtimeSinceStartup;

        ClipContext masterContext = GetMasterContext();
        if (masterContext == null) return;
        // 设置帧域大小和锚点，y需要用1减
        _frameAreaElement.style.width = _frameSizeField.value.x;
        _frameAreaElement.style.height = _frameSizeField.value.y;
        _frameAreaElement.style.backgroundColor = _bgColorField.value;
        //
        _framePivotIcon.style.position = Position.Absolute;
        _framePivotIcon.transform.position = pivotPosition;
        //
        foreach (ClipContext context in _clipContextList) {
            RefreshImage(context);
        }
        foreach (ClipContext context in _clipContextList) {
            RefreshBoxElements(context);
        }
    }

    /// <summary>
    /// 图片旋转缩放是基于自身Bottom，基于图片自身Bottom并不影响逻辑层，是纯表现层的，用户设置好最终坐标即可
    /// frame.position为相对帧域轴心点坐标，也即相对角色坐标坐标，这里需要转为UI坐标（左上角）
    /// </summary>
    private void RefreshImage(ClipContext context, bool forceReload = false) {
        VisualElement imageElement = context.imageElement;
        SpriteAnimationFrame frame = context.frame;
        if (forceReload || !frame.sprite) {
            frame.sprite = UnityEditorUtil.LoadSprite(frame.spritePath);
        }
        Sprite sprite = frame.sprite;
        if (!sprite) {
            imageElement.visible = false;
            return;
        }
        int rawWidth = sprite.texture.width;
        int rawHeight = sprite.texture.height;

        imageElement.visible = true;
        imageElement.style.backgroundImage = sprite.texture;
        imageElement.style.unityBackgroundImageTintColor = (Color)frame.tint;
        imageElement.style.width = rawWidth;
        imageElement.style.height = rawHeight;
        imageElement.style.position = Position.Absolute;
        // 旋转和缩放不影响图片的UI坐标
        Vector2 imgBottom = pivotPosition + new Vector2(frame.position.x, -frame.position.y);
        imageElement.transform.rotation = Quaternion.Euler(0, 0, frame.rotation);
        imageElement.transform.scale = frame.scale;
        imageElement.transform.position = imgBottom - new Vector2(rawWidth / 2f, rawHeight);
    }

    private void RefreshBoxElements(ClipContext clipContext) {
        foreach (VisualElement boxElement in clipContext.attackBoxElements) {
            if (boxElement.userData is not BoxContext context) break;
            RefreshBoxElement(boxElement, context.box);
        }
        foreach (VisualElement boxElement in clipContext.hurtBoxElements) {
            if (boxElement.userData is not BoxContext context) break;
            RefreshBoxElement(boxElement, context.box);
        }
    }

    private void RefreshBoxElement(VisualElement boxElement, MinMaxAABB aabb) {
        boxElement.style.width = aabb.Width;
        boxElement.style.height = aabb.Height;
        boxElement.transform.position = pivotPosition + new Vector2(aabb.min.x, -aabb.max.y);
    }

    #region frame-menu-area

    private void OnPlayTimerCallback(TimerState state) {
        float globalPlayTime = _playTimeSlider.value + (state.deltaTime / 1000f) * _timeScaleField.value;
        if (globalPlayTime >= _playTimeSlider.highValue) {
            _playTimeSlider.SetValueWithoutNotify(0);
            _playTimeField.SetValueWithoutNotify(0);
        } else {
            _playTimeSlider.SetValueWithoutNotify(globalPlayTime);
            _playTimeField.SetValueWithoutNotify(globalPlayTime);
        }
        bool needRefreshPreviewArea = false;
        for (int clipIndex = 0; clipIndex < _clipContextList.Count; clipIndex++) {
            ClipContext context = _clipContextList[clipIndex];
            if (!context.CheckFrameIndex()) {
                continue;
            }
            float playTime = context.playTime;
            context.playTime = globalPlayTime;
            while (context.playTime >= context.playDuration) {
                context.playTime -= context.playDuration;
            }
            int frameIndex = context.frameIndex;
            if (context.playTime < playTime) { // 回环或调整slider
                context.OnLoopback();
            } else {
                context.frameTime += (context.playTime - playTime);
                if (context.frameTime <= context.frame.duration) {
                    continue;
                }
                context.frameTime -= context.frame.duration;
                context.frameIndex++;
                if (context.frameIndex > context.endFrame) { // 回环
                    context.OnLoopback();
                }
            }
            if (frameIndex == context.frameIndex) {
                continue;
            }
            needRefreshPreviewArea = true;
            BindBoxElements(context, true);
            BindBoxElements(context, false);
            if (clipIndex == 0) {
                _frameIndexField.SetValueWithoutNotify(context.frameIndex);
                BindFrameInfoElements();
            }
        }
        if (needRefreshPreviewArea) {
            RefreshPreviewArea();
        }
    }

    private void OnPlayToggleChanged(ChangeEvent<bool> evt) {
        evt.StopPropagation();
        if (_playTimer == null || _clipContextList.Count == 0) return;
        if (evt.newValue) {
            RefreshPlayTimeSliderRange();
            _playTimer.Resume();
        } else {
            _playTimer.Pause();
        }
    }

    private void RefreshPlayTimeSliderRange() {
        Vector2Int range = _frameRangeField.value;
        ClipContext masterContext = GetMasterContext();
        if (masterContext != null) {
            range.y = Math.Clamp(range.y, 0, masterContext.clip.FrameCount - 1); // end
            range.x = Math.Clamp(range.x, 0, range.y); // start
            _frameRangeField.SetValueWithoutNotify(range);
        }
        // 记录时间最长的动画，确保所有动画可正确循环
        float maxTime = 0;
        if (_enableRangeToggle.value) {
            foreach (ClipContext context in _clipContextList) {
                context.clip.RefreshDuration();
                context.startFrame = range.x;
                context.endFrame = range.y;
                context.playDuration = context.clip.GetDuration(range.x, range.y);
                maxTime = Math.Max(maxTime, context.playDuration);
            }
        } else {
            foreach (ClipContext context in _clipContextList) {
                context.clip.RefreshDuration();
                context.startFrame = 0;
                context.endFrame = context.clip.FrameCount - 1;
                context.playDuration = context.clip.duration;
                maxTime = Math.Max(maxTime, context.playDuration);
            }
        }
        _playTimeSlider.highValue = maxTime;
    }

    private void OnGlobalAABBFieldChanged(ChangeEvent<MinMaxAABB> evt) {
        evt.StopPropagation();
        BoxContext context = _selectedElement.userData as BoxContext;
        if (context == null) return;
        OnBoxItemValueChanged(context, evt.newValue, false);
    }

    private void OnBackgroundColorChanged(ChangeEvent<Color> evt) {
        evt.StopPropagation();
        _frameAreaElement.style.backgroundColor = evt.newValue;
    }

    private void OnPickIgnoreChanged(ChangeEvent<bool> evt) {
        evt.StopPropagation();
        PickingMode pickingMode = _imagePickIgnoreToggle.value ? PickingMode.Ignore : PickingMode.Position;
        foreach (ClipContext context in _clipContextList) {
            context.imageElement.pickingMode = pickingMode;
        }
    }

    private void OnShowBoxToggleChanged(ChangeEvent<Enum> evt) {
        evt.StopPropagation();
        if (evt.currentTarget == _showAttackBoxToggle) {
            RebindAttackBoxElements();
        } else {
            RebindHurtBoxElements();
        }
    }

    private void RefreshBoxElementVisible(bool isAttackBox) {
        for (int clipIndex = 0; clipIndex < _clipContextList.Count; clipIndex++) {
            ClipContext context = _clipContextList[clipIndex];
            List<VisualElement> boxElements;
            MinMaxAABB[] boxArray;
            bool visible;
            if (isAttackBox) {
                boxElements = context.attackBoxElements;
                boxArray = context.frame.attackBoxes;
                visible = IsBoxElementVisible(context, true);
            } else {
                boxElements = context.hurtBoxElements;
                boxArray = context.frame.hurtBoxes;
                visible = IsBoxElementVisible(context, false);
            }
            // 只修改使用中的Element的可见性
            for (int index = 0; index < boxArray.Length; index++) {
                boxElements[index].visible = visible;
            }
        }
    }

    private bool IsBoxElementVisible(ClipContext clipContext, bool isAttackBox) {
        RangeMode mode = isAttackBox
            ? (RangeMode)_showAttackBoxToggle.value.GetHashCode()
            : (RangeMode)_showHurtBoxToggle.value.GetHashCode();
        return mode switch
        {
            RangeMode.All => true,
            RangeMode.Master => clipContext == GetMasterContext(),
            _ => false,
        };
    }

    private void OnFrameIndexChanged(ChangeEvent<int> evt) {
        evt.StopPropagation();
        ClipContext masterContext = GetMasterContext();
        if (masterContext == null) return;
        //
        int frameIndex = ClampFrameIndex(_frameIndexField.value, masterContext.clip.FrameCount);
        _frameIndexField.SetValueWithoutNotify(frameIndex);
        if (masterContext.frameIndex == frameIndex) {
            return;
        }
        // 所有Clip同步切换的体验更好些
        foreach (ClipContext context in _clipContextList) {
            int prevIndex = context.frameIndex;
            context.frameIndex = ClampFrameIndex(frameIndex, context.clip.FrameCount);
            // 启用帧区间的话，修正播放时间
            if (_enableRangeToggle.value) {
                context.playTime = context.clip.GetDuration(context.startFrame, context.frameIndex - 1);
                context.playTime = Math.Min(context.playTime, context.playDuration);
            } else {
                context.playTime = context.clip.GetDuration(0, context.frameIndex - 1);
            }
            context.frameTime = 0;
            if (context.frameIndex != prevIndex) {
                BindBoxElements(context, true);
                BindBoxElements(context, false);
            }
        }
        _playTimeSlider.SetValueWithoutNotify(masterContext.playTime);
        _playTimeField.SetValueWithoutNotify(masterContext.playTime);
        BindFrameInfoElements();
        RefreshPreviewArea();
    }

    private void OnClickFrameIndexPrev(ClickEvent evt) {
        evt.StopPropagation();
        _frameIndexField.value--;
    }

    private void OnClickFrameIndexNext(ClickEvent evt) {
        evt.StopPropagation();
        _frameIndexField.value++;
    }

    private void RepairFrameIndex() {
        ClipContext context = GetMasterContext();
        if (context == null) {
            _frameIndexField.SetValueWithoutNotify(0);
            return;
        }
        int frameIndex = ClampFrameIndex(_frameIndexField.value, context.clip.FrameCount);
        _frameIndexField.value = frameIndex;
    }

    private static int ClampFrameIndex(int frameIndex, int frameCount) {
        return frameCount == 0 ? 0 : Math.Clamp(frameIndex, 0, frameCount - 1);
    }

    #endregion

    #region box-elments

    private void BindBoxElements(ClipContext clipContext, bool isAttackBox) {
        clipContext.ApplyModifiedProperties();
        MinMaxAABB[] boxArray;
        List<VisualElement> boxElementList;
        VisualElement styleElement;
        if (isAttackBox) {
            boxArray = clipContext.frame.attackBoxes;
            boxElementList = clipContext.attackBoxElements;
            styleElement = _attackBoxStyleElement;
        } else {
            boxArray = clipContext.frame.hurtBoxes;
            boxElementList = clipContext.hurtBoxElements;
            styleElement = _hurtBoxStyleElement;
        }
        //
        while (boxElementList.Count < boxArray.Length) {
            VisualElement element = CreateFromStyle(styleElement);
            element.RegisterCallback<MouseDownEvent>(OnBoxElementMouseDown);
            element.RegisterCallback<MouseMoveEvent>(OnBoxElementMouseMove);
            element.RegisterCallback<MouseUpEvent>(ReleaseMouse);
            element.RegisterCallback<WheelEvent>(OnBoxElementWheelEvent);
            element.RegisterCallback<ContextClickEvent>(ShowBoxElementContextMenu);
            boxElementList.Add(element);
            clipContext.container.Add(element);
        }
        // 绑定有效区间
        bool visible = IsBoxElementVisible(clipContext, isAttackBox);
        for (int idx = 0; idx < boxArray.Length; idx++) {
            boxElementList[idx].visible = visible;
            boxElementList[idx].userData = new BoxContext(clipContext, isAttackBox, idx);
        }
        // 解绑多余部分
        for (int idx = boxArray.Length; idx < boxElementList.Count; idx++) {
            boxElementList[idx].visible = false;
            boxElementList[idx].userData = null;
        }
    }

    private void OnBoxElementWheelEvent(WheelEvent evt) {
        if (_selectedElement != evt.currentTarget) {
            return;
        }
        evt.StopPropagation();

        BoxContext context = _selectedElement.userData as BoxContext;
        if (context == null) return;
        MinMaxAABB aabb = context.box;
        // 宽高缩放10%，保持center不变
        float scale = evt.delta.y > 0 ? 0.9f : 1.1f;
        Vector3 center = aabb.Center;
        aabb.Width *= scale;
        aabb.Height *= scale;
        aabb.Center = center;
        aabb.Truncate();
        OnBoxItemValueChanged(context, aabb);
        // 修正拖拽数据
        _dragStartMousePosition = evt.mousePosition;
        _dragStartItemPosition = aabb.min;
        _dragStartItemSize = aabb.Size;
    }

    private void OnBoxElementMouseMove(MouseMoveEvent evt) {
        if (_selectedElement != evt.currentTarget
            || !_selectedElement.HasMouseCapture()) {
            return;
        }
        BoxContext context = _selectedElement.userData as BoxContext;
        if (context == null) return;
        Vector2 offset = (evt.mousePosition - _dragStartMousePosition) / _frameAreaElement.transform.scale;
        Vector3 minPosition = _dragStartItemPosition + new Vector3(offset.x, -1 * offset.y);
        UnityEditorUtil.Truncate(ref minPosition);
        MinMaxAABB aabb = context.box;
        aabb.min = minPosition;
        aabb.Size = _dragStartItemSize;
        OnBoxItemValueChanged(context, aabb);
    }

    private void OnBoxElementMouseDown(MouseDownEvent evt) {
        if (evt.button != 0) return;
        evt.StopPropagation();
        CancelImageSelectEffect();
        _selectedElement = (VisualElement)evt.currentTarget;
        _dragStartMousePosition = evt.mousePosition;
        _selectedElement.CaptureMouse(); // 拖拽期间不丢失鼠标事件

        BoxContext context = (BoxContext)_selectedElement.userData;
        MinMaxAABB box = context.box;
        _dragStartItemPosition = box.min;
        _dragStartItemSize = box.Size;
        _aabbField.SetEnabled(true);
        _aabbField.SetValueWithoutNotify(box);
    }

    private void ShowBoxElementContextMenu(ContextClickEvent evt) {
        evt.StopPropagation();
        VisualElement element = (VisualElement)evt.currentTarget;
        if (element.userData is not BoxContext context) return;
        //
        GenericMenu menu = new GenericMenu();
        menu.AddDisabledItem(new GUIContent($"Box({context.clipContext.clip.name}), Index: {context.boxIndex}"));
        menu.AddItem(new GUIContent("删除"), false, OnClickBoxItemDelect, context);
        menu.ShowAsContext();
    }

    #endregion

    #region image-element

    private void OnImageElementWheelEvent(WheelEvent evt) {
        if (_selectedElement != evt.currentTarget) {
            return;
        }
        evt.StopPropagation();

        ClipContext context = _selectedElement.userData as ClipContext;
        if (context == null) return;
        float scale = evt.delta.y > 0 ? 0.9f : 1.1f;
        context.serializedScale.vector2Value *= scale;
        context.ApplyModifiedProperties();
        context.imageElement.transform.scale = context.serializedScale.vector2Value;
        // 修正拖拽数据
        _dragStartMousePosition = evt.mousePosition;
        _dragStartItemPosition = context.frame.position;
        _dragStartItemSize = context.imageElement.transform.position;
    }

    private void OnImageElementMouseMove(MouseMoveEvent evt) {
        if (_selectedElement != evt.currentTarget
            || !_selectedElement.HasMouseCapture()) {
            return;
        }
        ClipContext context = _selectedElement.userData as ClipContext;
        if (context == null) return;
        if (editMode == EditMode.Move) {
            Vector2 offset = (evt.mousePosition - _dragStartMousePosition) / _frameAreaElement.transform.scale;
            context.imageElement.transform.position = _dragStartItemSize + (Vector3)offset;
            // TODO 需要支持0.5像素
            Vector2 framePosition = (Vector2)_dragStartItemPosition + new Vector2(offset.x, -offset.y);
            UnityEditorUtil.Truncate(ref framePosition);
            context.serializedPosition.vector2Value = framePosition;
            context.ApplyModifiedProperties();
        } else if (editMode == EditMode.Rotation) {
            Vector2 offset = (evt.mousePosition - _dragStartMousePosition);
            _dragStartMousePosition = evt.mousePosition;
            float deg = offset.x > 0 ? 1 : -1;
            deg += context.serializedRotation.floatValue;
            if (deg > 180) deg = -180;
            if (deg < -180) deg = 180;
            context.serializedRotation.floatValue = deg;
            context.ApplyModifiedProperties();
        }
    }

    private void OnImageElementMouseDown(MouseDownEvent evt) {
        if (evt.button != 0) return;
        evt.StopPropagation();
        if (evt.currentTarget != _selectedElement) {
            CancelImageSelectEffect();
        }
        _selectedElement = (VisualElement)evt.currentTarget;
        _dragStartMousePosition = evt.mousePosition;
        _selectedElement.CaptureMouse(); // 拖拽期间不丢失鼠标事件
        _selectedElement.SetBorderWidth(1);
        _aabbField.SetEnabled(false);

        ClipContext context = (ClipContext)_selectedElement.userData;
        _dragStartItemPosition = context.frame.position;
        _dragStartItemSize = context.imageElement.transform.position;
        TryMoveClipToTop(context);
    }

    /** 取消图片选中效果 */
    private void CancelImageSelectEffect() {
        if (_selectedElement == null) return;
        if (_selectedElement.userData is ClipContext context) {
            context.imageElement.SetBorderWidth(0);
        }
    }

    private void ShowImageElementContextMenu(ContextClickEvent evt) {
        evt.StopPropagation();
        VisualElement element = (VisualElement)evt.currentTarget;
        ClipContext context = element.userData as ClipContext;
        if (context == null) return;

        GenericMenu menu = new GenericMenu();
        menu.AddDisabledItem(new GUIContent("Image(" + context.clip.name + ")"));
        int frameIndex = _frameIndexField.value;
        if (frameIndex == 0) {
            menu.AddDisabledItem(new GUIContent("左移1帧"));
        } else {
            menu.AddItem(new GUIContent("左移1帧"), false, OnClickFrameMoveLeft, null);
        }
        if (frameIndex + 1 >= context.clip.FrameCount) {
            menu.AddDisabledItem(new GUIContent("右移1帧"));
        } else {
            menu.AddItem(new GUIContent("右移1帧"), false, OnClickFrameMoveRight, null);
        }
        menu.AddItem(new GUIContent("删除"), false, OnClickFrameDelete, null);
        menu.AddItem(new GUIContent("插入"), false, OnClickFrameInsert, null);
        // 在多动画编辑模式下，如果通过点击空白添加攻击盒，容易产生混乱，不知道添加到哪个动画上了
        Vector2 localMousePosition = (evt.mousePosition - _frameAreaElement.worldBound.position) / _frameAreaElement.transform.scale;
        FrameMenuContext menuContext = new FrameMenuContext(localMousePosition);
        menu.AddItem(new GUIContent("添加攻击盒"), false, OnClickFrameAddAttackBox, menuContext);
        menu.AddItem(new GUIContent("添加受击盒"), false, OnClickFrameAddHurtBox, menuContext);
        menu.ShowAsContext();
    }

    private void OnClickFrameMoveRight(object _) {
        int frameIndex = _frameIndexField.value;
        SwapFrame(frameIndex + 1);
    }

    private void OnClickFrameMoveLeft(object _) {
        int frameIndex = _frameIndexField.value;
        SwapFrame(frameIndex - 1);
    }

    private void SwapFrame(int targetIndex) {
        ClipContext context = GetMasterContext();
        if (context == null || !context.CheckFrameIndex(targetIndex)) return;
        int frameIndex = _frameIndexField.value;
        context.serializedFrameArray.MoveArrayElement(frameIndex, targetIndex);
        context.ApplyModifiedProperties();
        context.clip.RefreshDuration();
        //
        _frameIndexField.value = targetIndex;
    }

    private void OnClickFrameAddHurtBox(object obj) {
        FrameMenuContext context = (FrameMenuContext)obj;
        OnClickFrameAddBox(context, false);
    }

    private void OnClickFrameAddAttackBox(object obj) {
        FrameMenuContext context = (FrameMenuContext)obj;
        OnClickFrameAddBox(context, true);
    }

    private void OnClickFrameAddBox(FrameMenuContext context, bool isAttackBox) {
        ClipContext clipContext = GetMasterContext();
        if (clipContext == null || !clipContext.CheckFrameIndex()) return;
        //
        Vector2 offset = context.localMousePosition - pivotPosition;
        offset.y = -1 * offset.y;
        MinMaxAABB aabb = MinMaxAABB.OfCenter(offset, new Vector3(50, 50, 20));
        clipContext.AddBox(aabb, isAttackBox);
        //
        BindBoxElements(clipContext, isAttackBox);
        RefreshBoxElements(clipContext);
    }

    private void OnClickFrameInsert(object _) {
        int frameIndex = _frameIndexField.value;
        ClipContext context = GetMasterContext();
        if (context == null || !context.CheckFrameIndex(frameIndex)) return;

        context.serializedFrameArray.InsertArrayElementAtIndex(frameIndex + 1);
        context.ApplyModifiedProperties();
        //
        _frameIndexField.value = frameIndex + 1;
    }

    private void OnClickFrameDelete(object _) {
        int frameIndex = _frameIndexField.value;
        ClipContext context = GetMasterContext();
        if (context == null || !context.CheckFrameIndex(frameIndex)) return;
        context.serializedFrameArray.DeleteArrayElementAtIndex(frameIndex);
        context.ApplyModifiedProperties();
        //
        _frameIndexField.value = ClampFrameIndex(frameIndex, context.clip.FrameCount);
    }

    private class FrameMenuContext
    {
        public readonly Vector2 localMousePosition; // image-area下的坐标

        public FrameMenuContext(Vector2 localMousePosition) {
            this.localMousePosition = localMousePosition;
        }
    }

    #endregion

    #region frame-context-menu

    private void ReleaseMouse(MouseUpEvent evt) {
        if (evt.button == (int)MouseButton.RightMouse) return;
        _selectedElement?.ReleaseMouse();
    }

    private void OnFrameAreaWheelEvent(WheelEvent evt) {
        if (_selectedElement != evt.currentTarget) {
            return;
        }
        evt.StopPropagation();

        float scale = evt.delta.y > 0 ? -0.1f : 0.1f;
        scale += _frameScaleField.value;
        scale = Mathf.Clamp(scale, 0.5f, 5f);
        _frameScaleField.SetValueWithoutNotify(scale);
        _frameAreaElement.transform.scale = new Vector3(scale, scale);

        // 修正拖拽数据
        _dragStartMousePosition = evt.mousePosition;
        _dragStartItemPosition = _frameAreaElement.transform.position;
    }

    private void OnFrameAreaMouseMove(MouseMoveEvent evt) {
        if (_selectedElement != evt.currentTarget
            || !_selectedElement.HasMouseCapture()) {
            return;
        }
        Vector2 offset = evt.mousePosition - _dragStartMousePosition;
        _frameAreaElement.transform.position = _dragStartItemPosition + (Vector3)offset;
    }

    private void OnFrameAreaMouseDown(MouseDownEvent evt) {
        if (evt.button == (int)MouseButton.RightMouse) return;
        evt.StopPropagation();
        CancelImageSelectEffect();
        _selectedElement = _frameAreaElement;
        _aabbField.SetEnabled(false);

        if (evt.button == (int)MouseButton.MiddleMouse) {
            _dragStartMousePosition = evt.mousePosition;
            _dragStartItemPosition = _frameAreaElement.transform.position;
            _selectedElement.CaptureMouse(); // 拖拽期间不丢失鼠标事件
        }
    }

    #endregion

    #endregion

    #region frame-info

    private void BindFrameInfoElements() {
        ClipContext context = GetMasterContext();
        if (context == null || !context.CheckFrameIndex()) {
            _frameInfoElement.SetEnabled(false);
            _hurtBoxListView.SetEnabled(false);
            _attackBoxListView.SetEnabled(false);
            return;
        }
        _frameInfoElement.SetEnabled(true);
        _hurtBoxListView.SetEnabled(true);
        _attackBoxListView.SetEnabled(true);
        _hurtBoxListView.SetFoldout(false);
        _attackBoxListView.SetFoldout(false);

        // BindProperty不支持绑定到自定义字段，手动维护同步
        SpriteAnimationFrame frame = context.frame;
        SerializedProperty serializedFrame = context.serializedFrame;
        _frameInfoElement.Q<ObjectPathField>().value = frame.spritePath;
        _frameInfoElement.Q<Vector2Field>("position").BindProperty(serializedFrame.FindPropertyRelative("position"));
        _frameInfoElement.Q<Vector2Field>("scale").BindProperty(serializedFrame.FindPropertyRelative("scale"));
        _frameInfoElement.Q<FloatField>("rotation").BindProperty(serializedFrame.FindPropertyRelative("rotation"));
        _frameInfoElement.Q<FloatField>("duration").BindProperty(serializedFrame.FindPropertyRelative("duration"));
        _frameInfoElement.Q<IntegerField>("hurt-type").BindProperty(serializedFrame.FindPropertyRelative("hurtType"));

        _frameInfoElement.Q<ColorField>("tint").BindProperty(serializedFrame.FindPropertyRelative("tint"));
        _frameInfoElement.Q<IntegerField>("interp").BindProperty(serializedFrame.FindPropertyRelative("interp"));
        // _frameInfoElement.Q<EnumField>("flip-type").BindProperty(serializedFrame.FindPropertyRelative("flipType"));

        SerializedProperty serializeHurtBoxes = context.serializedHurtBoxes;
        _hurtBoxListView.BindProperty(serializeHurtBoxes);
        _hurtBoxListView.makeItem = MakeBoxItem;
        _hurtBoxListView.bindItem = BindHurtBoxItem;
        _hurtBoxListView.unbindItem = UnbindBoxItem;

        SerializedProperty serializedAttackBoxes = context.serializedAttackBoxes;
        _attackBoxListView.BindProperty(serializedAttackBoxes);
        _attackBoxListView.makeItem = MakeBoxItem;
        _attackBoxListView.bindItem = BindAttackBoxItem;
        _attackBoxListView.unbindItem = UnbindBoxItem;
    }

    private void OnFrameDurationChanged(ChangeEvent<float> evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        context.clip.RefreshDuration();
        RefreshPlayTimeSliderRange();
    }

    private void OnFrameSpritePathChanged(ChangeEvent<ObjectPath> evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        using (SerializedProperty property = context.serializedFrame.FindPropertyRelative("spritePath")) {
            evt.newValue.WriteProperty(property);
            context.ApplyModifiedProperties();
        }
        RefreshImage(context, true);
    }

    private void OnImagePropertyChanged(EventBase _) {
        ClipContext context = GetMasterContext();
        RefreshImage(context);
    }

    private void RebindHurtBoxElements() { // 只有通过List添加或删除的才触发...
        ClipContext context = GetMasterContext();
        BindBoxElements(context, false);
        RefreshBoxElements(context);
    }

    private void RebindAttackBoxElements() {
        ClipContext context = GetMasterContext();
        BindBoxElements(context, true);
        RefreshBoxElements(context);
    }

    #endregion

    #region box-list-item

    private static void UnbindBoxItem(VisualElement element, int index) {
        element.userData = null;
    }

    private void BindHurtBoxItem(VisualElement element, int index) {
        BindBoxItem(element, index, false);
    }

    private void BindAttackBoxItem(VisualElement element, int index) {
        BindBoxItem(element, index, true);
    }

    private void BindBoxItem(VisualElement element, int index, bool isAttackBox) {
        ClipContext context = GetMasterContext();
        if (context == null) return;
        SpriteAnimationFrame frame = context.frame;
        MinMaxAABB[] boxArray = isAttackBox ? frame.attackBoxes : frame.hurtBoxes;
        if (index >= boxArray.Length) { // 在删除元素的时候可能触发
            return;
        }
        AABBField aabb = (AABBField)element;
        aabb.SetValueWithoutNotify(boxArray[index]);
        element.userData = new BoxContext(context, true, index);
    }

    private VisualElement MakeBoxItem() {
        AABBField field = AABBField.Create(true);
        field.SetValueWithoutNotify(new MinMaxAABB(Vector3.zero, new Vector3(50, 50, 20)));
        field.RegisterValueChangedCallback(OnBoxItemValueChanged);
        field.RegisterCallback<ContextClickEvent>(ShowBoxItemContextMenu);
        return field;
    }

    private void OnBoxItemValueChanged(ChangeEvent<MinMaxAABB> evt) {
        evt.StopPropagation();
        AABBField element = (AABBField)evt.currentTarget;
        BoxContext context = element.userData as BoxContext;
        if (context == null) return;
        OnBoxItemValueChanged(context, evt.newValue);
    }

    private void OnBoxItemValueChanged(BoxContext context, MinMaxAABB box, bool refreshGlobalField = true) {
        box.Truncate(); // 修正为整数
        context.box = box;
        context.clipContext.SetDirty();
        //
        ClipContext clipContext = context.clipContext;
        ClipContext masterContext = GetMasterContext();
        if (clipContext == masterContext) {
            if (context.isAttackBox) {
                _attackBoxListView.RefreshItem(context.boxIndex);
            } else {
                _hurtBoxListView.RefreshItem(context.boxIndex);
            }
        }
        RefreshBoxElement(context.boxElement, box);
        if (refreshGlobalField && context.Equals(_selectedElement?.userData)) {
            _aabbField.SetValueWithoutNotify(box);
        }
    }

    private void ShowBoxItemContextMenu(ContextClickEvent evt) {
        evt.StopPropagation();
        AABBField element = (AABBField)evt.currentTarget;
        BoxContext context = element.userData as BoxContext;
        if (context == null) return;
        //
        GenericMenu menu = new GenericMenu();
        menu.AddDisabledItem(new GUIContent("Index: " + context.boxIndex));
        menu.AddItem(new GUIContent("删除"), false, OnClickBoxItemDelect, context);
        menu.ShowAsContext();
    }

    private void OnClickBoxItemDelect(object obj) {
        BoxContext context = (BoxContext)obj;
        context.clipContext.DeleteBox(context.boxIndex, context.isAttackBox);
        //
        BindBoxElements(context.clipContext, context.isAttackBox);
        RefreshBoxElements(context.clipContext);
    }

    #endregion

    #region clip-info

    // 重置value
    private void OnFrameCountFocusOut(FocusOutEvent evt) {
        evt.StopPropagation();
        // 延迟重置，允许时间点击按钮
        this.rootVisualElement.schedule.Execute(() => {
            ClipContext context = GetMasterContext();
            if (context == null) return;
            _frameCountField.SetValueWithoutNotify(context.clip.FrameCount);
        }).StartingIn(1000);
    }

    private void OnClickFrameCountApply(KeyDownEvent evt) {
        if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter) return;
        // evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null) return;
        SpriteAnimationClip clip = context.clip;
        // 限制单次增加数量
        int count = Math.Clamp(_frameCountField.value, 0, clip.FrameCount + 100);
        int delta = count - clip.FrameCount;
        if (delta == 0) {
            return;
        }
        context.serializedFrameArray.arraySize = count;
        context.ApplyModifiedProperties();
        context.clip.RefreshDuration();
        //
        _frameCountField.SetValueWithoutNotify(count);
        _clipDurationField.SetValueWithoutNotify(context.clip.duration);
        RepairFrameIndex();
    }

    private void OnClickFrameCountDec(ClickEvent evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null || context.clip.FrameCount == 0) return;
        context.serializedFrameArray.arraySize--;
        context.ApplyModifiedProperties();
        context.clip.RefreshDuration();
        //
        _frameCountField.SetValueWithoutNotify(context.clip.FrameCount);
        _clipDurationField.SetValueWithoutNotify(context.clip.duration);
        RepairFrameIndex();
    }

    private void OnClickFrameCountAdd(ClickEvent evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null) return;
        context.serializedFrameArray.arraySize++;
        context.ApplyModifiedProperties();
        context.clip.RefreshDuration();
        //
        _frameCountField.SetValueWithoutNotify(context.clip.FrameCount);
        _clipDurationField.SetValueWithoutNotify(context.clip.duration);
    }

    #endregion

    #region batch-operate

    private void OnClickDurationLerp(ClickEvent evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null) return;
        context.clip.LerpFrameInterval(_batchDurationField.value);
        context.SetDirty();
    }

    private void OnClickDurationSet(ClickEvent evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null) return;
        context.clip.SetFrameInterval(_batchDurationField.value);
        context.SetDirty();
    }

    private void OnClickDurationAdd(ClickEvent evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null) return;
        context.clip.AddFrameInterval(_batchDurationField.value);
        context.SetDirty();
    }

    private void OnClickRotationLerp(ClickEvent evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null) return;
        context.clip.LerpFrameRotation(_batchRotationField.value);
        context.SetDirty();
    }

    private void OnClickRotationSet(ClickEvent evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null) return;
        context.clip.SetFrameRotation(_batchRotationField.value);
        context.SetDirty();
    }

    private void OnClickRotationAdd(ClickEvent evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null) return;
        context.clip.AddFrameRotation(_batchRotationField.value);
        context.SetDirty();
    }

    private void OnClickScaleLerp(ClickEvent evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null) return;
        context.clip.LerpFrameScale(_batchScaleField.value);
        context.SetDirty();
    }

    private void OnClickScaleSet(ClickEvent evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null) return;
        context.clip.SetFrameScale(_batchScaleField.value);
        context.SetDirty();
    }

    private void OnClickScaleAdd(ClickEvent evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null) return;
        context.clip.AddFrameScale(_batchScaleField.value);
        context.SetDirty();
    }

    private void OnClickPositionLerp(ClickEvent evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null) return;
        context.clip.LerpFramePosition(_batchPositionField.value);
        context.SetDirty();
    }

    private void OnClickPositionSet(ClickEvent evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null) return;
        context.clip.SetFramePosition(_batchPositionField.value);
        context.SetDirty();
    }

    private void OnClickPositionAdd(ClickEvent evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null) return;
        context.clip.AddFramePosition(_batchPositionField.value);
        context.SetDirty();
    }

    #endregion

    #region sync-operate

    private void OnClickSyncFrameDuration(ClickEvent evt) {
        evt.StopPropagation();
        if (_clipContextList.Count <= 1) return;
        ClipContext masterContext = _clipContextList[0];
        for (int index = 1; index < _clipContextList.Count; index++) {
            ClipContext context = _clipContextList[index];
            SpriteAnimationClip.SyncFrameDuration(masterContext.clip, context.clip);
            context.SetDirty();
        }
        RefreshPreviewArea();
    }

    private void OnClickSyncFramePosition(ClickEvent evt) {
        evt.StopPropagation();
        if (_clipContextList.Count <= 1) return;
        ClipContext masterContext = _clipContextList[0];
        for (int index = 1; index < _clipContextList.Count; index++) {
            ClipContext context = _clipContextList[index];
            SpriteAnimationClip.SyncFramePosition(masterContext.clip, context.clip);
            context.SetDirty();
        }
        RefreshPreviewArea();
    }

    private void OnClickSyncFrameOrder(ClickEvent evt) {
        evt.StopPropagation();
        if (_clipContextList.Count <= 1) return;
        ClipContext masterContext = _clipContextList[0];
        for (int index = 1; index < _clipContextList.Count; index++) {
            ClipContext context = _clipContextList[index];
            SpriteAnimationClip.SyncFrameOrder(masterContext.clip, context.clip);
            context.SetDirty();
        }
        RefreshPreviewArea(true);
    }

    #endregion

    public void CreateGUI() {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Core/Editor/SpriteAnimation/AnimationClipEditor.uxml");
        VisualElement clonedTree = visualTree.CloneTree();
        ScrollView scrollView = new ScrollView();
        scrollView.contentContainer.Add(clonedTree);
        scrollView.contentContainer.style.flexDirection = FlexDirection.Column;
        // scrollView.contentContainer.style.flexWrap = Wrap.Wrap;
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
        scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        root.Add(scrollView);

        InitGUI(clonedTree);
    }

    [OnOpenAsset(1)]
    private static bool OnOpenAsset(int instanceID) {
        SpriteAnimationClip clip = EditorUtility.InstanceIDToObject(instanceID) as SpriteAnimationClip;
        if (!clip) {
            return false;
        }
        OpenWindow();
        AnimationClipEditor window = GetWindow<AnimationClipEditor>();
        window.TryAddClip(clip);
        return true;
    }
    
    private static VisualElement CreateClipElement() {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Core/Editor/SpriteAnimation/ClipListItem.uxml");
        return visualTree.CloneTree()[0];
    }

    // 用于创建图片、攻击盒、受击盒框
    private static VisualElement CreateFromStyle(VisualElement styleHolder) {
        VisualElement element = new VisualElement();
        IResolvedStyle style = styleHolder.resolvedStyle;
        //
        element.style.borderBottomWidth = style.borderBottomWidth;
        element.style.borderTopWidth = style.borderTopWidth;
        element.style.borderLeftWidth = style.borderLeftWidth;
        element.style.borderRightWidth = style.borderRightWidth;
        //
        element.style.borderTopColor = style.borderTopColor;
        element.style.borderBottomColor = style.borderBottomColor;
        element.style.borderLeftColor = style.borderLeftColor;
        element.style.borderRightColor = style.borderRightColor;
        //
        element.style.flexGrow = 0;
        element.style.flexShrink = 0;
        element.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100));
        element.style.position = style.position;
        return element;
    }

    private enum RangeMode
    {
        All,
        Master,
        None
    }

    private enum EditMode
    {
        [InspectorName("查看")]
        View,
        [InspectorName("移动")]
        Move,
        [InspectorName("旋转")]
        Rotation
    }
}
}