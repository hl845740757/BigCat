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
using Wjybxx.Dson.Types;

namespace Wjybxx.BigCat.UnityCore
{
/// <summary>
/// 对象路径，用于指向<see cref="ObjectBucket"/>中的对象。
///
/// 注：
/// 1.由于Unity不支持包含readonly属性的结构体，因此不能使用<see cref="ObjectPtr"/>。
/// 2.可通过<see cref="ObjectReferenceAttribute"/>配置是否优先使用name引用。
/// </summary>
[Serializable]
public struct ObjectPath : IEquatable<ObjectPath>
{
    /// <summary>
    /// 对象桶路径
    /// </summary>
    public string bucketPath;
    /// <summary>
    /// 本地路径
    /// (通常为objectId或name)
    /// </summary>
    public string localPath;

    public ObjectPath(string bucketPath, string localPath) {
        this.bucketPath = bucketPath;
        this.localPath = localPath;
    }

    /// <summary>
    /// 路径是否为空
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(this.bucketPath)
                           && string.IsNullOrWhiteSpace(this.localPath);

    /// <summary>
    /// 池化<see cref="bucketPath"/>
    /// </summary>
    public void Intern() {
        if (!string.IsNullOrWhiteSpace(this.bucketPath)) {
            this.bucketPath = string.Intern(bucketPath);
        }
    }

    public bool Equals(ObjectPath other) {
        return bucketPath == other.bucketPath && localPath == other.localPath;
    }

    public override bool Equals(object obj) {
        return obj is ObjectPath other && Equals(other);
    }

    public override int GetHashCode() {
        int hash = bucketPath != null ? bucketPath.GetHashCode() : 0;
        return (hash * 397) ^ (localPath != null ? localPath.GetHashCode() : 0);
    }

    public override string ToString() {
        return $"{nameof(bucketPath)}: {bucketPath}, {nameof(localPath)}: {localPath}";
    }

    public static bool operator ==(ObjectPath left, ObjectPath right) {
        return left.Equals(right);
    }

    public static bool operator !=(ObjectPath left, ObjectPath right) {
        return !left.Equals(right);
    }
}
}