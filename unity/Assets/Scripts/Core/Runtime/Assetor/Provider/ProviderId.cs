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

namespace Wjybxx.BigCat.Assetor
{
public readonly struct ProviderId : IEquatable<ProviderId>
{
    public readonly string assetPath;
    public readonly Type assetType;
    public readonly ELoadMethod loadMethod;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assetPath">资产路径</param>
    /// <param name="assetType">请求的资产类型</param>
    /// <param name="loadMethod">加载方式</param>
    public ProviderId(string assetPath, Type assetType, ELoadMethod loadMethod) {
        this.assetPath = assetPath;
        this.assetType = assetType;
        this.loadMethod = loadMethod;
    }

    public bool Equals(ProviderId other) {
        return loadMethod == other.loadMethod
               && assetType == other.assetType
               && assetPath == other.assetPath;
    }

    public override bool Equals(object obj) {
        return obj is ProviderId other && Equals(other);
    }

    public override int GetHashCode() {
        int hashCode = (assetPath != null ? assetPath.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (assetType != null ? assetType.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (int)loadMethod;
        return hashCode;
    }

    public static bool operator ==(ProviderId left, ProviderId right) {
        return left.Equals(right);
    }

    public static bool operator !=(ProviderId left, ProviderId right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        return $"{nameof(assetPath)}: {assetPath}, {nameof(assetType)}: {assetType}, {nameof(loadMethod)}: {loadMethod}";
    }
}
}