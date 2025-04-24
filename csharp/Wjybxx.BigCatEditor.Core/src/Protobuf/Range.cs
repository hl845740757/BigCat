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

namespace Wjybxx.BigCatEditor.Protobuf
{
/// <summary>
/// 范围
/// (不限制start和end之间的大小关系)
/// </summary>
public readonly struct Range : IEquatable<Range>
{
    public readonly int start;
    public readonly int end;

    public Range(int start, int end) {
        this.start = start;
        this.end = end;
    }

    public bool Equals(Range other) {
        return start == other.start && end == other.end;
    }

    public override bool Equals(object? obj) {
        return obj is Range other && Equals(other);
    }

    public override int GetHashCode() {
        return (start * 397) ^ end;
    }

    public static bool operator ==(Range left, Range right) {
        return left.Equals(right);
    }

    public static bool operator !=(Range left, Range right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        return $"{nameof(start)}: {start}, {nameof(end)}: {end}";
    }
}
}