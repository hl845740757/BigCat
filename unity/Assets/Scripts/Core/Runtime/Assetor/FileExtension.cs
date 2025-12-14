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
using System.Runtime.InteropServices;
using System.Text;

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// 文件扩展名结构体
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal readonly struct FileExtension : IEquatable<FileExtension>
{
    private const int MAX_LENGTH = 8;

    [FieldOffset(0)] public readonly char c0;
    [FieldOffset(2)] public readonly char c1;
    [FieldOffset(4)] public readonly char c2;
    [FieldOffset(6)] public readonly char c3;
    [FieldOffset(8)] public readonly char c4;
    [FieldOffset(10)] public readonly char c5;
    [FieldOffset(12)] public readonly char c6;
    [FieldOffset(14)] public readonly char c7;

    [FieldOffset(0)] private readonly long m1;
    [FieldOffset(8)] private readonly long m2;

    public FileExtension(ReadOnlySpan<char> text) : this() {
        if (text.Length > MAX_LENGTH) {
            throw new ArgumentException();
        }
        for (int index = 0; index < text.Length; index++) {
            char value = text[index];
            switch (index) {
                case 0: c0 = value; break;
                case 1: c1 = value; break;
                case 2: c2 = value; break;
                case 3: c3 = value; break;
                case 4: c4 = value; break;
                case 5: c5 = value; break;
                case 6: c6 = value; break;
                case 7: c7 = value; break;
                default: throw new IndexOutOfRangeException();
            }
        }
    }

    public char this[int index] {
        get {
            return index switch
            {
                0 => c0,
                1 => c1,
                2 => c2,
                3 => c3,
                4 => c4,
                5 => c5,
                6 => c6,
                7 => c7,
                _ => throw new IndexOutOfRangeException()
            };
        }
    }

    public int Length {
        get {
            if (m2 != 0) {
                // if (c4 == 0) return 4;
                if (c5 == 0) return 5;
                if (c6 == 0) return 6;
                if (c7 == 0) return 7;
                return 8;
            }
            if (m1 != 0) {
                // if (c0 == 0) return 0;
                if (c1 == 0) return 1;
                if (c2 == 0) return 2;
                if (c3 == 0) return 3;
                return 4;
            }
            return 0;
        }
    }

    public bool Equals(FileExtension other) {
        return m1 == other.m1 && m2 == other.m2;
    }

    public override bool Equals(object obj) {
        return obj is FileExtension other && Equals(other);
    }

    public override int GetHashCode() {
        return (m1.GetHashCode() * 397) ^ m2.GetHashCode();
    }

    public static bool operator ==(FileExtension left, FileExtension right) {
        return left.Equals(right);
    }

    public static bool operator !=(FileExtension left, FileExtension right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        int length = Length;
        StringBuilder sb = new StringBuilder(length);
        for (int index = 0; index < length; index++) {
            char c = this[index];
            if (c == 0) {
                break;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }
}
}