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
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Wjybxx.Commons.Apt;
using Wjybxx.Commons.Poet;

namespace Wjybxx.BigCat.Apt
{
/// <summary>
/// RpcService注解处理器
/// </summary>
[Generator]
public class RpcServiceProcessor : ISourceGenerator
{
    #region const

    private const string CNAME_RPC_SERVICE = "Wjybxx.BigCat.Fx.RpcServiceAttribute";
    private const string PNAME_SERVICE_ID = "ServiceId";

    private const string CNAME_RPC_METHOD = "Wjybxx.BigCat.Fx.RpcMethodAttribute";
    private const string PNAME_METHOD_ID = "MethodId";
    private const string PNAME_ARG_SHARABLE = "ArgSharable";
    private const string PNAME_RESULT_SHARABLE = "ResultSharable";
    private const string PNAME_MANUAL_RETURN = "ManualReturn";
    private const string PNAME_CUSTOM_DATA = "CustomData";
    private const string PNAME_BUILDER_PATTERN = "builderPattern";

    // C#的RpcMethodSpec包含一个泛型类和一个非泛型类
    private const string CNAME_METHOD_SPEC = "Wjybxx.BigCat.Fx.RpcMethodSpec";
    private const string CNAME_METHOD_SPEC_T = "Wjybxx.BigCat.Fx.RpcMethodSpec`1";
    private const string CNAME_METHOD_REGISTRY = "Wjybxx.BigCat.Fx.RpcProxyRegistry";
    private const string CNAME_CONTEXT = "Wjybxx.BigCat.Fx.RpcContext`1";

    private const int MAX_PARAMETER_COUNT = 1; // 限制最大一个参数

    // Future有点多...
    private const string CNAME_FUTURE = "Wjybxx.Commons.Concurrent.IFuture";
    private const string CNAME_FUTURE_T = "Wjybxx.Commons.Concurrent.IFuture`1";
    private const string CNAME_VALUE_FUTURE = "Wjybxx.Commons.Concurrent.ValueFuture";
    private const string CNAME_VALUE_FUTURE_T = "Wjybxx.Commons.Concurrent.ValueFuture`1";
    private const string CNAME_TASK = "System.Threading.Tasks.Task";
    private const string CNAME_TASK_T = "System.Threading.Tasks.Task`1";

    #endregion

#nullable disable
    private INamedTypeSymbol anno_rpcServiceElement;
    private INamedTypeSymbol anno_rpcMethodElement;

    // internal INamedTypeSymbol type_MethodSpec;
    // internal ClassName typeName_MethodSpec;
    internal INamedTypeSymbol type_MethodSpec_T;
    internal ClassName typeName_MethodSpec_T;
    internal ClassName typeName_MethodRegistry;

    internal INamedTypeSymbol type_Context;
    internal ClassName typeName_Context_T;

    private ITypeSymbol type_Object;
    private List<INamedTypeSymbol> futureTypeMirrors = new(6);

    private GeneratorExecutionContext sourceProductionContext;
    private Compilation compilation;
    internal AttributeSpec processorInfoAnnotation;
    private readonly CodeWriter _codeWriter = new CodeWriter();
#nullable restore

    #region Init

    private void EnsureInited(GeneratorExecutionContext sourceProductionContext, Compilation compilation) {
        if (this.compilation != null) return;
        this.sourceProductionContext = sourceProductionContext;
        this.compilation = compilation;
        this.processorInfoAnnotation = AptUtils.NewProcessorInfoAnnotation(typeof(RpcServiceProcessor),
            assembly: compilation.Assembly.Identity.Name);
        // anno
        anno_rpcServiceElement = compilation.GetTypeByMetadataName(CNAME_RPC_SERVICE);
        anno_rpcMethodElement = compilation.GetTypeByMetadataName(CNAME_RPC_METHOD);
        // methodSpec
        // type_MethodSpec = compilation.GetTypeByMetadataName(CNAME_METHOD_SPEC);
        // typeName_MethodSpec = AptUtils.ClassNameOfCanonicalName(CNAME_METHOD_SPEC);
        type_MethodSpec_T = compilation.GetTypeByMetadataName(CNAME_METHOD_SPEC_T);
        typeName_MethodSpec_T = (ClassName)AptUtils.ParseType(type_MethodSpec_T!);
        typeName_MethodRegistry = AptUtils.ClassNameOfCanonicalName(CNAME_METHOD_REGISTRY);
        // ctx
        type_Context = compilation.GetTypeByMetadataName(CNAME_CONTEXT);
        typeName_Context_T = (ClassName)AptUtils.ParseType(type_Context!);

        type_Object = compilation.GetSpecialType(SpecialType.System_Object);
        // future
        futureTypeMirrors.Add(compilation.GetTypeByMetadataName(CNAME_FUTURE));
        futureTypeMirrors.Add(compilation.GetTypeByMetadataName(CNAME_FUTURE_T));
        futureTypeMirrors.Add(compilation.GetTypeByMetadataName(CNAME_VALUE_FUTURE));
        futureTypeMirrors.Add(compilation.GetTypeByMetadataName(CNAME_VALUE_FUTURE_T));
        futureTypeMirrors.Add(compilation.GetTypeByMetadataName(CNAME_TASK));
        futureTypeMirrors.Add(compilation.GetTypeByMetadataName(CNAME_TASK_T));
    }

    private void ReportDiagnostic(DiagnosticSeverity severity, ISymbol? symbol, int code, string msgFormat, params object[] args) {
        Location? location = symbol == null ? null : symbol.GetFirstLocation();
        DiagnosticDescriptor descriptor = new DiagnosticDescriptor("RpcApt" + code, "", msgFormat, "RpcApt", severity, true);
        sourceProductionContext.ReportDiagnostic(Diagnostic.Create(descriptor, location, args));
    }

    private void ReportException(Exception ex, ISymbol? symbol) {
        ReportDiagnostic(DiagnosticSeverity.Error, symbol, 0001, "Processor Caught Exception message: {0}, stackTrace: {1}",
            ex.Message, ex.StackTrace);
    }

    private bool IsBuildingAssemblyNode(INamedTypeSymbol typeSymbol) {
        IAssemblySymbol buildingAssembly = compilation.Assembly;
        IAssemblySymbol nodeAssembly = typeSymbol.ContainingAssembly;
        return buildingAssembly.Name == nodeAssembly.Name;
        // return nodeAssembly.Equals(buildingAssembly, SymbolEqualityComparer.Default);
    }

    public void Initialize(GeneratorInitializationContext context) {
        context.RegisterForSyntaxNotifications(() => new OptionsSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context) {
        // 在unity下可能处理到其它程序集文件
        if (context.Compilation.GetTypeByMetadataName(CNAME_RPC_SERVICE) == null) {
            return;
        }
        EnsureInited(context, context.Compilation);
        if (context.SyntaxReceiver is not OptionsSyntaxReceiver optionsSyntaxReceiver) {
            return;
        }
        foreach (var declarationSyntax in optionsSyntaxReceiver.typeDeclarationNodes) {
            var semanticModel = context.Compilation.GetSemanticModel(declarationSyntax.SyntaxTree);
            var typeSymbol = semanticModel.GetDeclaredSymbol(declarationSyntax) as INamedTypeSymbol;
            if (typeSymbol == null) {
                continue;
            }
            if (!IsBuildingAssemblyNode(typeSymbol)) {
                continue;
            }
            if (AptUtils.HasUsedForReflectionAttribute(typeSymbol.GetAttributes())) {
                continue;
            }
            var attributeData = AptUtils.GetAttribute(typeSymbol.GetAttributes(), CNAME_RPC_SERVICE);
            if (attributeData == null) {
                continue;
            }
            try {
                List<IMethodSymbol> rpcMethods = CheckBase(typeSymbol);
                GenProxyClass(typeSymbol, rpcMethods);
            }
            catch (Exception ex) {
                ReportException(ex, typeSymbol);
            }
        }
    }

    private class OptionsSyntaxReceiver : ISyntaxReceiver
    {
        public readonly List<TypeDeclarationSyntax> typeDeclarationNodes = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
            // 3.8.0 API太原始了...我们把所有有注解的类型都扫描进去，然后在Execute的时候通过语义模型处理
            if (syntaxNode is TypeDeclarationSyntax classDecl && classDecl.AttributeLists.Count > 0) {
                typeDeclarationNodes.Add(classDecl);
            }
        }
    }

    #endregion

    #region check

    /** @return rpc方法 - 避免每次查找，开销较大 */
    private List<IMethodSymbol> CheckBase(INamedTypeSymbol typeSymbol) {
        List<IMethodSymbol> allMethodList = CollectMethods(typeSymbol);
        List<IMethodSymbol> rpcMethodList = new(allMethodList.Count);
        HashSet<int> methodIdSet = new();
        foreach (IMethodSymbol method in allMethodList) {
            AttributeData? annoValueMap = GetMethodAnnoValueMap(method);
            if (annoValueMap == null) { // 不是rpc方法
                continue;
            }
            int methodId = GetMethodId(method, annoValueMap);
            if (methodId == 0) { // 未正确初始化
                continue;
            }
            if (method.IsStatic) { // 不可以是静态的
                ReportDiagnostic(DiagnosticSeverity.Error, method, 1001, "RpcMethod: method can't be static!");
                continue;
            }
            if (!method.IsPublic()) { // 必须是public
                ReportDiagnostic(DiagnosticSeverity.Error, method, 1002, "RpcMethod: method must be public!");
                continue;
            }
            if (methodId < 0 || methodId > 9999) {
                ReportDiagnostic(DiagnosticSeverity.Error, method, 1003, "RpcMethod: id must between [0,9999]!");
                continue;
            }
            if (!methodIdSet.Add(methodId)) { // 同一个类中的方法id不可以重复
                ReportDiagnostic(DiagnosticSeverity.Error, method, 1004, "RpcMethod: id is duplicate!");
                continue;
            }
            CheckParameters(method);
            CheckReturnType(method);
            rpcMethodList.Add(method);
        }
        return rpcMethodList;
    }

    private List<IMethodSymbol> CollectMethods(INamedTypeSymbol typeSymbol) {
        List<IMethodSymbol> result = new List<IMethodSymbol>();
        // 超类中的方法
        foreach (ISymbol symbol in BeanUtils.GetAllMembersWithInherit(typeSymbol, new List<SymbolKind>() { SymbolKind.Method })) {
            IMethodSymbol methodSymbol = (IMethodSymbol)symbol;
            result.Add(methodSymbol);
        }
        // 接口中的方法
        foreach (INamedTypeSymbol @interface in typeSymbol.AllInterfaces) {
            foreach (ISymbol symbol in @interface.GetMembers()) {
                if (symbol.Kind == SymbolKind.Method) {
                    result.Add((IMethodSymbol)symbol);
                }
            }
        }
        return result;
    }

    private void CheckParameters(IMethodSymbol method) {
        ImmutableArray<IParameterSymbol> parameters = method.Parameters;
        if (parameters.Length == 0) {
            return;
        }
        FirstArgType firstArgType = GetFirstArgType(method);
        // 检测方法参数个数
        int maxParameterCount = firstArgType == FirstArgType.Context ? MAX_PARAMETER_COUNT + 1 : MAX_PARAMETER_COUNT;
        if (parameters.Length > maxParameterCount) {
            ReportDiagnostic(DiagnosticSeverity.Error, method, 1011, "RpcMethod: method has too many parameters");
        }
        // C# void不能作为泛型参数...因此无需检查void，但应该检查是否使用了in/ref修饰context；警告级别
        if (firstArgType == FirstArgType.Context
            && parameters[0].RefKind == RefKind.None) {
            ReportDiagnostic(DiagnosticSeverity.Warning, method, 1012, "RpcMethod: context does not use the 'in' or 'ref' modifier");
        }

        // 检查后续是否存在context参数
        for (int idx = firstArgType == FirstArgType.Context ? 1 : 0; idx < parameters.Length; idx++) {
            var parameterSymbol = parameters[idx];
            if (IsContext(parameterSymbol.Type)) {
                ReportDiagnostic(DiagnosticSeverity.Error, method, 1013, "RpcMethod: context must be declared as the first parameter");
            }
            // 其实本地还是支持基本类型的
        }
    }

    private void CheckReturnType(IMethodSymbol method) {
        // C# void不能作为泛型参数...因此无需检查
        // 其实本地还是支持基本类型的
    }

    #endregion

    #region gen

    private void GenProxyClass(INamedTypeSymbol typeElement, List<IMethodSymbol> rpcMethodList) {
        AttributeData serviceAnnoMirror = AptUtils.GetAttribute(typeElement.GetAttributes(), CNAME_RPC_SERVICE)!;
        if (!AptUtils.GetAttributeValue(serviceAnnoMirror, PNAME_SERVICE_ID, out TypedConstant annoValue)) {
            return;
        }
        int serviceId = (int)annoValue.Value!;
        var builder = new RpcProxyGenerator(this, typeElement, serviceId, rpcMethodList)
            .Execute()
            .AddSpec(new CodeBlockSpec(CodeBlock.Of("\n"))); // 插入换行符

        new RpcExporterGenerator(this, typeElement, serviceId, rpcMethodList)
            .Execute(builder);
        WriteToFile(builder.Build(), typeElement);
    }

    private void WriteToFile(TypeSpec typeSpec, INamedTypeSymbol typeSymbol) {
        string outputNamespace = typeSymbol.ContainingNamespace.ToDisplayString();
        CsharpFile csharpFile = CsharpFile.NewBuilder(typeSpec.Name)
            .AddSpec(NamespaceSpec.Of(outputNamespace, typeSpec))
            .Build();

        _codeWriter.Reset();
        sourceProductionContext.AddSource(typeSpec.Name,
            _codeWriter.Write(csharpFile));
    }

    #endregion

    #region 注解解析

    internal AttributeData? GetMethodAnnoValueMap(IMethodSymbol method) {
        return AptUtils.GetAttribute(method.GetAttributes(), CNAME_RPC_METHOD);
    }

    internal int GetMethodId(IMethodSymbol method, AttributeData attributeData) {
        if (AptUtils.GetAttributeValue(attributeData, PNAME_METHOD_ID, out TypedConstant r)) {
            return (int)r.Value!;
        }
        return 0;
    }

    /** 获取方法切面数据 */
    internal string? GetCustomData(IMethodSymbol method, AttributeData annoValueMap) {
        if (AptUtils.GetAttributeValue(annoValueMap, PNAME_CUSTOM_DATA, out TypedConstant r)) {
            return (string?)r.Value;
        }
        return null;
    }

    /** 是否使用builder模式 -- c#端不使用 */
    internal bool IsBuilderPattern(IMethodSymbol method, AttributeData annoValueMap) {
        if (AptUtils.GetAttributeValue(annoValueMap, PNAME_BUILDER_PATTERN, out TypedConstant r)) {
            return (bool)r.Value!;
        }
        return false;
    }

    /** 是否手动返回结果 */
    internal bool IsManualReturn(IMethodSymbol method, AttributeData annoValueMap) {
        if (AptUtils.GetAttributeValue(annoValueMap, PNAME_MANUAL_RETURN, out TypedConstant r)) {
            return (bool)r.Value!;
        }
        return false;
    }

    /** 方法参数是否可共享 */
    internal bool IsArgSharable(IMethodSymbol method, AttributeData annoValueMap) {
        if (AptUtils.GetAttributeValue(annoValueMap, PNAME_ARG_SHARABLE, out TypedConstant r)) {
            return (bool)r.Value!;
        }
        return false;
    }

    /** 方法结果是否可共享 */
    internal bool IsResultSharable(IMethodSymbol method, AttributeData annoValueMap) {
        if (AptUtils.GetAttributeValue(annoValueMap, PNAME_RESULT_SHARABLE, out TypedConstant r)) {
            return (bool)r.Value!;
        }
        return false;
    }

    #endregion

    /** 是否是RpcContext类型 */
    internal bool IsContext(ITypeSymbol type) {
        return type.OriginalDefinition.IsSubTypeOf(type_Context);
    }

    /** 是否是Future类型 */
    internal bool IsFuture(ITypeSymbol type) {
        type = type.OriginalDefinition;
        foreach (var futureTypeMirror in futureTypeMirrors) {
            if (type.IsSubTypeOf(futureTypeMirror)) {
                return true;
            }
        }
        return false;
    }

    /** 方法首个参数的类型 -- 主要测试是否是Context */
    internal FirstArgType GetFirstArgType(IMethodSymbol method) {
        ImmutableArray<IParameterSymbol> parameters = method.Parameters;
        if (parameters.Length == 0) return FirstArgType.None;
        if (IsContext(parameters[0].Type)) return FirstArgType.Context;
        return FirstArgType.Other;
    }

    /** rpc的返回值 -- 不是方法的直接返回值 */
    internal ITypeSymbol RpcReturnType(IMethodSymbol method) {
        ITypeSymbol returnType = method.ReturnType;
        if (returnType.SpecialType == SpecialType.System_Void) {
            // 包含context时，context的泛型值作为返回值类型
            ImmutableArray<IParameterSymbol> parameters = method.Parameters;
            if (parameters.Length > 0 && IsContext(parameters[0].Type)) {
                INamedTypeSymbol namedTypeSymbol = (INamedTypeSymbol)parameters[0].Type;
                return namedTypeSymbol.TypeArguments[0];
            }
            // void转object
            return type_Object;
        }
        // future类型，future的泛型值作为返回值类型 -- 可能是非泛型的Future
        if (IsFuture(returnType)) {
            INamedTypeSymbol namedTypeSymbol = (INamedTypeSymbol)returnType;
            ImmutableArray<ITypeSymbol> typeArguments = namedTypeSymbol.TypeArguments;
            if (typeArguments.Length > 0) {
                return typeArguments[0];
            }
            // void转object
            return type_Object;
        } else {
            return returnType;
        }
    }
}
}