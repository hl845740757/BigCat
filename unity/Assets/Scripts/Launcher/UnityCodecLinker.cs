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
using Wjybxx.BTree.Branch;
using Wjybxx.Commons.Attributes;
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCat.Launcher
{
/// <summary>
/// 用于为Unity的常用类型生成DsonCodec
/// </summary>
// [UsedForReflectionBasedGenerator]
[DsonCodecLinkerGroup]
public class UnityCodecLinker
{
    private SimpleParallel<object> parallel;
    private Join<object> join;
    
    private Vector2 _vector2;
    private Vector3 _vector3;
}
}