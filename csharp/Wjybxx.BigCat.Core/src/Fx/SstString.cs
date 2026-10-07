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

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// 共享字符串表中的字符串
/// (考虑结构化代替int值，int值只适合Excel表字段导出时生成，不适合直接定义引用)
/// </summary>
public readonly struct SstString : IEquatable<SstString>
{
    /// <summary>
    /// 字符串在表格中的坐标Id
    /// </summary>
    public readonly int locationId;

    public SstString(int locationId) {
        this.locationId = locationId;
    }

    /// <summary>
    /// 获取字符串值
    /// </summary>
    public string Value => SstMgr.GetString(locationId);

    public static explicit operator string(SstString sstString) => SstMgr.GetString(sstString.locationId);

    public static implicit operator int(SstString sstString) => sstString.locationId;

    public static explicit operator SstString(int locationId) => new SstString(locationId);

    public bool Equals(SstString other) {
        return locationId == other.locationId;
    }

    public override bool Equals(object? obj) {
        return obj is SstString other && Equals(other);
    }

    public override int GetHashCode() {
        return locationId;
    }

    public override string ToString() {
        return $"{nameof(locationId)}: {locationId}";
    }
}
}