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

using UnityEditor;
using Wjybxx.BigCat.Core;

namespace Wjybxx.BigCat.Editor.Util
{
public static class AssetMenus
{
    [MenuItem("Window/BigCat/EditorMenus/RefreshSpriteGroup")]
    public static void RefreshAllSpriteGroups() {
        foreach (string guid in AssetDatabase.FindAssets("t:SpriteGroup", UnityEditorUtil.spriteSearchFolders)) {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            SpriteGroup spriteGroup = AssetDatabase.LoadAssetAtPath<SpriteGroup>(assetPath);
            if (spriteGroup) {
                spriteGroup.Refresh();
            }
        }
    }
    
    [MenuItem("Window/BigCat/EditorMenus/RefreshAudioGroup")]
    public static void RefreshAllAudioGroups() {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioGroup", UnityEditorUtil.audioSearchFolders)) {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            AudioGroup audioGroup = AssetDatabase.LoadAssetAtPath<AudioGroup>(assetPath);
            if (audioGroup) {
                audioGroup.Refresh();
            }
        }
    }
}
}