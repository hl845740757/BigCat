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

using System;

namespace Wjybxx.BigCat.Util
{
/// <summary>
/// 全局配置id (global config id)
/// 注：框架层的全局配置ID使用最通用的形式，不做任何假设优化。
/// </summary>
public readonly struct Gcid : IEquatable<Gcid>
{
    /// <summary>
    /// 子表id
    /// </summary>
    public readonly int listId;
    /// <summary>
    /// 子表内id
    /// </summary>
    public readonly int localId;

    public Gcid(int listId, int localId) {
        this.listId = listId;
        this.localId = localId;
    }

    public bool Equals(Gcid other) {
        return listId == other.listId && localId == other.localId;
    }

    public override bool Equals(object obj) {
        return obj is Gcid other && Equals(other);
    }

    public override int GetHashCode() {
        return (listId * 397) ^ localId;
    }

    public static bool operator ==(Gcid left, Gcid right) {
        return left.listId == right.listId && left.localId == right.localId;
    }

    public static bool operator !=(Gcid left, Gcid right) {
        return !(left == right);
    }

    public override string ToString() {
        return $"{nameof(listId)}: {listId}, {nameof(localId)}: {localId}";
    }
}
}