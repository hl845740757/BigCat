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

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Wjybxx.Commons.Apt;
using Wjybxx.Commons.Poet;

namespace Wjybxx.BigCat.Apt
{
/// <summary>
/// 生成客户端使用的封包代码
/// </summary>
public class RpcProxyGenerator
{
    private readonly RpcServiceProcessor processor;
    private readonly INamedTypeSymbol typeSymbol;

    private readonly int serviceId;
    private readonly List<IMethodSymbol> rpcMethods;
    private readonly ClassName serviceTypeName;

    public RpcProxyGenerator(RpcServiceProcessor processor, INamedTypeSymbol typeSymbol,
                             int serviceId, List<IMethodSymbol> rpcMethods) {
        this.processor = processor;
        this.typeSymbol = typeSymbol;
        this.serviceId = serviceId;
        this.rpcMethods = rpcMethods;
        this.serviceTypeName = (ClassName)AptUtils.ParseType(typeSymbol);
    }

    public TypeSpec Execute() {
        TypeSpec.Builder typeBuilder = TypeSpec.NewClassBuilder(GetClientProxyClassName(typeSymbol))
            .AddModifiers(Modifiers.Public | Modifiers.Sealed)
            .AddAttribute(processor.processorInfoAnnotation)
            .AddAttribute(AptUtils.NewSourceFileRefAnnotation(AptUtils.ParseType(typeSymbol)));

        // 生成代理方法
        foreach (IMethodSymbol method in rpcMethods) {
            AttributeData annoValueMap = processor.GetMethodAnnoValueMap(method)!;
            MethodSpec proxyMethodSpec = GenClientMethodProxy(method, annoValueMap);
            typeBuilder.AddMethod(proxyMethodSpec);
        }
        return typeBuilder.Build();
    }

    private static string GetClientProxyClassName(INamedTypeSymbol typeSymbol) {
        return typeSymbol.Name + "Proxy";
    }

    /// <summary>A
    /// <![CDATA[
    /// public static MethodSpec<Response> method1(Request request) {
    ///     return new RpcMethodSpec<>(1, 2, request, false);
    /// }
    /// ]]>
    /// </summary>
    private MethodSpec GenClientMethodProxy(IMethodSymbol method, AttributeData annoValueMap) {
        MethodSpec.Builder builder = MethodSpec.NewMethodBuilder(method.Name)
            .AddModifiers(Modifiers.Public | Modifiers.Static);
        // 拷贝泛型参数
        AptUtils.CopyTypeParameters(builder, method);

        // 添加返回类型 - 带泛型
        ITypeSymbol rpcReturnType = processor.RpcReturnType(method);
        INamedTypeSymbol proxyReturnType = processor.type_MethodSpec_T.Construct(rpcReturnType);
        builder.Returns(AptUtils.ParseType(proxyReturnType));

        // 拷贝方法参数
        AptUtils.CopyParameters(builder, method);
//        builder.varargs(method.isVarArgs());

        // 去除context参数
        List<ParameterSpec> parameters = builder.parameters;
        FirstArgType firstArgType = processor.GetFirstArgType(method);
        if (firstArgType == FirstArgType.Context) {
            parameters.RemoveAt(0);
        }
        if (parameters.Count == 0) {
            // 无参(serviceId, methodId, null, true)
            builder.codeBuilder.AddStatement("return new ($L, $L, null, true)",
                serviceId, processor.GetMethodId(method, annoValueMap));
        } else {
            // 1个参数(serviceId, methodId, parameter, sharable)
            builder.codeBuilder.AddStatement("return new ($L, $L, $L, $L)",
                serviceId, processor.GetMethodId(method, annoValueMap),
                parameters[0].Name, processor.IsArgSharable(method, annoValueMap));
        }
        // 添加一个引用，方便定位 -- 不完全准确，但胜过没有
        builder.document.Add("<see cref=\"$T.$L\"/>", serviceTypeName, method.Name);
        return builder.Build();
    }
}
}