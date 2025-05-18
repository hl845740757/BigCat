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
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCat.Tests;

[DsonSerializable]
public class Response : IEquatable<Response>
{
#nullable disable
    private int val;
    private string stringVal;

    public int Val {
        get => val;
        set => val = value;
    }
    public string StringVal {
        get => stringVal;
        set => stringVal = value;
    }

    public override string ToString() {
        return $"{nameof(val)}: {val}, {nameof(stringVal)}: {stringVal}";
    }

    public bool Equals(Response other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return val == other.val && stringVal == other.stringVal;
    }

    public override bool Equals(object obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Response)obj);
    }

    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode() {
        return (val * 397) ^ (stringVal != null ? stringVal.GetHashCode() : 0);
    }
}