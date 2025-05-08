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
using Wjybxx.BigCatEditor.Core;
using Range = Wjybxx.BigCatEditor.Core.Range;

namespace Wjybxx.BigCatEditor.Protobuf
{
/// <summary>
/// Message和Eum的公共抽象
/// </summary>
public abstract class PBTypeElement : PBElement
{
    /** 保留字段编号 */
    private readonly List<Range> reservedNumbers = new();
    /** 保留字段名 */
    private readonly List<string> reservedNames = new();

    public PBTypeElement AddReservedNumber(int number) {
        reservedNumbers.Add(new Range(number, number));
        return this;
    }

    public PBTypeElement AddReservedNumber(int start, int end) {
        reservedNumbers.Add(new Range(start, end));
        return this;
    }

    public PBTypeElement AddReservedName(string name) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        reservedNames.Add(name);
        return this;
    }

    public List<Range> ReservedNumbers => reservedNumbers;
    public List<string> ReservedNames => reservedNames;
}
}