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

namespace Wjybxx.BigCatEditor.DataScript
{
/// <summary>
/// 其实等效ClassName，但开销更小
/// </summary>
public readonly struct DSTypeSymbol
{
    public readonly string symbol; // 暂不支持解析A.B.C
    public readonly string name;
    public readonly List<DSTypeSymbol>? typeArguments;
    public readonly bool isNullable;

    public DSTypeSymbol(string symbol, string name, List<DSTypeSymbol>? typeArguments, bool isNullable) {
        this.symbol = symbol;
        this.name = name;
        this.typeArguments = typeArguments;
        this.isNullable = isNullable;
    }

    public static DSTypeSymbol Parse(string typeSymbol) {
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
}
}