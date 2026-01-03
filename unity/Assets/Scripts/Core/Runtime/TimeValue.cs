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
using System.Globalization;

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// 时间值抽象
/// </summary>
public struct TimeValue : IEquatable<TimeValue>
{
    /// <summary>
    /// 时间值
    /// </summary>
    public float value;
    /// <summary>
    /// 时间单位
    /// </summary>
    public readonly TimeUnit unit;

    public TimeValue(float mValue, TimeUnit mUnit = TimeUnit.Second) {
        this.value = mValue;
        this.unit = mUnit;
    }

    public static implicit operator TimeValue(float value) => new TimeValue(value);

    public static bool operator ==(TimeValue lhs, TimeValue rhs) {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        return (double)lhs.value == rhs.value && lhs.unit == rhs.unit;
    }

    public static bool operator !=(TimeValue lhs, TimeValue rhs) => !(lhs == rhs);

    public bool Equals(TimeValue other) => other == this;

    public override bool Equals(object obj) => obj is TimeValue other && other == this;

    public override int GetHashCode() {
        return (this.value.GetHashCode() * 397) ^ (int)this.unit;
    }

    public override string ToString() {
        string str1 = this.value.ToString(CultureInfo.InvariantCulture.NumberFormat);
        string str2 = this.unit switch
        {
            TimeUnit.Second => "s",
            TimeUnit.Millisecond => "ms",
            _ => string.Empty
        };
        return str1 + str2;
    }
}
}