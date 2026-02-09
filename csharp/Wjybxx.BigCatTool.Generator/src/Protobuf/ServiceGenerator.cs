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
using Wjybxx.BigCatTool.Core;
using Wjybxx.BigCatTool.Protobuf;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Poet;
using Wjybxx.Dson;
using static Wjybxx.BigCatTool.DataScript.CodeGeneratorHelper;

namespace Wjybxx.BigCatTool.Generator.Protobuf
{
/// <summary>
/// 根据PB文件生成Rpc服务类
///
/// 文件规范见<code>protobuf.md</code>
/// </summary>
public class ServiceGenerator
{
    private readonly PBRepository repository;
    private readonly string outDir;
    private readonly List<string> superInterfaces;

    private readonly AttributeSpec processorInfo;
#nullable disable
    /** 当前处理的文件缓存 -- 用于查询依赖 */
    private PBFile _curFile;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository">要处理的pb文件</param>
    /// <param name="outDir">文件输出目录</param>
    /// <param name="superInterfaces">服务需要实现的接口</param>
    public ServiceGenerator(PBRepository repository, string outDir, List<string> superInterfaces = null) {
        this.repository = repository;
        this.outDir = outDir;
        this.superInterfaces = superInterfaces ?? new List<string>();
        this.processorInfo = GeneratorUtil.NewProcessorInfoAnnotation(typeof(ServiceGenerator));
    }

    public void Execute() {
        if (!Directory.Exists(outDir)) {
            Directory.CreateDirectory(outDir);
        }
        foreach (PBFile pbFile in repository.GetFiles()) {
            _curFile = pbFile;
            foreach (PBService service in pbFile.GetServices()) {
                try {
                    BuildService(service);
                }
                catch (Exception e) {
                    throw new Exception($"service: {service.Name}", e);
                }
            }
            _curFile = null;
        }
    }

    private void BuildService(PBService service) {
        Annotation serviceAnnotation = service.GetAnnotation(PBAnnations.RPC);
        if (serviceAnnotation == null) {
            return;
        }
        TypeSpec.Builder typeBuilder = TypeSpec.NewInterfaceBuilder(service.Name)
            .AddModifiers(Modifiers.Public)
            .AddAttribute(processorInfo)
            .AddDocument(BuildDocument(service.Comments));
        // 继承的接口
        foreach (string superinterface in superInterfaces) {
            ClassName className = GeneratorUtil.ClassNameOfCanonicalName(superinterface);
            typeBuilder.AddBaseClass(className);
        }
        // service注解
        DsonObject<string> serviceData = serviceAnnotation.AsObject();
        {
            AttributeSpec.Builder annoBuilder = AttributeSpec.NewBuilder(TYPE_NAME_RPC_SERVICE)
                .AddMember(PNAME_SERVICE_ID, GetServiceId(serviceData).ToString());
            typeBuilder.AddAttribute(annoBuilder.Build());
        }
        // 方法列表
        foreach (PBMethod method in service.GetMethods()) {
            Annotation methodAnnotation = method.GetAnnotation(PBAnnations.RPC);
            if (methodAnnotation == null) {
                continue;
            }
            DsonObject<string> methodData = methodAnnotation.AsObject();
            MethodSpec.Builder methodBuilder = MethodSpec.NewMethodBuilder(method.Name);
            // method注解
            {
                AttributeSpec.Builder annoBuilder = AttributeSpec.NewBuilder(TYPE_NAME_RPC_METHOD)
                    .AddMember(PNAME_METHOD_ID, GetMethodId(method.Number, methodData).ToString());
                // .addMember("ArgSharable", "false") // C#端的pb对象不是builder模式，不是不可变对象
                // .addMember("ResultSharable", "false"); 
                // 是否手动返回结果
                if (IsManualReturn(methodData, serviceData)) {
                    annoBuilder.AddMember(PNAME_MANUAL_RETURN, "true");
                }
                // 自定义数据-字符串
                Annotation custom = method.GetAnnotation(PBAnnations.RPC_CUSTOM);
                if (custom != null) {
                    annoBuilder.AddMember(PNAME_CUSTOM_DATA, "$S", custom.value);
                }
                methodBuilder.AddAttribute(annoBuilder.Build());
            }
            // 处理方法的模式
            if (IsAsyncMethod(methodData, serviceData)) {
                BuildWithAsyncMode(method, methodData, serviceData, methodBuilder);
            } else {
                BuildWithSyncMode(method, methodData, serviceData, methodBuilder);
            }
            // 方法注释
            methodBuilder.AddDocument(BuildDocument(method.Comments));
            typeBuilder.AddMethod(methodBuilder.Build());
        }
        ClassName serviceTypeName = ClassNameOfType(service.Name);
        GeneratorUtil.WriteToFile(outDir, serviceTypeName.ns, typeBuilder.Build());
    }

    private void BuildWithSyncMode(PBMethod method, DsonObject<string> methodData, DsonObject<string> serviceData,
                                   MethodSpec.Builder methodBuilder) {
        // 仅处理void
        TypeName returnType;
        if (method.ResultType != null) {
            returnType = ClassNameOfType(method.ResultType);
        } else {
            returnType = TypeName.VOID;
        }
        methodBuilder.Returns(returnType);

        // 是否需要context参数
        if (IsRequireContext(methodData, serviceData)) {
            methodBuilder.AddParameter(ParseRpcContextType(method), "rpcContext");
        }
        // 正常参数
        if (method.ParameterType != null) {
            TypeName argType = ClassNameOfType(method.ParameterType);
            string argName = method.ParameterName ?? "request";
            methodBuilder.AddParameter(argType, argName);
        }
    }

    private void BuildWithAsyncMode(PBMethod method, DsonObject<string> methodData, DsonObject<string> serviceData,
                                    MethodSpec.Builder methodBuilder) {
        // 返回值类型封装为future
        TypeName returnType;
        if (method.ResultType != null) {
            TypeName resultType = ClassNameOfType(method.ResultType);
            returnType = TYPE_NAME_VALUE_FUTURE_T.WithTypeArguments(resultType);
        } else {
            returnType = TYPE_NAME_VALUE_FUTURE;
        }
        methodBuilder.Returns(returnType);

        // 是否需要context参数--插在首位
        if (IsRequireContext(methodData, serviceData)) {
            methodBuilder.AddParameter(ParseRpcContextType(method), "rpcCtx");
        }
        // 正常参数
        if (method.ParameterType != null) {
            TypeName argType = ClassNameOfType(method.ParameterType);
            string argName = method.ParameterName ?? "request";
            methodBuilder.AddParameter(argType, argName);
        }
    }

    private TypeName ParseRpcContextType(PBMethod method) {
        TypeName contextType;
        if (method.ResultType != null) {
            TypeName resultType = ClassNameOfType(method.ResultType);
            contextType = TYPE_NAME_RPC_CONTEXT_T.WithTypeArguments(resultType);
        } else {
            // void时使用object代替 -- 可临时返回结果
            contextType = TYPE_NAME_RPC_CONTEXT_T.WithTypeArguments(TypeName.OBJECT);
        }
        // c#需要传引用
        return contextType.MakeByRefType();
    }

    #region 类名解析

    /// <summary>
    /// 根据引用的类型名字获得关联的ClassName -- 进行类型引用
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private ClassName ClassNameOfType(string type) {
        // 先尝试从当前文件查询
        PBElement topElement = repository.GetTopElement(_curFile.Name, type);
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

    #endregion

    #region RPC注解解析

    private static readonly ClassName TYPE_NAME_RPC_SERVICE = GeneratorUtil.ClassNameOfCanonicalName("Wjybxx.BigCat.Fx.RpcServiceAttribute");
    private static readonly ClassName TYPE_NAME_RPC_METHOD = GeneratorUtil.ClassNameOfCanonicalName("Wjybxx.BigCat.Fx.RpcMethodAttribute");
    //
    private static readonly ClassName TYPE_NAME_RPC_CONTEXT_T = ClassName.Get("Wjybxx.BigCat.Fx", "RpcContext",
        new List<TypeName> { TypeParameterName.Get("T") });
    //
    private static readonly ClassName TYPE_NAME_VALUE_FUTURE = ClassName.Get(typeof(ValueFuture));
    private static readonly ClassName TYPE_NAME_VALUE_FUTURE_T = ClassName.Get(typeof(ValueFuture<>));

    private const string PNAME_SERVICE_ID = "ServiceId";
    private const string PNAME_METHOD_ID = "MethodId";
    private const string PNAME_MANUAL_RETURN = "ManualReturn";
    private const string PNAME_ARG_SHARABLE = "ArgSharable";
    private const string PNAME_RESULT_SHARABLE = "ResultSharable";
    private const string PNAME_CUSTOM_DATA = "CustomData";

    // 服务上的async等用于配置默认值
    // @Rpc {id: 1, async: true, ctx: true, manual: true}
    private static int GetServiceId(DsonObject<string> methodData) {
        // 默认是double类型
        return methodData["id"].AsNumber().IntValue;
    }

    // @Rpc {id: 1, async: true, ctx: true, manual: true}
    private static int GetMethodId(int? number, DsonObject<string> methodData) {
        // 默认是double类型
        return number ?? methodData["id"].AsNumber().IntValue;
    }

    private static bool IsAsyncMethod(DsonObject<string> methodData, DsonObject<string> serviceData) {
        if (methodData.TryGetValue("async", out DsonValue value)
            || serviceData.TryGetValue("async", out value)) {
            return GetBool(value);
        }
        return false;
    }

    private static bool IsManualReturn(DsonObject<string> methodData, DsonObject<string> serviceData) {
        if (methodData.TryGetValue("manual", out DsonValue value)
            || serviceData.TryGetValue("manual", out value)) {
            return GetBool(value);
        }
        return false;
    }

    private static bool IsRequireContext(DsonObject<string> methodData, DsonObject<string> serviceData) {
        // 手动返回结果时也需要ctx -- 且方法注解的优先级高于服务的默认配置
        if (methodData.TryGetValue("ctx", out DsonValue value)
            || methodData.TryGetValue("manual", out value)) {
            return GetBool(value);
        }
        if (serviceData.TryGetValue("ctx", out value)
            || serviceData.TryGetValue("manual", out value)) {
            return GetBool(value);
        }
        return false;
    }

    private static bool GetBool(DsonValue value) {
        if (value.DsonType == DsonType.Bool) return value.AsBool();
        if (value.IsNumber) return value.AsNumber().IntValue == 1;
        return false;
    }

    #endregion
}
}