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
using System.IO;
using Wjybxx.BigCatEditor.Core;
using Wjybxx.BigCatEditor.Protobuf;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Poet;
using Wjybxx.Dson;
using CoreUtil = Wjybxx.BigCatEditor.Core.Util;

namespace Wjybxx.BigCatEditor.Generator.Protobuf
{
/// <summary>
/// 根据PB文件生成Rpc服务类
///
/// 文件规范见<code>protobuf.md</code>
/// </summary>
public class ServiceGenerator
{
    private static readonly ClassName anno_rpcService = GeneratorUtil.ClassNameOfCanonicalName("Wjybxx.BigCat.Fx.RpcServiceAttribute");
    private static readonly ClassName anno_rpcMethod = GeneratorUtil.ClassNameOfCanonicalName("Wjybxx.BigCat.Fx.RpcMethodAttribute");
    //
    private static readonly ClassName clsName_rpcContext_t = ClassName.Get("Wjybxx.BigCat.Fx", "RpcContext",
        new List<TypeName> { TypeParameterName.Get("T") });
    //
    private static readonly ClassName clsName_valueFuture = ClassName.Get(typeof(ValueFuture));
    private static readonly ClassName clsName_valueFuture_t = ClassName.Get(typeof(ValueFuture<>));

    private readonly PBRepository repository;
    private readonly string outDir;
    private readonly ServiceGeneratorHandler? handler;
    private readonly AttributeSpec processorInfo;

#nullable disable
    /** 当前处理的文件缓存 -- 用于查询依赖 */
    private PBFile _curFile;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository">要处理的pb文件</param>
    /// <param name="outDir">文件输出目录</param>
    /// <param name="handler">钩子函数</param>
    public ServiceGenerator(PBRepository repository, string outDir, ServiceGeneratorHandler? handler) {
        this.repository = repository;
        this.outDir = outDir;
        this.handler = handler;

        processorInfo = GeneratorUtil.NewProcessorInfoAnnotation(typeof(ServiceGenerator));
    }

    public void Execute() {
        foreach (PBFile pbFile in repository.GetFiles()) {
            _curFile = pbFile;
            foreach (PBService service in pbFile.GetServices()) {
                try {
                    BuildService(service);
                }
                catch (Exception e) {
                    throw new Exception($"service: {service.SimpleName}", e);
                }
            }
            _curFile = null;
        }
    }

    private void BuildService(PBService service) {
        Annotation serviceAnnotation = service.GetAnnotation("RpcService");
        if (serviceAnnotation == null) {
            return;
        }
        TypeSpec.Builder typeBuilder = TypeSpec.NewInterfaceBuilder(service.SimpleName)
            .AddModifiers(Modifiers.Public)
            .AddAttribute(processorInfo);
        // 继承的接口
        foreach (string superinterface in service.Superinterfaces) {
            ClassName className = GeneratorUtil.ClassNameOfCanonicalName(superinterface);
            typeBuilder.AddBaseClass(className);
        }
        // service注解
        {
            DsonObject<string> serviceData = serviceAnnotation.DsonValue.AsObject();
            AttributeSpec.Builder annoBuilder = AttributeSpec.NewBuilder(anno_rpcService)
                .AddMember("ServiceId", GetServiceId(service, serviceData).ToString());
            typeBuilder.AddAttribute(annoBuilder.Build());
        }
        // 方法列表
        foreach (PBMethod method in service.GetMethods()) {
            Annotation methodAnnotation = method.GetAnnotation("RpcMethod");
            if (methodAnnotation == null) {
                continue;
            }
            DsonObject<string> methodData = methodAnnotation.DsonValue.AsObject();
            MethodSpec.Builder methodBuilder = MethodSpec.NewMethodBuilder(method.SimpleName);
            // .AddModifiers(Modifiers.Public | Modifiers.Abstract); // C#端的Poet是我自己实现的
            // method注解 
            {
                AttributeSpec.Builder annoBuilder = AttributeSpec.NewBuilder(anno_rpcMethod)
                    .AddMember("MethodId", GetMethodId(method, methodData).ToString());
                // .addMember("ArgSharable", "true") // C#端的pb对象不是builder模式，不是不可变对象
                // .addMember("ResultSharable", "true"); 
                // 是否手动返回结果
                if (IsManualReturn(method, methodData)) {
                    annoBuilder.AddMember("ManualReturn", "true");
                }
                // 自定义数据-字符串
                Annotation custom = method.GetAnnotation("RpcCustom");
                if (custom != null) {
                    annoBuilder.AddMember("CustomData", "$S", custom.value);
                }
                methodBuilder.AddAttribute(annoBuilder.Build());
            }
            // 处理方法的模式
            if (IsAsyncMethod(method, methodData)) {
                BuildWithAsyncMode(method, methodData, methodBuilder);
            } else {
                BuildWithSyncMode(method, methodData, methodBuilder);
            }
            // 方法注释
            if (method.Comments.Count > 0) {
                methodBuilder.AddDocument(BuildComment(method.Comments));
            }
            typeBuilder.AddMethod(methodBuilder.Build());
        }
        // 接口注释
        if (service.Comments.Count > 0) {
            typeBuilder.AddDocument(BuildComment(service.Comments));
        }
        ClassName serviceTypeName = ClassNameOfType(service.SimpleName);
        GeneratorUtil.WriteToFile(outDir, serviceTypeName.ns, typeBuilder.Build());
    }

    private CodeBlock BuildComment(List<string> comments) {
        CodeBlock.Builder codeBuilder = CodeBlock.NewBuilder();
        foreach (string comment in comments) {
            if (Annotation.IsAnnotationComment(comment)) {
                continue;
            }
            if (!codeBuilder.IsEmpty) {
                codeBuilder.Add("\n");
            }
            // 跳过双斜杠
            int start = CoreUtil.IndexOfNonWhitespace(comment, 2);
            codeBuilder.Add(comment.Substring(start).TrimEnd());
        }
        return codeBuilder.Build();
    }

    private void BuildWithSyncMode(PBMethod method, DsonObject<string> methodData, MethodSpec.Builder methodBuilder) {
        // 仅处理void
        TypeName returnType;
        if (method.ResultType != null) {
            returnType = ClassNameOfType(method.ResultType);
        } else {
            returnType = TypeName.VOID;
        }
        methodBuilder.Returns(returnType);

        // 是否需要context参数
        if (IsRequireContext(method, methodData)) {
            methodBuilder.AddParameter(ParseRpcContextType(method), "rpcContext");
        }
        // 正常参数
        if (method.ParameterType != null) {
            ClassName argType = ClassNameOfType(method.ParameterType);
            string argName = method.ParameterName ?? handler?.ParameterName(method.ParameterType) ?? "request";
            methodBuilder.AddParameter(argType, argName);
        }
    }

    private void BuildWithAsyncMode(PBMethod method, DsonObject<string> methodData, MethodSpec.Builder methodBuilder) {
        // 返回值类型封装为future
        TypeName returnType;
        if (method.ResultType != null) {
            ClassName resultType = ClassNameOfType(method.ResultType);
            returnType = clsName_valueFuture_t.WithTypeArguments(resultType);
        } else {
            returnType = clsName_valueFuture;
        }
        methodBuilder.Returns(returnType);

        // 是否需要context参数--重复
        if (IsRequireContext(method, methodData)) {
            methodBuilder.AddParameter(ParseRpcContextType(method), "rpcCtx");
        }
        // 正常参数
        if (method.ParameterType != null) {
            ClassName argType = ClassNameOfType(method.ParameterType);
            string argName = method.ParameterName ?? handler?.ParameterName(method.ParameterType) ?? "request";
            methodBuilder.AddParameter(argType, argName);
        }
    }

    private TypeName ParseRpcContextType(PBMethod method) {
        TypeName contextType;
        if (method.ResultType != null) {
            ClassName resultType = ClassNameOfType(method.ResultType);
            contextType = clsName_rpcContext_t.WithTypeArguments(resultType);
        } else {
            // void时使用object代替 -- 可临时返回结果
            contextType = clsName_rpcContext_t.WithTypeArguments(TypeName.OBJECT);
        }
        // c#需要传引用
        return contextType.MakeByRefType();
    }

    #region 注解解析

    //@RpcService {id: 1, exporter: true, proxy: true}
    private int GetServiceId(PBService service, DsonObject<string> serviceData) {
        // 默认是double类型
        return serviceData["id"].AsDsonNumber().IntValue;
    }

    //@RpcMethod {id: 1, async: true, ctx: true, manual: true}
    private int GetMethodId(PBMethod method, DsonObject<string> methodData) {
        // 默认是double类型
        return methodData["id"].AsDsonNumber().IntValue;
    }

    private bool IsAsyncMethod(PBMethod method, DsonObject<string> methodData) {
        if (!methodData.TryGetValue("async", out DsonValue value)) {
            return handler != null && handler.IsAsyncMethod(method); // 默认false
        }
        return GetBool(value);
    }

    private bool IsManualReturn(PBMethod method, DsonObject<string> methodData) {
        if (!methodData.TryGetValue("manual", out DsonValue value)) {
            return handler != null && handler.IsManualReturn(method); // 默认false
        }
        return GetBool(value);
    }

    private bool IsRequireContext(PBMethod method, DsonObject<string> methodData) {
        if (!methodData.TryGetValue("ctx", out DsonValue value)) {
            return handler != null && handler.IsRequireContext(method); // 默认false
        }
        return GetBool(value);
    }

    private static bool GetBool(DsonValue value) {
        if (value.DsonType == DsonType.Bool) return value.AsBool();
        if (value.IsNumber) return value.AsDsonNumber().IntValue == 1;
        return false;
    }

    #endregion

    /// <summary>
    /// 根据引用的类型名字获得关联的ClassName -- 进行类型引用
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private ClassName ClassNameOfType(string type) {
        // 先尝试从当前文件查询
        PBElement topElement = repository.GetTopElement(_curFile.SimpleName, type);
        if (topElement != null) {
            return ClassName.Get(GetNamespace(_curFile), type);
        }
        // 尝试从依赖的文件中查询--这里会产生一些临时字符串，不重要
        foreach (string fileName in _curFile.ResolvedImports) {
            string simpleName = Path.GetFileNameWithoutExtension(fileName);
            topElement = repository.GetTopElement(simpleName, type);
            if (topElement == null) {
                continue;
            }
            PBFile pbFile = repository.GetFile(simpleName);
            return ClassName.Get(GetNamespace(pbFile), type);
        }
        // 不存在的依赖
        throw new ArgumentException("class not found, type " + type);
    }

    private static string GetNamespace(PBFile pbFile) {
        // 处理文件中定义了命名空间的情况
        string package = pbFile.GetOption(PBKeywords.CSHARP_NAMESPACE);
        if (string.IsNullOrWhiteSpace(package)) {
            package = pbFile.GetOption(PBKeywords.PACKAGE);
        }
        if (string.IsNullOrWhiteSpace(package)) {
            throw new InvalidOperationException("namespace is absent");
        }
        return package;
    }
}
}