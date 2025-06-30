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
using System.Collections.Generic;
using Wjybxx.Commons;

namespace Wjybxx.BigCatTool.DataScript
{
/// <summary>
/// 其实等效ClassName，但开销更小
/// </summary>
internal readonly struct DSTypeSymbol : IEquatable<DSTypeSymbol>
{
    public readonly string symbol; // List<String>
    public readonly string name; // List 支持A.B.C
    public readonly List<DSTypeSymbol>? typeArguments; // String
    public readonly bool isNullable;

    public DSTypeSymbol(string symbol, string name, List<DSTypeSymbol>? typeArguments, bool isNullable) {
        this.symbol = symbol;
        this.name = name;
        this.typeArguments = typeArguments;
        this.isNullable = isNullable;
    }

    public bool HasTypeArguments => typeArguments != null && typeArguments.Count > 0;
    public int TypeArgumentCount => typeArguments != null ? typeArguments.Count : 0;

    public static DSTypeSymbol Parse(string typeSymbol) {
        typeSymbol = ObjectUtil.DeleteWhitespace(typeSymbol);
        int startIdx = typeSymbol.IndexOf('<');
        bool isNullable = typeSymbol.EndsWith('?');
        if (startIdx < 0) {
            string name = isNullable ? typeSymbol.Substring2(0, typeSymbol.Length - 1) : typeSymbol;
            return new DSTypeSymbol(typeSymbol, name, null, isNullable);
        }
        List<DSTypeSymbol> typeArguments = new List<DSTypeSymbol>();
        // 需要通过出入栈确定范围
        int stack = 1;
        int endIdx = startIdx + 1;
        int argStart = startIdx + 1;
        for (int len = typeSymbol.Length; endIdx < len; endIdx++) {
            char c = typeSymbol[endIdx];
            if (c == '<') {
                stack++;
            } else if (c == '>') { // 同时切割了'?'
                if (stack == 1) {
                    DSTypeSymbol typeArgument = Parse(typeSymbol.Substring2(argStart, endIdx));
                    typeArguments.Add(typeArgument);
                    argStart = -1;
                }
                stack--;
                if (stack == 0) {
                    break;
                }
            } else if (c == ',') { // 同时切割了'?'
                if (stack == 1) {
                    DSTypeSymbol typeArgument = Parse(typeSymbol.Substring2(argStart, endIdx));
                    typeArguments.Add(typeArgument);
                    argStart = -1;
                }
            } else {
                if (stack == 1 && argStart < 0) {
                    argStart = endIdx;
                }
            }
        }
        // 处理校验
        if (isNullable) endIdx++;
        if (endIdx + 1 != typeSymbol.Length) {
            throw new ArgumentException("invalid typeSymbol: " + typeSymbol);
        }
        {
            string name = typeSymbol.Substring2(0, startIdx);
            return new DSTypeSymbol(typeSymbol, name, typeArguments, isNullable);
        }
    }

    #region equals

    public bool Equals(DSTypeSymbol other) {
        return symbol == other.symbol
               && name == other.name
               && isNullable == other.isNullable
               && SequenceEqual(typeArguments, other.typeArguments);
    }

    public override bool Equals(object? obj) {
        return obj is DSTypeSymbol other && Equals(other);
    }

    public override int GetHashCode() {
        unchecked {
            int hashCode = symbol.GetHashCode();
            hashCode = (hashCode * 397) ^ name.GetHashCode();
            hashCode = (hashCode * 397) ^ isNullable.GetHashCode();
            hashCode = (hashCode * 397) ^ HashCode(typeArguments);
            return hashCode;
        }
    }

    #endregion

    public override string ToString() {
        return $"{nameof(symbol)}: {symbol},"
               + $" {nameof(name)}: {name},"
               + $" {nameof(typeArguments)}: {typeArguments},"
               + $" {nameof(isNullable)}: {isNullable}";
    }

    private static int HashCode(List<DSTypeSymbol>? typeArguments) {
        if (typeArguments == null) return 0;
        int r = 1;
        foreach (DSTypeSymbol typeArgument in typeArguments) {
            r = r * 397 ^ typeArgument.GetHashCode();
        }
        return r;
    }

    private static bool SequenceEqual(List<DSTypeSymbol>? lhs, List<DSTypeSymbol>? rhs) {
        if (ReferenceEquals(lhs, rhs)) return true;
        if (lhs == null || rhs == null) return false;
        int count = lhs.Count;
        if (count != rhs.Count) return false;
        for (int idx = 0; idx < count; idx++) {
            if (!lhs[idx].Equals(rhs[idx])) return false;
        }
        return true;
    }
}
}