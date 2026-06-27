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
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// XYZ限制为[0, 360]的整数，可使用一个int值封装。
/// </summary>
[Serializable]
public struct Euler32 : IEquatable<Euler32>
{
    private const int MASK_X = 1023;
    private const int MASK_Y = 1023 << 10;
    private const int MASK_Z = 1023 << 20;
    private const int OFFSET_Y = 10;
    private const int OFFSET_Z = 20;

    [SerializeField]
    private int xyz;

    public Euler32(int x, int y, int z) {
        this.xyz = 0;
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public int x {
        get => xyz & MASK_X;
        set {
            CheckValue(value);
            xyz = (xyz & ~MASK_X) | (value);
        }
    }

    public int y {
        get => (xyz & MASK_Y) >> OFFSET_Y;
        set {
            CheckValue(value);
            xyz = (xyz & ~MASK_Y) | (value << OFFSET_Y);
        }
    }

    public int z {
        get => (xyz & MASK_Z) >> OFFSET_Z;
        set {
            CheckValue(value);
            xyz = (xyz & ~MASK_Z) | (value << OFFSET_Z);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckValue(int value) {
        if (value < 0 || value > 360) {
            throw new ArgumentOutOfRangeException(nameof(value) + ": " + value);
        }
    }

    public static implicit operator int(Euler32 value) {
        return value.xyz;
    }

    public static implicit operator Vector3Int(Euler32 value) {
        return new Vector3Int(value.x, value.y, value.z);
    }

    public static explicit operator Euler32(Vector3Int vector3) {
        return new Euler32(vector3.x, vector3.y, vector3.z);
    }

    public static explicit operator Euler32(int xyz) {
        int x = xyz & MASK_X;
        int y = (xyz & MASK_Y) >> OFFSET_Y;
        int z = (xyz & MASK_Z) >> OFFSET_Z;
        return new Euler32(x, y, z);
    }

    #region equals

    public bool Equals(Euler32 other) {
        return xyz == other.xyz;
    }

    public override bool Equals(object obj) {
        return obj is Euler32 other && Equals(other);
    }

    public override int GetHashCode() {
        return xyz;
    }

    public static bool operator ==(Euler32 left, Euler32 right) {
        return left.xyz == right.xyz;
    }

    public static bool operator !=(Euler32 left, Euler32 right) {
        return left.xyz != right.xyz;
    }

    public override string ToString() {
        return $"{nameof(x)}: {x}, {nameof(y)}: {y}, {nameof(z)}: {z}";
    }

    #endregion
}
}