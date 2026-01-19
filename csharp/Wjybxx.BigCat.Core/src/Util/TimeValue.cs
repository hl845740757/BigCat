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

namespace Wjybxx.BigCat.Util
{
/// <summary>
/// 时间值抽象
/// </summary>
public readonly struct TimeValue : IEquatable<TimeValue>
{
    /// <summary>
    /// 时间值
    /// </summary>
    public readonly float value;
    /// <summary>
    /// 时间单位
    /// </summary>
    public readonly TimeUnit unit;

    public TimeValue(float mValue, TimeUnit mUnit = TimeUnit.Second) {
        this.value = mValue;
        this.unit = mUnit;
    }

    /// <summary>
    /// 转换时间单位
    /// </summary>
    public TimeValue ConvertUnit(TimeUnit targetUnit) {
        if (this.unit == targetUnit) return this;
        return targetUnit == TimeUnit.Second
            ? new TimeValue(this.value / 1000f, targetUnit)
            : new TimeValue(this.value * 1000f, targetUnit);
    }

    public static implicit operator TimeValue(float value) => new TimeValue(value);

    public static TimeValue operator +(TimeValue lhs, float delta) {
        return new TimeValue(lhs.value + delta, lhs.unit);
    }

    public static TimeValue operator -(TimeValue lhs, float delta) {
        return new TimeValue(lhs.value - delta, lhs.unit);
    }

    public static TimeValue operator *(TimeValue lhs, float scale) {
        return new TimeValue(lhs.value * scale, lhs.unit);
    }

    public static TimeValue operator /(TimeValue lhs, float scale) {
        return new TimeValue(lhs.value / scale, lhs.unit);
    }

    // 慎重使用
    public static TimeValue operator +(TimeValue lhs, TimeValue rhs) {
        TimeUnit timeUnit = lhs.unit;
        if (timeUnit == rhs.unit) {
            return new TimeValue(lhs.value + rhs.value, timeUnit);
        }
        return timeUnit == TimeUnit.Second
            ? new TimeValue(lhs.value + rhs.value / 1000f, timeUnit)
            : new TimeValue(lhs.value + rhs.value * 1000f, timeUnit);
    }

    public static TimeValue operator -(TimeValue lhs, TimeValue rhs) {
        TimeUnit timeUnit = lhs.unit;
        if (timeUnit == rhs.unit) {
            return new TimeValue(lhs.value - rhs.value, timeUnit);
        }
        return timeUnit == TimeUnit.Second
            ? new TimeValue(lhs.value - rhs.value / 1000f, timeUnit)
            : new TimeValue(lhs.value - rhs.value * 1000f, timeUnit);
    }

    #region equals

    public bool Equals(TimeValue other) => other == this;

    public override bool Equals(object obj) => obj is TimeValue other && other == this;

    public override int GetHashCode() {
        return (this.value.GetHashCode() * 397) ^ (int)this.unit;
    }

    // ReSharper disable CompareOfFloatsByEqualityOperator
    public static bool operator ==(TimeValue lhs, TimeValue rhs) {
        return (double)lhs.value == rhs.value && lhs.unit == rhs.unit;
    }

    public static bool operator !=(TimeValue lhs, TimeValue rhs) {
        return !(lhs == rhs);
    }

    #endregion

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