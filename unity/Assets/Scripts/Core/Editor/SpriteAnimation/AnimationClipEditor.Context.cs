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
using Wjybxx.BigCat.Animator;
using Wjybxx.BigCat.Core;

namespace Wjybxx.BigCat.CoreEditor.SpriteAnimation
{
public partial class AnimationClipEditor
{
    /// <summary>
    /// 尽量通过<see cref="SerializedObject"/>修改对象，以支持Undo操作
    /// </summary>
    private class ClipContext : IDisposable
    {
        public readonly SpriteAnimationClip clip;
        public readonly SerializedObject serializedClip;
        public readonly SerializedProperty serializedFrameArray;

        private int _frameIndex; // 当前编辑/播放帧
        public float playTime; // 播放时间
        public VisualElement clipElement;
        public VisualElement container;
        public VisualElement imageElement;
        public readonly List<VisualElement> damageBoxElements = new List<VisualElement>();
        public readonly List<VisualElement> hurtBoxElements = new List<VisualElement>();

        public ClipContext(SpriteAnimationClip clip,
                           SerializedObject serializedClip,
                           SerializedProperty serializedFrameArray) {
            this.clip = clip;
            this.serializedClip = serializedClip;
            this.serializedFrameArray = serializedFrameArray;
            CachePropertyFields();
        }

        public int frameIndex {
            get => _frameIndex;
            set {
                if (_frameIndex == value) return;
                _frameIndex = value;
                serializedClip.ApplyModifiedProperties(); // 确保数据同步
                CachePropertyFields();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void CachePropertyFields() {
            serializedFrame?.Dispose();
            serializedDamageBoxes?.Dispose();
            serializedHurtBoxes?.Dispose();
            serializedPosition?.Dispose();
            //
            if (CheckFrameIndex()) {
                serializedFrame = serializedFrameArray.GetArrayElementAtIndex(_frameIndex);
                serializedDamageBoxes = serializedFrame.FindPropertyRelative("damageBoxes");
                serializedHurtBoxes = serializedFrame.FindPropertyRelative("hurtBoxes");
                serializedPosition = serializedFrame.FindPropertyRelative("position");
            } else {
                serializedFrame = null;
                serializedDamageBoxes = null;
                serializedHurtBoxes = null;
                serializedPosition = null;
            }
        }

        public SpriteAnimationFrame frame => clip[frameIndex];
        public SerializedProperty serializedFrame { get; private set; }
        public SerializedProperty serializedDamageBoxes { get; private set; }
        public SerializedProperty serializedHurtBoxes { get; private set; }
        public SerializedProperty serializedPosition { get; private set; }

        /// <summary>
        /// 轴心点的UI坐标
        /// </summary>
        public Vector2 pivotPosition => new Vector2(
            clip.frameSize.x * clip.framePivot.x,
            clip.frameSize.x * (1 - clip.framePivot.y));

        /// <summary>
        /// 检查当前索引的有效性
        /// </summary>
        /// <returns></returns>
        public bool CheckFrameIndex() => frameIndex >= 0 && frameIndex < clip.FrameCount;

        public bool CheckFrameIndex(int frameIndex) => frameIndex >= 0 && frameIndex < clip.FrameCount;

        public MinMaxAABB GetBox(int index, bool isDamageBox) {
            return isDamageBox ? frame.damageBoxes[index] : frame.hurtBoxes[index];
        }

        public void DeleteBox(int index, bool isDamageBox) {
            SerializedProperty property = isDamageBox ? serializedDamageBoxes : serializedHurtBoxes;
            property.DeleteArrayElementAtIndex(index);
            serializedClip.ApplyModifiedProperties();
        }

        public void AddBox(MinMaxAABB box, bool isDamageBox) {
            SerializedProperty property = isDamageBox ? serializedDamageBoxes : serializedHurtBoxes;
            int arraySize = property.arraySize;
            property.InsertArrayElementAtIndex(arraySize);
            // TODO 缓存
            using (SerializedProperty elementAtIndex = property.GetArrayElementAtIndex(arraySize)) {
                box.WriteProperty(elementAtIndex);
                serializedClip.ApplyModifiedProperties();
            }
        }

        public void SetBox(int index, MinMaxAABB box, bool isDamageBox) {
            SerializedProperty property = isDamageBox ? serializedDamageBoxes : serializedHurtBoxes;
            // TODO 缓存
            using (SerializedProperty elementAtIndex = property.GetArrayElementAtIndex(index)) {
                box.WriteProperty(elementAtIndex);
                serializedClip.ApplyModifiedProperties();
            }
        }

        public void SetDirty() {
            serializedClip.Update();
            EditorUtility.SetDirty(clip);
        }

        public void ApplyModifiedProperties() {
            serializedClip.ApplyModifiedProperties();
        }

        public void Dispose() {
            serializedClip.Dispose();
            serializedFrameArray.Dispose();
            serializedFrame?.Dispose();
            serializedDamageBoxes?.Dispose();
            serializedHurtBoxes?.Dispose();
            serializedPosition?.Dispose();

            if (container == null) return;
            container.Clear();
            container.RemoveFromHierarchy();
            container = null;
            imageElement = null;
            damageBoxElements.Clear();
            hurtBoxElements.Clear();
        }
    }

    private class BoxContext
    {
        public readonly ClipContext clipContext;
        public readonly bool isDamageBox;
        public readonly int boxIndex;

        public BoxContext(ClipContext clipContext,
                          bool isDamageBox, int boxIndex) {
            this.clipContext = clipContext;
            this.isDamageBox = isDamageBox;
            this.boxIndex = boxIndex;
        }

        public bool isHurtBox => !isDamageBox;

        public MinMaxAABB box {
            get => clipContext.GetBox(boxIndex, isDamageBox);
            set => clipContext.SetBox(boxIndex, value, isDamageBox);
        }
        public VisualElement boxElement => isDamageBox
            ? clipContext.damageBoxElements[boxIndex]
            : clipContext.hurtBoxElements[boxIndex];

        public override bool Equals(object obj) {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((BoxContext)obj);
        }

        public override int GetHashCode() {
            int hashCode = (clipContext != null ? clipContext.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ isDamageBox.GetHashCode();
            hashCode = (hashCode * 397) ^ boxIndex;
            return hashCode;
        }
    }
}
}