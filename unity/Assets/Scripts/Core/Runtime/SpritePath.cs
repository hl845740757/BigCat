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

namespace Wjybxx.BigCat.UnityCore
{
/// <summary>
/// 图片路径
/// </summary>
[Serializable]
public struct SpritePath : IEquatable<SpritePath>
{
    /// <summary>
    /// <see cref="SpriteGroup"/>的路径
    /// </summary>
    public string groupPath;
    /// <summary>
    /// 图片组内的Index
    /// </summary>
    public int index;

    public SpritePath(string groupPath, int index) {
        this.groupPath = groupPath;
        this.index = index;
    }

    public bool IsEmpty => string.IsNullOrWhiteSpace(this.groupPath);

    /// <summary>
    /// 池化<see cref="groupPath"/>字符串
    /// </summary>
    public void Intern() {
        if (!string.IsNullOrWhiteSpace(groupPath)) {
            groupPath = string.Intern(groupPath);
        }
    }

    public bool Equals(SpritePath other) {
        return index == other.index && groupPath == other.groupPath;
    }

    public override bool Equals(object obj) {
        return obj is SpritePath other && Equals(other);
    }

    public override int GetHashCode() {
        return ((groupPath != null ? groupPath.GetHashCode() : 0) * 397) ^ index;
    }

    public override string ToString() {
        return $"{nameof(groupPath)}: {groupPath}, {nameof(index)}: {index}";
    }

    public static bool operator ==(SpritePath left, SpritePath right) {
        return left.Equals(right);
    }

    public static bool operator !=(SpritePath left, SpritePath right) {
        return !left.Equals(right);
    }
}
}