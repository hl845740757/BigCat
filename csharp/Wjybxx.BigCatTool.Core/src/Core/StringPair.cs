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
public readonly struct StringPair : IEquatable<StringPair>
{
#nullable disable
    public readonly string key;
    public readonly string value;

    public StringPair(string key, string value) {
        this.key = key;
        this.value = value;
    }

    public bool Equals(StringPair other) {
        return key == other.key && value == other.value;
    }

    public override bool Equals(object obj) {
        return obj is StringPair other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(key, value);
    }

    public static bool operator ==(StringPair left, StringPair right) {
        return left.Equals(right);
    }

    public static bool operator !=(StringPair left, StringPair right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        return $"{nameof(key)}: {key}, {nameof(value)}: {value}";
    }
}
}