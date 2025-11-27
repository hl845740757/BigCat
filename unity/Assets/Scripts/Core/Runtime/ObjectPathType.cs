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

using Wjybxx.Commons;

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// 资产对象类型
///
/// 1.定义<see cref="ObjectPath"/>中的type，用于编辑器和运行时。
/// 2.暂只定义需要特殊加载的类型
/// </summary>
public enum ObjectPathType
{
    /// <summary>
    /// 默认，路径为资产全路径
    /// </summary>
    Default = 0,
    /// <summary>
    /// 图组中的图片，GroupPath + index
    /// (其实可以考虑配置为name，运行时再转为index)
    /// </summary>
    SpriteOfGroup = 1,
    /// <summary>
    /// 图集中的图片，AtlasPath + name
    /// </summary>
    SpriteOfAtlas = 2,
    /// <summary>
    /// 音效组中的音效，GroupPath + index
    /// </summary>
    AudioOfGroup = 3,

    /// <summary>
    /// Excel表的行，即配置对象引用（SheetName + id）
    /// </summary>
    ExcelRow = 8,
    /// <summary>
    /// Excel表单元格，通常是I18N字符串坐标
    /// </summary>
    ExcelValue = 9,

    // 自定义资产
    // SpriteGroup = 11,
    // SpriteAnimation,
    // AudioGroup,

    // Unity原生资产
    // AnimationClip = 21,
    // AudioClip,
    // AudioMixer,
    // Font,
    // Material,
    // Mesh,
    // Model,
    // PhysicMaterial,
    // Prefab,
    // Scene,
    // Script,
    // Shader,
    // Sprite,
    // Texture,
    // RenderTexture,
    // VideoClip,
}
}