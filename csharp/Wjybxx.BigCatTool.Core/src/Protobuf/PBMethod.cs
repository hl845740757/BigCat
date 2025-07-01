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

namespace Wjybxx.BigCatTool.Protobuf
{
/// <summary>
/// RPC方法
/// </summary>
public class PBMethod : PBElement
{
#nullable disable
    /** 方法参数的类型 -- 可能null */
    private string? parameterType;
    /** 方法参数的名字 -- pb的grpc默认不支持参数name，真扯淡 */
    private string? parameterName;
    /** 方法返回值的类型 -- 可能null */
    private string? resultType;
    /** 方法的数字编号 */
    private int? number;
#nullable enable

    public override PBElementKind Kind => PBElementKind.Method;

    public bool HasParameter => !string.IsNullOrWhiteSpace(parameterType);
    public bool HasResult => !string.IsNullOrWhiteSpace(resultType);

    #region Props

    public string? ParameterType {
        get => parameterType;
        set => parameterType = value;
    }
    public string? ParameterName {
        get => parameterName;
        set => parameterName = value;
    }
    public string? ResultType {
        get => resultType;
        set => resultType = value;
    }

    public int? Number {
        get => number;
        set => number = value;
    }

    #endregion

    protected override void ToString(StringBuilder sb) {
        sb.Append(", parameterType='").Append(parameterType).Append('\'')
            .Append(", parameterName='").Append(parameterName).Append('\'')
            .Append(", resultType='").Append(resultType).Append('\'')
            .Append(", number=").Append(number);
    }
}
}