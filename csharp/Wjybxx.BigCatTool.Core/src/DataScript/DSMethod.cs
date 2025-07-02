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

namespace Wjybxx.BigCatTool.DataScript
{
/// <summary>
///
/// <h3>语法</h3>
/// <code>func Main(Request req) : (Response) = 1;</code>
///
/// 语法类似于protobuf的rpc方法，但有两点差异：
/// 1.方法包含参数时需要声明变量名 -- pb是没有参数名的。
/// 2.需要为方法支持一个数字编号 -- 以支持重载。
/// 3.方法至多自己出一个参数和一个结果
///
/// <h3>泛型</h3>
/// 1.方法本身都不支持泛型，但可以使用类型的泛型参数。
/// 2.应当避免在服务以外的上下文使用函数。
///
/// <h3>函数的目的</h3>
/// 由于ds文件中定义了大量的数据结构，我期望能在Rpc接口中复用这些数据结构；
/// 而之前的工具仅支持pb文件生成rpc接口，而ds和pb文件并不互通，因此才在ds文件中支持定义rpc接口和函数。
/// </summary>
public class DSMethod : DSElement
{
#nullable disable
    /** 方法参数的类型 -- 可能null */
    private readonly string? parameterTypeSymbol;
    /** 方法参数的名字 */
    private readonly string? parameterName;
    /** 方法返回值的类型 -- 可能null */
    private readonly string? resultTypeSymbol;
    /** 方法的数字编号 */
    private readonly int number;

    /** 参数类型 -- build时解析 */
    private DSTypeElement? parameterType;
    /** 返回值类型 -- build时解析 */
    private DSTypeElement? resultType;

    /** 泛型方法的原始定义 -- 可能引用了泛型参数 */
    private readonly DSMethod _originDefine;
#nullable enable

    public DSMethod(string simpleName,
                    string? parameterTypeSymbol, string? parameterName, string? resultTypeSymbol,
                    int number) : base(simpleName) {
        this.parameterTypeSymbol = parameterTypeSymbol;
        this.parameterName = parameterName;
        this.resultTypeSymbol = resultTypeSymbol;
        this.number = number;
        _originDefine = null;
    }

    public DSMethod(DSMethod originDefine, DSTypeElement? parameterType, DSTypeElement? resultType)
        : base(originDefine.SimpleName) {
        _originDefine = originDefine;
        this.parameterType = parameterType;
        this.resultType = resultType;
        this.parameterName = originDefine.parameterName;
        this.number = originDefine.number;
    }

    public override DSElementKind Kind => DSElementKind.Method;
    public override DSElement OriginDefine => _originDefine ?? this;
    public DSMethod OriginMethod => _originDefine ?? this;

    public bool HasParameter => parameterType != null;
    public bool HasResult => resultType != null;

    #region props

    public string? ParameterTypeSymbol => parameterTypeSymbol;
    public string? ParameterName => parameterName;
    public string? ResultTypeSymbol => resultTypeSymbol;
    public int Number => number;

    public DSTypeElement? ParameterType {
        get => parameterType;
        set => parameterType = value;
    }

    public DSTypeElement? ResultType {
        get => resultType;
        set => resultType = value;
    }

    #endregion

    protected override void ToString(StringBuilder sb) {
        sb.Append(", parameterType='").Append(parameterTypeSymbol).Append('\'')
            .Append(", parameterName='").Append(parameterName).Append('\'')
            .Append(", resultType='").Append(resultTypeSymbol).Append('\'')
            .Append(", number=").Append(number);
    }
}
}