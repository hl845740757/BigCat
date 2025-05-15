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
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Wjybxx.Commons.Apt;
using Wjybxx.Commons.Poet;

namespace Wjybxx.BigCat.Apt
{
/// <summary>
/// 生成服务器的方法导出代码(lambda)
/// </summary>
public class RpcExporterGenerator
{
    private const string varName_registry = "registry";
    private const string varName_instance = "instance";
    private const string varName_context = "context";
    private const string varName_parameter = "parameter";

    private readonly RpcServiceProcessor processor;
    private readonly INamedTypeSymbol typeSymbol;

    private readonly int serviceId;
    private readonly List<IMethodSymbol> rpcMethods;
    private readonly ClassName serviceTypeName;

    public RpcExporterGenerator(RpcServiceProcessor processor, INamedTypeSymbol typeSymbol,
                                int serviceId, List<IMethodSymbol> rpcMethods) {
        this.processor = processor;
        this.typeSymbol = typeSymbol;
        this.serviceId = serviceId;
        this.rpcMethods = rpcMethods;
        this.serviceTypeName = (ClassName)AptUtils.ParseType(typeSymbol);
    }

    public TypeSpec Execute() {
        TypeSpec.Builder typeBuilder = TypeSpec.NewClassBuilder(GetServerProxyClassName(typeSymbol))
            .AddModifiers(Modifiers.Public | Modifiers.Sealed)
            .AddAttribute(processor.processorInfoAnnotation)
            .AddAttribute(AptUtils.NewSourceFileRefAnnotation(AptUtils.ParseType(typeSymbol)));

        List<MethodSpec> serverMethodProxyList = new(rpcMethods.Count);
        // 生成代理方法
        foreach (IMethodSymbol method in rpcMethods) {
            serverMethodProxyList.Add(GenServerMethodProxy(method));
        }
        typeBuilder.AddMethods(serverMethodProxyList);

        // 生成注册方法
        typeBuilder.AddMethod(GenRegisterMethod(serverMethodProxyList));
        return typeBuilder.Build();
    }

    private static string GetServerProxyClassName(INamedTypeSymbol typeSymbol) {
        return typeSymbol.Name + "Exporter";
    }

    /// <summary>
    /// 生成注册方法
    /// 
    /// <![CDATA[
    /// public static void export(RpcProxyRegistry registry, T instance) {
    ///     exportMethod1(registry, instance);
    ///     exportMethod2(registry, instance);
    /// }
    /// ]]>
    /// </summary>
    /// <param name="serverProxyMethodList"></param>
    /// <returns></returns>
    private MethodSpec GenRegisterMethod(List<MethodSpec> serverProxyMethodList) {
        MethodSpec.Builder builder = MethodSpec.NewMethodBuilder("Export")
            .AddModifiers(Modifiers.Public | Modifiers.Static)
            .Returns(TypeName.VOID)
            .AddParameter(processor.typeName_MethodRegistry, varName_registry)
            .AddParameter(serviceTypeName, varName_instance);
        // 添加调用
        foreach (MethodSpec method in serverProxyMethodList) {
            builder.codeBuilder.AddStatement("$L($L, $L)", method.name, varName_registry, varName_instance);
        }
        return builder.Build();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="method"></param>
    /// <returns></returns>
    private MethodSpec GenServerMethodProxy(IMethodSymbol method) {
        AttributeData annoValueMap = processor.GetMethodAnnoValueMap(method)!;
        int methodId = processor.GetMethodId(method, annoValueMap);
        MethodSpec.Builder builder = MethodSpec.NewMethodBuilder(GetServerProxyMethodName(methodId, method))
            .AddModifiers(Modifiers.Private | Modifiers.Static)
            .Returns(TypeName.VOID)
            .AddParameter(processor.typeName_MethodRegistry, varName_registry)
            .AddParameter(serviceTypeName, varName_instance);
        // 拷贝泛型参数
        AptUtils.CopyTypeParameters(builder, method);
        // 注册方法代理
        builder.codeBuilder.Add(GenMethodProxy(method, methodId, annoValueMap).Build());
        // 注册切面数据
        string customData = processor.GetCustomData(method, annoValueMap);
        if (customData != null) {
            builder.codeBuilder.AddStatement("$L.setProxyData($L, $L, $S)", varName_registry, serviceId, methodId, customData);
        }
        return builder.Build();
    }

    /** 生成方法代理 */
    private CodeBlock.Builder GenMethodProxy(IMethodSymbol method, int methodId,
                                             AttributeData annoValueMap) {
        CodeBlock.Builder codeBuilder = CodeBlock.NewBuilder();
        // registry -- 传入泛型参数，可以避免不必要的类型转换；要使用ref必须显式声明context的类型
        TypeName rpcReturnTypeName = AptUtils.ParseType(processor.RpcReturnType(method));
        TypeName rpcContextTypeName = processor.typeName_Context_T.WithTypeArguments(rpcReturnTypeName);
        codeBuilder.BeginControlFlow("$L.Register<$T>($L, $L, (ref $T context, object $L) =>",
            varName_registry, rpcReturnTypeName, serviceId, methodId,
            rpcContextTypeName, varName_parameter);
        // 可变性设置
        if (processor.IsResultSharable(method, annoValueMap)) {
            codeBuilder.AddStatement("context.IsSharable = true");
        }
        if (processor.IsManualReturn(method, annoValueMap)) {
            codeBuilder.AddStatement("context.IsManualReturn = true");
        }
        // 执行方法调用 -- 这里测试方法的直接返回值
        if (method.ReturnsVoid) {
            StringBuilder format = new StringBuilder(32);
            List<object> arguments = new(4);
            genInvokeStatement(method, format, arguments);
            codeBuilder.AddStatement(format.ToString(), arguments.ToArray()); // 需要ToArray
        } else {
            StringBuilder format = new StringBuilder(32);
            List<object> arguments = new(4);
            {
                format.Append("$T tempR = ");
                arguments.Add(AptUtils.ParseType(method.ReturnType));
            }
            genInvokeStatement(method, format, arguments);
            codeBuilder.AddStatement(format.ToString(), arguments.ToArray()); // 需要ToArray

            codeBuilder.AddStatement("if (context.IsManualReturn) return");
            if (processor.IsFuture(method.ReturnType)) {
                codeBuilder.AddStatement("context.SendAsyncResult(tempR)");
            } else {
                codeBuilder.AddStatement("context.SendResult(tempR)");
            }
        }
        codeBuilder.Unindent(); // endControlFlow会拼入空格...
        codeBuilder.AddStatement("})");
        return codeBuilder;
    }

    /**
     * 获取代理方法的名字
     */
    private static string GetServerProxyMethodName(int methodId, IMethodSymbol method) {
        // 加上methodId防止重复
        return "_Export" + Util.FirstCharToUpperCase(method.Name) + "_" + methodId;
    }

    /**
     * 生成方法调用代码，没有分号和换行符。
     * {@code instance.rpcMethod(a, b, c)}
     */
    private void genInvokeStatement(IMethodSymbol method, StringBuilder format, List<object> arguments) {
        // 调用方法
        format.Append("$L.$L(");
        arguments.Add(varName_instance);
        arguments.Add(method.Name);

        // 去除context -- Context的类型转换已在上面统一处理
        ImmutableArray<IParameterSymbol> parameters = method.Parameters;
        if (parameters.Length > 0 && processor.IsContext(parameters[0].Type)) {
            // context需要使用ref/in修饰
            RefKind refKind = parameters[0].RefKind;
            if (refKind == RefKind.Ref) {
                format.Append("ref ");
            } else if (refKind == RefKind.In) {
                format.Append("in ");
            }
            format.Append("context");
            if (parameters.Length > 1) {
                format.Append(", ");
            }
            parameters = parameters.RemoveAt(0);
        }
        // 方法参数已限定为最多1个，Object向下转换
        if (parameters.Length > 0) {
            IParameterSymbol variableElement = parameters[0];
            TypeName parameterTypeName = AptUtils.ParseType(variableElement.Type);

            format.Append("($T) $L");
            arguments.Add(parameterTypeName);
            arguments.Add(varName_parameter);
        }
        format.Append(")");
    }
}
}