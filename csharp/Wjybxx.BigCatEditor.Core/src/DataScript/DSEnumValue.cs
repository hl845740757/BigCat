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

using System.Text;

namespace Wjybxx.BigCatEditor.DataScript
{
/// <summary>
/// 枚举值
/// </summary>
public class DSEnumValue : DSElement
{
    /** 数字id */
    private readonly int number;

    public DSEnumValue(string simpleName, int number) : base(simpleName) {
        this.number = number;
    }

    public override DSElementKind Kind => DSElementKind.EnumValue;
    public int Number => number;

    protected override void ToString(StringBuilder sb) {
        sb.Append(", number=").Append(number);
    }
}
}