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

using UnityEngine;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// 用于为Unity的常用类型生成DsonCodec
/// </summary>
// [UsedForReflectionBasedGenerator]
[DsonCodecLinkerGroup]
public class UnityCodecLinker
{
    private Vector2 _vector2;
    private Vector3 _vector3;
    private Vector4 _vector4;
    private Quaternion _quaternion;
    // private Vector2Int _vector2Int; // 命名不规范，需要特殊映射...
    // private Vector3Int _vector3Int;
    private Color _color;

// ReSharper disable All
    [DsonCodecLinkerBean(typeof(Vector2Int))]
    private class Vector2IntLinker
    {
        [DsonIgnore(false)]
        [DsonProperty(Name = "x", Getter = "x", Setter = "x")]
        private int m_X;
        [DsonIgnore(false)]
        [DsonProperty(Name = "y", Getter = "y", Setter = "y")]
        private int m_Y;
    }

    [DsonCodecLinkerBean(typeof(Vector3Int))]
    private class Vector3IntLinker
    {
        [DsonIgnore(false)]
        [DsonProperty(Name = "x", Getter = "x", Setter = "x")]
        private int m_X;
        [DsonIgnore(false)]
        [DsonProperty(Name = "y", Getter = "y", Setter = "y")]
        private int m_Y;
        [DsonIgnore(false)]
        [DsonProperty(Name = "z", Getter = "z", Setter = "z")]
        private int m_Z;
    }
}
}