using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Wjybxx.BigCat.Animator;
using Wjybxx.BigCat.Core;
using Wjybxx.BigCat.CoreEditor.UIElements;
using Wjybxx.Commons;
using Object = UnityEngine.Object;

namespace Wjybxx.BigCat.CoreEditor.SpriteAnimation
{
/// <summary>
/// 
/// 注：预览模式下，最大每秒30帧的播放频率。
/// </summary>
public partial class AnimationClipEditor : EditorWindow
{
    private Button _pickClipButton;
    private VisualElement _clipListView;
    private Toggle _clipLoopToggle;
    private FloatField _clipWeightField;
    private FloatField _clipDurationField;
    private Vector2IntField _frameSizeField;
    private Vector2Field _framePivotField;
    private IntegerField _frameCountField;

    private Vector2Field _batchPositionField;
    private Vector2Field _batchScaleField;
    private FloatField _batchRotationField;
    private FloatField _batchDurationField;

    private VisualElement _frameAreaElement;
    private IntegerField _frameIndexField;
    private EnumField _showDmgBoxToggle;
    private EnumField _showHurtBoxToggle;
    private EnumField _enableImageClickToggle;
    private ColorField _bgColorField;
    private Toggle _playToggle;
    private Slider _playTimeSlider;
    private AABBField _aabbField;

    private VisualElement _framePivotIcon;
    private VisualElement _imageStyleElement;
    private VisualElement _dmgBoxStyleElement;
    private VisualElement _hurtBoxStyleElement;

    private VisualElement _frameInfoElement;
    private ListView _hurtBoxListView;
    private ListView _damageBoxListView;

    private int toolBarIndex;
    private readonly List<ClipContext> _clipContextList = new(4); // 当前编辑/播放的所有Clip
    private VisualElement _selectedElement; // 选中的图片和或攻击盒
    private Vector2 _dragStartMousePosition;
    private Vector3 _dragStartItemPosition;
    private Vector3 _dragStartItemSize;

    private IVisualElementScheduledItem _playTimer;
    private double _lastRefreshTime;

    [MenuItem("Window/BigCat/AnimationClipEditor")]
    public static void ShowExample() {
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
            BindBoxElements(context, true);
            BindBoxElements(context, false);
        }
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

    #region init-GUI

    private void InitGUI(VisualElement root) {
        if (root.childCount == 0) {
            return;
        }
        root.Q<ToolbarButton>("toolbar0").RegisterCallback<ClickEvent>(OnClickToolbar0);
        root.Q<ToolbarButton>("toolbar1").RegisterCallback<ClickEvent>(OnClickToolbar1);
        _pickClipButton = root.Q<Button>("pick-clip");
        _clipListView = root.Q("clip-list");
        _pickClipButton.RegisterCallback<ClickEvent>(ShowPickClipWindow);
        _clipListView.RegisterCallback<DragEnterEvent>(OnClipListDragEnter);
        InitSyncOperateArea(root);
        InitClipInfoArea(root);
        InitBatchOperateArea(root);
        InitFrameInfoArea(root);
        InitFramePreviewArea(root);

        // 初始化展示
        OnMasterClipChanged();
    }

    private void InitClipInfoArea(VisualElement root) {
        _clipLoopToggle = root.Q<Toggle>("loop");
        _clipWeightField = root.Q<FloatField>("weight");
        _clipDurationField = root.Q<FloatField>("duration");
        //
        _frameSizeField = root.Q<Vector2IntField>("frame-size");
        _frameSizeField.RegisterCallback<FocusOutEvent>(OnFrameSizeFocusOut);
        _frameSizeField.RegisterValueChangedCallback(OnFrameSizeChanged);
        //
        _framePivotField = root.Q<Vector2Field>("frame-pivot");
        _framePivotField.RegisterCallback<FocusOutEvent>(OnFramePivotFocusOut);
        _framePivotField.RegisterValueChangedCallback(OnFramePivotChanged);
        //
        _frameCountField = root.Q<IntegerField>("frame-count");
        _frameCountField.RegisterCallback<FocusOutEvent>(OnFrameCountFocusOut);
        root.Q<Button>("frame-count-add").RegisterCallback<ClickEvent>(OnClickFrameCountAdd);
        root.Q<Button>("frame-count-dec").RegisterCallback<ClickEvent>(OnClickFrameCountDec);
        root.Q<Button>("frame-count-apply").RegisterCallback<ClickEvent>(OnClickFrameCountApply);
    }

    private void InitSyncOperateArea(VisualElement root) {
        VisualElement syncDiv = root.Q("clip-sync-div");
        syncDiv.Q<Button>("sync-frame-size").RegisterCallback<ClickEvent>(OnClickSyncFrameSize);
        syncDiv.Q<Button>("sync-frame-order").RegisterCallback<ClickEvent>(OnClickSyncFrameOrder);
        syncDiv.Q<Button>("sync-frame-duration").RegisterCallback<ClickEvent>(OnClickSyncFrameDuration);
        syncDiv.Q<Button>("sync-frame-position").RegisterCallback<ClickEvent>(OnClickSyncFramePosition);
        syncDiv.Q<Button>("sync-frame-rotation").RegisterCallback<ClickEvent>(OnClickSyncFrameRotation);
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
        _damageBoxListView = root.Q<ListView>("damage-boxes");
        _hurtBoxListView.SetFoldout(false);
        _hurtBoxListView.itemsAdded += _ => RebindHurtBoxElements();
        _hurtBoxListView.itemsRemoved += _ => RebindHurtBoxElements();
        _damageBoxListView.SetFoldout(false);
        _damageBoxListView.itemsAdded += _ => RebindDamageBoxElements();
        _damageBoxListView.itemsRemoved += _ => RebindDamageBoxElements();
        // 帧数据变化刷新UI和图片
        _frameInfoElement.Q<FloatField>("duration").RegisterValueChangedCallback(OnFrameDurationChanged);
        _frameInfoElement.QuerySpritePathField("sprite-path").RegisterValueChangedCallback(OnFrameSpritePathChanged);
        _frameInfoElement.Q<Vector2Field>("position").RegisterValueChangedCallback(OnImagePropertyChanged);
        _frameInfoElement.Q<Vector2Field>("scale").RegisterValueChangedCallback(OnImagePropertyChanged);
        _frameInfoElement.Q<FloatField>("rotation").RegisterValueChangedCallback(OnImagePropertyChanged);
        _frameInfoElement.Q<ColorField>("tint").RegisterValueChangedCallback(OnImagePropertyChanged);
    }

    private void InitFramePreviewArea(VisualElement root) {
        _frameAreaElement = root.Q<VisualElement>("frame-area");
        //
        VisualElement frameAreaParent = _frameAreaElement.parent;
        _framePivotIcon = frameAreaParent.Q<VisualElement>("frame-pivot-icon");
        _imageStyleElement = frameAreaParent.Q<VisualElement>("image-style");
        _dmgBoxStyleElement = frameAreaParent.Q<VisualElement>("dmg-box-style");
        _hurtBoxStyleElement = frameAreaParent.Q<VisualElement>("hurt-box-style");
        _frameAreaElement.RegisterCallback<MouseDownEvent>(ReleaseSelectedElement);
        _frameAreaElement.RegisterCallback<ContextClickEvent>(ShowFrameContextMenu);
        //
        _frameIndexField = frameAreaParent.Q<IntegerField>("frame-index");
        _frameIndexField.SetValueWithoutNotify(0);
        _frameIndexField.RegisterValueChangedCallback(OnFrameIndexChanged);
        frameAreaParent.Q<Button>("frame-index-prev").RegisterCallback<ClickEvent>(OnClickFrameIndexPrev);
        frameAreaParent.Q<Button>("frame-index-next").RegisterCallback<ClickEvent>(OnClickFrameIndexNext);
        // 
        _showDmgBoxToggle = frameAreaParent.Q<EnumField>("show-dmg-box");
        _showHurtBoxToggle = frameAreaParent.Q<EnumField>("show-hurt-box");
        _enableImageClickToggle = frameAreaParent.Q<EnumField>("enable-image-click");
        _bgColorField = frameAreaParent.Q<ColorField>("bg-color");
        _playToggle = frameAreaParent.Q<Toggle>("play");
        _playTimeSlider = frameAreaParent.Q<Slider>("play-time");
        _aabbField = frameAreaParent.QueryAABBField("AABBField");
        _showDmgBoxToggle.Init(RangeMode.All);
        _showHurtBoxToggle.Init(RangeMode.All);
        _enableImageClickToggle.Init(RangeMode.All);
        _showDmgBoxToggle.RegisterValueChangedCallback(OnShowBoxToggleChanged);
        _showHurtBoxToggle.RegisterValueChangedCallback(OnShowBoxToggleChanged);
        _enableImageClickToggle.RegisterValueChangedCallback(OnImageToggleChanged);
        _bgColorField.RegisterValueChangedCallback(OnBackgroundColorChanged);
        //
        UnityEditorUtil.SetFieldLabelMargin(_playToggle, -70);
        _playToggle.SetValueWithoutNotify(false);
        _playToggle.RegisterValueChangedCallback(OnPlayToggleChanged);
        _playTimeSlider.SetValueWithoutNotify(0);
        _playTimeSlider.RegisterValueChangedCallback(OnPlayTimeSliderChanged);
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
        toolBarIndex = 0;
        rootVisualElement.Q("clip-info-div").parent.SetDisplay(true);
        rootVisualElement.Q("clip-sync-div").SetDisplay(false);
        RefreshPreviewArea();
    }

    private void OnClickToolbar1(ClickEvent evt) {
        evt.StopPropagation();
        toolBarIndex = 1;
        rootVisualElement.Q("clip-info-div").parent.SetDisplay(false);
        rootVisualElement.Q("clip-sync-div").SetDisplay(true);
    }

    private void ShowPickClipWindow(ClickEvent evt) {
        evt.StopPropagation();
        string filePath = EditorUtility.OpenFilePanel("选择动画资产", UnityEditorUtil.lastOpenFolder, "asset");
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

    private void TryAddClip(SpriteAnimationClip clip) {
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
        BindBoxElements(context, true);
        BindBoxElements(context, false);
        //
        if (_clipContextList.Count == 1) {
            _frameIndexField.SetValueWithoutNotify(0);
            OnMasterClipChanged();
        } else {
            context.frameIndex = ClampFrameIndex(_frameIndexField.value, context.clip.FrameCount);
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
        _clipContextList.Remove(context);
        _clipContextList.Insert(0, context);
        _clipListView.Remove(context.clipElement);
        _clipListView.Insert(0, context.clipElement);
        //
        OnMasterClipChanged();
        RefreshPreviewArea(true);
        // 尽量保留index
        int frameIndex = ClampFrameIndex(_frameIndexField.value, context.clip.FrameCount);
        _frameIndexField.value = frameIndex;
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
        ClipContext context = GetMasterContext();
        if (context == null) {
            BindFrameInfoElements(); // 绑定到空
            return;
        }
        _frameCountField.SetValueWithoutNotify(context.clip.FrameCount);
        context.container.SetDisplay(true);

        SerializedObject serializedClip = context.serializedClip;
        _clipLoopToggle.BindProperty(serializedClip.FindProperty("loop"));
        _clipWeightField.BindProperty(serializedClip.FindProperty("weight"));
        _clipDurationField.BindProperty(serializedClip.FindProperty("duration"));
        _frameSizeField.BindProperty(serializedClip.FindProperty("frameSize"));
        _framePivotField.BindProperty(serializedClip.FindProperty("framePivot"));
        //
        BindFrameInfoElements();
        RefreshImagePickingMode();
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
        _frameAreaElement.style.width = masterContext.clip.frameSize.x;
        _frameAreaElement.style.height = masterContext.clip.frameSize.y;
        _frameAreaElement.style.backgroundColor = _bgColorField.value;
        //
        Vector2 pivotPosition = masterContext.pivotPosition;
        _framePivotIcon.style.position = Position.Absolute;
        _framePivotIcon.transform.position = pivotPosition;
        //
        foreach (ClipContext context in _clipContextList) {
            RefreshImage(context, pivotPosition, forceReload);
        }
        foreach (ClipContext context in _clipContextList) {
            RefreshBoxElements(context, pivotPosition);
        }
    }

    /// <summary>
    /// 图片旋转缩放是基于自身Bottom，基于图片自身Bottom并不影响逻辑层，是纯表现层的，用户设置好最终坐标即可
    /// frame.position为相对帧域轴心点坐标，也即相对角色坐标坐标，这里需要转为UI坐标（左上角）
    /// </summary>
    private void RefreshImage(ClipContext context, Vector2 pivotPosition, bool forceReload = false) {
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

    private void RefreshBoxElements(ClipContext clipContext, Vector2 pivotPosition) {
        foreach (VisualElement boxElement in clipContext.damageBoxElements) {
            if (boxElement.userData is not BoxContext context) break;
            RefreshBoxElement(boxElement, context.box, pivotPosition);
        }
        foreach (VisualElement boxElement in clipContext.hurtBoxElements) {
            if (boxElement.userData is not BoxContext context) break;
            RefreshBoxElement(boxElement, context.box, pivotPosition);
        }
    }

    private void RefreshBoxElement(VisualElement boxElement, MinMaxAABB aabb, Vector2 pivotPosition) {
        boxElement.style.width = aabb.Width;
        boxElement.style.height = aabb.Height;
        boxElement.transform.position = pivotPosition + new Vector2(aabb.min.x, -aabb.max.y);
    }

    #region frame-menu-area

    private void OnPlayTimerCallback(TimerState state) {
        float globalPlayTime = _playTimeSlider.value + state.deltaTime / 1000f;
        if (globalPlayTime >= _playTimeSlider.highValue) {
            _playTimeSlider.SetValueWithoutNotify(0);
        } else {
            _playTimeSlider.SetValueWithoutNotify(globalPlayTime);
        }
        bool needRefreshPreviewArea = false;
        for (int clipIndex = 0; clipIndex < _clipContextList.Count; clipIndex++) {
            ClipContext context = _clipContextList[clipIndex];
            if (!context.CheckFrameIndex()) {
                continue;
            }
            float playTime = context.playTime;
            context.playTime = globalPlayTime;
            while (context.playTime >= context.clip.duration) {
                context.playTime -= context.clip.duration;
            }
            int frameIndex = context.frameIndex;
            if (context.playTime < playTime) { // 回环
                context.frameIndex = 0;
            } else {
                float endTime = context.clip[context.frameIndex].endTime;
                if (context.playTime < endTime) {
                    continue;
                }
                context.frameIndex++;
                if (context.frameIndex >= context.clip.FrameCount) {
                    context.frameIndex = 0;
                    context.playTime -= context.clip.duration;
                }
            }
            if (frameIndex != context.frameIndex) {
                needRefreshPreviewArea = true;
                BindBoxElements(context, true);
                BindBoxElements(context, false);
                if (clipIndex == 0) {
                    _frameIndexField.SetValueWithoutNotify(context.frameIndex);
                    BindFrameInfoElements();
                }
            }
        }
        if (needRefreshPreviewArea) {
            RefreshPreviewArea();
        }
    }

    private void OnPlayTimeSliderChanged(ChangeEvent<float> evt) {
        evt.StopPropagation();
        bool needRefreshPreviewArea = false;
        for (int clipIndex = 0; clipIndex < _clipContextList.Count; clipIndex++) {
            ClipContext context = _clipContextList[clipIndex];
            if (!context.CheckFrameIndex()) {
                continue;
            }
            context.playTime = evt.newValue;
            while (context.playTime >= context.clip.duration) {
                context.playTime -= context.clip.duration;
            }
            int frameIndex = context.clip.SearchFrameByTime(context.playTime);
            if (frameIndex == context.frameIndex) {
                continue;
            }
            context.frameIndex = frameIndex;
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
        float maxTime = 0; // 记录时间最长的动画，确保所有动画可正确循环
        foreach (ClipContext context in _clipContextList) {
            context.clip.RefreshDuration();
            maxTime = Math.Max(maxTime, context.clip.duration);
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

    private void OnImageToggleChanged(ChangeEvent<Enum> evt) {
        evt.StopPropagation();
        RefreshImagePickingMode();
    }

    private void RefreshImagePickingMode() {
        RangeMode mode = (RangeMode)_enableImageClickToggle.value.GetHashCode();
        foreach (ClipContext context in _clipContextList) {
            context.imageElement.pickingMode = mode switch
            {
                RangeMode.All => PickingMode.Position,
                RangeMode.Master => context == GetMasterContext() ? PickingMode.Position : PickingMode.Ignore,
                _ => PickingMode.Ignore
            };
        }
    }

    private void OnShowBoxToggleChanged(ChangeEvent<Enum> evt) {
        evt.StopPropagation();
        bool isDamageBox = evt.currentTarget == _showDmgBoxToggle;
        RefreshBoxElementVisible(isDamageBox);
    }

    private void RefreshBoxElementVisible(bool isDamageBox) {
        for (int clipIndex = 0; clipIndex < _clipContextList.Count; clipIndex++) {
            ClipContext context = _clipContextList[clipIndex];
            List<VisualElement> boxElements;
            MinMaxAABB[] boxArray;
            bool visible;
            if (isDamageBox) {
                boxElements = context.damageBoxElements;
                boxArray = context.frame.damageBoxes;
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

    private bool IsBoxElementVisible(ClipContext clipContext, bool isDamageBox) {
        RangeMode mode = isDamageBox
            ? (RangeMode)_showDmgBoxToggle.value.GetHashCode()
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
            context.playTime = context.frameIndex == 0 ? 0 : context.clip[context.frameIndex - 1].duration;
            if (context.frameIndex != prevIndex) {
                BindBoxElements(context, true);
                BindBoxElements(context, false);
            }
        }
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

    private void BindBoxElements(ClipContext clipContext, bool isDamageBox) {
        clipContext.ApplyModifiedProperties();
        MinMaxAABB[] boxArray;
        List<VisualElement> boxElementList;
        VisualElement styleElement;
        if (isDamageBox) {
            boxArray = clipContext.frame.damageBoxes;
            boxElementList = clipContext.damageBoxElements;
            styleElement = _dmgBoxStyleElement;
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
        bool visible = IsBoxElementVisible(clipContext, isDamageBox);
        for (int idx = 0; idx < boxArray.Length; idx++) {
            boxElementList[idx].visible = visible;
            boxElementList[idx].userData = new BoxContext(clipContext, isDamageBox, idx);
        }
        // 解绑多余部分
        for (int idx = boxArray.Length; idx < boxElementList.Count; idx++) {
            boxElementList[idx].visible = false;
            boxElementList[idx].userData = null;
        }
    }

    private void OnBoxElementWheelEvent(WheelEvent evt) {
        if (_selectedElement != evt.currentTarget
            || !_selectedElement.HasMouseCapture()) {
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
        _dragStartMousePosition = evt.mousePosition - _frameAreaElement.worldBound.position;
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
        // 需要计算在FrameArea下的相对坐标 - 使用Center+Size的方式更新AABB时，可能由于浮点数截断导致AABB变小
        Vector2 localMousePosition = evt.mousePosition - _frameAreaElement.worldBound.position;
        Vector2 offset = localMousePosition - _dragStartMousePosition;
        offset.y *= -1;
        MinMaxAABB aabb = context.box;
        aabb.min = _dragStartItemPosition + (Vector3)offset;
        aabb.Size = _dragStartItemSize;
        OnBoxItemValueChanged(context, aabb);
    }

    private void OnBoxElementMouseDown(MouseDownEvent evt) {
        if (evt.button != 0) return;
        evt.StopPropagation();
        _selectedElement = (VisualElement)evt.currentTarget;
        _dragStartMousePosition = evt.mousePosition - _frameAreaElement.worldBound.position;
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
        if (_selectedElement != evt.currentTarget
            || !_selectedElement.HasMouseCapture()) {
            return;
        }
        evt.StopPropagation();

        ClipContext context = _selectedElement.userData as ClipContext;
        if (context == null) return;
        float scale = evt.delta.y > 0 ? 0.9f : 1.1f;
        context.frame.scale *= scale;
        context.SetDirty();
        // 修正拖拽数据
        _dragStartMousePosition = evt.mousePosition - _frameAreaElement.worldBound.position;
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
        // 需要计算在FrameArea下的相对坐标
        Vector2 localMousePosition = evt.mousePosition - _frameAreaElement.worldBound.position;
        Vector2 offset = localMousePosition - _dragStartMousePosition;
        context.imageElement.transform.position = _dragStartItemSize + (Vector3)offset;
        //
        offset.y *= -1;
        Vector2 framePosition = (Vector2)_dragStartItemPosition + offset;
        context.serializedPosition.vector2Value = framePosition;
        context.ApplyModifiedProperties();
    }

    private void OnImageElementMouseDown(MouseDownEvent evt) {
        if (evt.button != 0) return;
        evt.StopPropagation();
        _selectedElement = (VisualElement)evt.currentTarget;
        _dragStartMousePosition = evt.mousePosition - _frameAreaElement.worldBound.position;
        _selectedElement.CaptureMouse(); // 拖拽期间不丢失鼠标事件

        ClipContext context = (ClipContext)_selectedElement.userData;
        _dragStartItemPosition = context.frame.position;
        _dragStartItemSize = context.imageElement.transform.position;
        TryMoveClipToTop(context);
    }

    private void ShowImageElementContextMenu(ContextClickEvent evt) {
        evt.StopPropagation();
        VisualElement element = (VisualElement)evt.currentTarget;
        ClipContext context = element.userData as ClipContext;
        if (context == null) return;
        // 提示一下归属的动画
        GenericMenu menu = new GenericMenu();
        menu.AddDisabledItem(new GUIContent("Image(" + context.clip.name + ")"));
        menu.ShowAsContext();
    }

    #endregion

    #region frame-context-menu

    private void ReleaseMouse(MouseUpEvent evt) {
        if (evt.button != 0) return;
        _selectedElement?.ReleaseMouse();
    }

    private void ReleaseSelectedElement(MouseDownEvent evt) {
        evt.StopPropagation();
        _selectedElement?.ReleaseMouse();
        _selectedElement = null;
        _aabbField.SetEnabled(false);
    }

    private void ShowFrameContextMenu(ContextClickEvent evt) {
        if (_playToggle.value) return;
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null || !context.CheckFrameIndex()) return;
        //
        Vector2 localMousePosition = evt.mousePosition - _frameAreaElement.worldBound.position;
        FrameMenuContext menuContext = new FrameMenuContext(localMousePosition);
        GenericMenu menu = new GenericMenu();
        menu.AddDisabledItem(new GUIContent("Frame(" + context.clip.name + ")"));
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
        menu.AddItem(new GUIContent("添加攻击盒"), false, OnClickFrameAddDamageBox, menuContext);
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
        if (context == null) return;
        int frameIndex = _frameIndexField.value;
        context.clip.frames.Swap(frameIndex, targetIndex);
        context.clip.RefreshDuration();
        context.SetDirty();
        //
        _frameIndexField.value = targetIndex;
    }

    private void OnClickFrameAddHurtBox(object obj) {
        FrameMenuContext context = (FrameMenuContext)obj;
        OnClickFrameAddBox(context, false);
    }

    private void OnClickFrameAddDamageBox(object obj) {
        FrameMenuContext context = (FrameMenuContext)obj;
        OnClickFrameAddBox(context, true);
    }

    private void OnClickFrameAddBox(FrameMenuContext context, bool isDamageBox) {
        ClipContext clipContext = GetMasterContext();
        if (clipContext == null || !clipContext.CheckFrameIndex()) return;
        //
        Vector2 offset = context.localMousePosition - clipContext.pivotPosition;
        offset.y = -1 * offset.y;
        MinMaxAABB aabb = MinMaxAABB.OfCenter(offset, new Vector3(50, 50, 20));
        clipContext.AddBox(aabb, isDamageBox);
        //
        BindBoxElements(clipContext, isDamageBox);
        RefreshBoxElements(clipContext, clipContext.pivotPosition);
    }

    private void OnClickFrameInsert(object _) {
        int frameIndex = _frameIndexField.value;
        ClipContext context = GetMasterContext();
        if (context == null || !context.CheckFrameIndex(frameIndex)) return;

        SpriteAnimationFrame frame = context.clip[frameIndex];
        SpriteAnimationFrame newFrame = new SpriteAnimationFrame();
        InheritFrameProps(frame, newFrame);
        context.clip.AddFrame(newFrame, frameIndex + 1);
        context.SetDirty();
        //
        _frameIndexField.value = frameIndex + 1;
    }

    private void OnClickFrameDelete(object _) {
        int frameIndex = _frameIndexField.value;
        ClipContext context = GetMasterContext();
        if (context == null || !context.CheckFrameIndex(frameIndex)) return;
        context.clip.RemoveFrame(frameIndex);
        context.SetDirty();
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

    #region frame-info

    private void BindFrameInfoElements() {
        ClipContext context = GetMasterContext();
        if (context == null || !context.CheckFrameIndex()) {
            _frameInfoElement.SetEnabled(false);
            _hurtBoxListView.SetEnabled(false);
            _damageBoxListView.SetEnabled(false);
            return;
        }
        _frameInfoElement.SetEnabled(true);
        _hurtBoxListView.SetEnabled(true);
        _damageBoxListView.SetEnabled(true);

        // BindProperty不支持绑定到自定义字段，手动维护同步
        SpriteAnimationFrame frame = context.frame;
        SerializedProperty serializedFrame = context.serializedFrame;
        _frameInfoElement.QuerySpritePathField("sprite-path").value = frame.spritePath;
        _frameInfoElement.Q<Vector2Field>("position").BindProperty(serializedFrame.FindPropertyRelative("position"));
        _frameInfoElement.Q<Vector2Field>("scale").BindProperty(serializedFrame.FindPropertyRelative("scale"));
        _frameInfoElement.Q<FloatField>("rotation").BindProperty(serializedFrame.FindPropertyRelative("rotation"));
        _frameInfoElement.Q<FloatField>("duration").BindProperty(serializedFrame.FindPropertyRelative("duration"));
        _frameInfoElement.Q<IntegerField>("interp").BindProperty(serializedFrame.FindPropertyRelative("interp"));
        _frameInfoElement.Q<Toggle>("shadow").BindProperty(serializedFrame.FindPropertyRelative("shadow"));
        _frameInfoElement.Q<ColorField>("tint").BindProperty(serializedFrame.FindPropertyRelative("tint"));

        SerializedProperty serializeHurtBoxes = context.serializedHurtBoxes;
        _hurtBoxListView.BindProperty(serializeHurtBoxes);
        _hurtBoxListView.makeItem = MakeBoxItem;
        _hurtBoxListView.bindItem = BindHurtBoxItem;
        _hurtBoxListView.unbindItem = UnbindBoxItem;

        SerializedProperty serializeDamageBoxes = context.serializedDamageBoxes;
        _damageBoxListView.BindProperty(serializeDamageBoxes);
        _damageBoxListView.makeItem = MakeBoxItem;
        _damageBoxListView.bindItem = BindDamageBoxItem;
        _damageBoxListView.unbindItem = UnbindBoxItem;
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
        if (context.CheckFrameIndex()) {
            using (SerializedProperty property = context.serializedFrame.FindPropertyRelative("spritePath")) {
                evt.newValue.WriteProperty(property);
                context.ApplyModifiedProperties();
            }
        }
        RefreshImage(context, context.pivotPosition, true);
    }

    private void OnImagePropertyChanged(EventBase _) {
        ClipContext context = GetMasterContext();
        RefreshImage(context, context.pivotPosition);
    }

    private void RebindHurtBoxElements() { // 只有通过List添加或删除的才触发...
        ClipContext context = GetMasterContext();
        BindBoxElements(context, false);
        RefreshBoxElements(context, context.pivotPosition);
    }

    private void RebindDamageBoxElements() {
        ClipContext context = GetMasterContext();
        BindBoxElements(context, true);
        RefreshBoxElements(context, context.pivotPosition);
    }

    #endregion

    #region box-list-item

    private static void UnbindBoxItem(VisualElement element, int index) {
        element.userData = null;
    }

    private void BindHurtBoxItem(VisualElement element, int index) {
        BindBoxItem(element, index, false);
    }

    private void BindDamageBoxItem(VisualElement element, int index) {
        BindBoxItem(element, index, true);
    }

    private void BindBoxItem(VisualElement element, int index, bool isDamageBox) {
        ClipContext context = GetMasterContext();
        if (context == null) return;
        SpriteAnimationFrame frame = context.frame;
        MinMaxAABB[] boxArray = isDamageBox ? frame.damageBoxes : frame.hurtBoxes;
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
            if (context.isDamageBox) {
                _damageBoxListView.RefreshItem(context.boxIndex);
            } else {
                _hurtBoxListView.RefreshItem(context.boxIndex);
            }
        }
        RefreshBoxElement(context.boxElement, box, masterContext.pivotPosition);
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
        context.clipContext.DeleteBox(context.boxIndex, context.isDamageBox);
        //
        BindBoxElements(context.clipContext, context.isDamageBox);
        RefreshBoxElements(context.clipContext, GetMasterContext().pivotPosition);
    }

    #endregion

    #region frame-size/pivot/count

    private void OnFrameSizeChanged(ChangeEvent<Vector2Int> evt) {
        evt.StopPropagation();
        RefreshPreviewArea();
    }

    private void OnFramePivotChanged(ChangeEvent<Vector2> evt) {
        evt.StopPropagation();
        RefreshPreviewArea();
    }

    // 重置value
    private void OnFramePivotFocusOut(FocusOutEvent evt) {
        evt.StopPropagation();
        // 延迟重置，允许时间点击按钮
        this.rootVisualElement.schedule.Execute(() => {
            ClipContext context = GetMasterContext();
            if (context == null) return;
            _framePivotField.value = context.clip.framePivot;
        }).StartingIn(1000);
    }

    private void OnFrameSizeFocusOut(FocusOutEvent evt) {
        evt.StopPropagation();
        // 延迟重置，允许时间点击按钮
        this.rootVisualElement.schedule.Execute(() => {
            ClipContext context = GetMasterContext();
            if (context == null) return;
            _frameSizeField.value = context.clip.frameSize;
        }).StartingIn(1000);
    }

    // 重置value
    private void OnFrameCountFocusOut(FocusOutEvent evt) {
        evt.StopPropagation();
        // 延迟重置，允许时间点击按钮
        this.rootVisualElement.schedule.Execute(() => {
            ClipContext context = GetMasterContext();
            if (context == null) return;
            _frameCountField.value = context.clip.FrameCount;
        }).StartingIn(1000);
    }

    private void OnClickFrameCountApply(ClickEvent evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null) return;
        SpriteAnimationClip clip = context.clip;
        // 限制单次增加数量
        int count = Math.Clamp(_frameCountField.value, 0, clip.FrameCount + 100);
        _frameCountField.SetValueWithoutNotify(count);
        int delta = count - clip.FrameCount;
        if (delta == 0) {
            return;
        }
        clip.FrameCount = count;
        for (int index = clip.FrameCount - count; index < clip.FrameCount; index++) {
            if (index == 0) continue;
            InheritFrameProps(clip[index - 1], clip[index]);
        }
        context.SetDirty();
        RepairFrameIndex();
    }

    private void OnClickFrameCountDec(ClickEvent evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null || context.clip.FrameCount == 0) return;
        context.clip.FrameCount--;
        context.SetDirty();
        //
        _frameCountField.SetValueWithoutNotify(context.clip.FrameCount);
        RepairFrameIndex();
    }

    private void OnClickFrameCountAdd(ClickEvent evt) {
        evt.StopPropagation();
        ClipContext context = GetMasterContext();
        if (context == null) return;
        SpriteAnimationClip clip = context.clip;
        SpriteAnimationFrame newFrame = new SpriteAnimationFrame();
        if (clip.FrameCount > 0) {
            InheritFrameProps(clip.LastFrame, newFrame);
        }
        clip.AddFrame(newFrame);
        context.SetDirty();
        //
        _frameCountField.SetValueWithoutNotify(clip.FrameCount);
        // RepairFrameIndex();
    }

    private static void InheritFrameProps(SpriteAnimationFrame srcFrame,
                                          SpriteAnimationFrame targetFrame) {
        targetFrame.spritePath = srcFrame.spritePath;
        targetFrame.spritePath.localId++;
        targetFrame.position = srcFrame.position;
        targetFrame.scale = srcFrame.scale;
        targetFrame.rotation = srcFrame.rotation;
        targetFrame.duration = srcFrame.duration;
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

    private void OnClickSyncFrameSize(ClickEvent evt) {
        evt.StopPropagation();
        if (_clipContextList.Count <= 1) return;
        ClipContext masterContext = _clipContextList[0];
        for (int index = 1; index < _clipContextList.Count; index++) {
            ClipContext context = _clipContextList[index];
            context.clip.frameSize = masterContext.clip.frameSize;
            context.clip.framePivot = masterContext.clip.framePivot;
            context.SetDirty();
        }
        RefreshPreviewArea();
    }

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

    private void OnClickSyncFrameRotation(ClickEvent evt) {
        evt.StopPropagation();
        if (_clipContextList.Count <= 1) return;
        ClipContext masterContext = _clipContextList[0];
        for (int index = 1; index < _clipContextList.Count; index++) {
            ClipContext context = _clipContextList[index];
            SpriteAnimationClip.SyncFrameRotation(masterContext.clip, context.clip);
            context.SetDirty();
        }
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
        ScrollView scrollView = new ScrollView(); // 编辑器里不能直接使用ScrollView...
        scrollView.contentContainer.Add(clonedTree);
        scrollView.contentContainer.style.flexDirection = FlexDirection.Column;
        // scrollView.contentContainer.style.flexWrap = Wrap.Wrap;
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
        scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        root.Add(scrollView);

        InitGUI(clonedTree);
    }

    private static VisualElement CreateClipElement() {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Core/Editor/SpriteAnimation/ClipListItem.uxml");
        return visualTree.CloneTree();
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

    #endregion
}
}