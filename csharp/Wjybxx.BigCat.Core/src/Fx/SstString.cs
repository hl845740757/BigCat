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
/// </summary>
public readonly struct SstString : IEquatable<SstString>
{
    private readonly int id;

    public SstString(int id) {
        this.id = id;
    }

    /// <summary>
    /// 获取字符串值
    /// </summary>
    public string Value => SstMgr.GetString(id);

    /// <summary>
    /// 隐式转换为string
    /// </summary>
    /// <param name="sstString"></param>
    /// <returns></returns>
    public static implicit operator string(SstString sstString) => SstMgr.GetString(sstString.id);

    public bool Equals(SstString other) {
        return id == other.id;
    }

    public override bool Equals(object? obj) {
        return obj is SstString other && Equals(other);
    }

    public override int GetHashCode() {
        return id;
    }

    public override string ToString() {
        return $"{nameof(id)}: {id}";
    }
}
}