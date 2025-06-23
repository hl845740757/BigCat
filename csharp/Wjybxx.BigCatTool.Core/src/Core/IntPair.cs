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

namespace Wjybxx.BigCatTool.Core
{
public readonly struct IntPair : IEquatable<IntPair>
{
    public readonly int key;
    public readonly int value;

    public IntPair(int key, int value) {
        this.key = key;
        this.value = value;
    }

    public bool Equals(IntPair other) {
        return key == other.key && value == other.value;
    }

    public override bool Equals(object? obj) {
        return obj is IntPair other && Equals(other);
    }

    public override int GetHashCode() {
        return (key * 397) ^ value;
    }

    public static bool operator ==(IntPair left, IntPair right) {
        return left.Equals(right);
    }

    public static bool operator !=(IntPair left, IntPair right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        return $"{nameof(key)}: {key}, {nameof(value)}: {value}";
    }
}
}