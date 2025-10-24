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

namespace Wjybxx.BigCat.Util
{
public readonly struct LongPair : IEquatable<LongPair>
{
    public readonly long key;
    public readonly long value;

    public LongPair(long key, long value) {
        this.key = key;
        this.value = value;
    }

    public bool Equals(LongPair other) {
        return key == other.key && value == other.value;
    }

    public override bool Equals(object? obj) {
        return obj is LongPair other && Equals(other);
    }

    public override int GetHashCode() {
        return (key.GetHashCode() * 397) ^ value.GetHashCode();
    }

    public static bool operator ==(LongPair left, LongPair right) {
        return left.Equals(right);
    }

    public static bool operator !=(LongPair left, LongPair right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        return $"{nameof(key)}: {key}, {nameof(value)}: {value}";
    }
}
}