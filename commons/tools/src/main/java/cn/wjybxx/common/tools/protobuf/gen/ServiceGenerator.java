/*
 * Copyright 2023 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.common.tools.protobuf.gen;

import cn.wjybxx.common.rpc.RpcContext;
import cn.wjybxx.common.rpc.RpcGenericContext;
import cn.wjybxx.common.rpc.RpcMethod;
import cn.wjybxx.common.rpc.RpcService;
import cn.wjybxx.common.tools.protobuf.*;
import cn.wjybxx.common.tools.util.GenClassUtils;
import com.squareup.javapoet.*;

import javax.lang.model.element.Modifier;
import java.io.File;
import java.io.IOException;
import java.util.List;

/**
 * 生成rpc服务接口
 *
 * @author wjybxx
 * date - 2023/10/13
 */
public class ServiceGenerator extends AbstractGenerator {

    private static final ClassName anno_rpcService = ClassName.get(RpcService.class);
    private static final ClassName anno_rpcMethod = ClassName.get(RpcMethod.class);

    private static final ClassName clsName_rpcContext = ClassName.get(RpcContext.class);
    private static final ClassName clsName_rpcGenericContext = ClassName.get(RpcGenericContext.class);

    private final File javaOutDir;
    private final AnnotationSpec generatorInfo;

    public ServiceGenerator(PBParserOptions options, PBRepository repository) {
        super(options, repository);
        this.javaOutDir = new File(options.getJavaOut());
        this.generatorInfo = GenClassUtils.newGeneratorInfoAnnotation(getClass());
    }

    public void build() throws IOException {
        for (PBFile pbFile : repository.getSortedFiles()) {
            for (PBService service : pbFile.getServices()) {
                buildService(service);
            }
        }
    }

    private void buildService(PBService service) throws IOException {
        TypeSpec.Builder typeBuilder = TypeSpec.interfaceBuilder(service.getSimpleName())
                .addModifiers(Modifier.PUBLIC)
                .addAnnotation(generatorInfo);
        // 继承的接口
        for (String superinterface : service.getSuperinterfaces()) {
            ClassName className = GenClassUtils.classNameOfCanonicalName(superinterface);
            typeBuilder.addSuperinterface(className);
        }
        // service注解
        {
            AnnotationSpec.Builder annoBuilder = AnnotationSpec.builder(anno_rpcService)
                    .addMember("serviceId", Integer.toString(service.getServiceId()));
            if (!service.isGenExporter()) {
                annoBuilder.addMember("genExporter", "false");
            }
            if (!service.isGenProxy()) {
                annoBuilder.addMember("genProxy", "false");
            }
            typeBuilder.addAnnotation(annoBuilder.build());
        }
        // 方法列表
        for (PBMethod method : service.getMethods()) {
            MethodSpec.Builder methodBuilder = MethodSpec.methodBuilder(method.getSimpleName())
                    .addModifiers(Modifier.PUBLIC, Modifier.ABSTRACT); // 不太智能，需要显式添加
            // method注解 -- javapoet生成的注解格式不友好，又不能控制...
            {
                AnnotationSpec.Builder annoBuilder = AnnotationSpec.builder(anno_rpcMethod)
                        .addMember("methodId", Integer.toString(method.getMethodId()))
                        .addMember("argSharable", "true")
                        .addMember("resultSharable", "true"); // 无参数和返回值时也设置为可共享，可避免不必要的序列化
                PBAnnotation sparam = method.getAnnotation(AnnotationTypes.SPARAM);
                if (sparam != null) {
                    annoBuilder.addMember("customData", "$S", sparam.value);
                }
                methodBuilder.addAnnotation(annoBuilder.build());
            }
            // 处理方法的模式
            switch (method.getMode()) {
                case PBMethod.MODE_CONTEXT -> buildWithContextMode(method, methodBuilder);
                case PBMethod.MODE_FUTURE -> buildWithFutureMode(method, methodBuilder);
                default -> buildWithDefMode(method, methodBuilder);
            }
            // 方法注释
            if (method.getComments().size() > 0) {
                methodBuilder.addJavadoc(buildComment(method.getComments()));
            }
            typeBuilder.addMethod(methodBuilder.build());
        }
        // 接口注释
        if (service.getComments().size() > 0) {
            typeBuilder.addJavadoc(buildComment(service.getComments()));
        }
        GenClassUtils.writeToFile(javaOutDir, typeBuilder.build(), options.getJavaPackage());
    }

    private CodeBlock buildComment(List<String> comments) {
        CodeBlock.Builder codeBuilder = CodeBlock.builder();
        for (String comment : comments) {
            if (PBParser.isAnnotationComment(comment)) {
                continue;
            }
            if (!codeBuilder.isEmpty()) {
                codeBuilder.add("\n");
            }
            codeBuilder.add(comment.substring(2).stripLeading());
        }
        return codeBuilder.build();
    }

    private void buildWithDefMode(PBMethod method, MethodSpec.Builder methodBuilder) {
        // 仅处理void
        TypeName returnType;
        if (method.getResultType() != null) {
            returnType = classNameOfType(method.getResultType());
        } else {
            returnType = TypeName.VOID;
        }
        methodBuilder.returns(returnType);

        // 是否需要context参数
        if (method.isCtx()) {
            methodBuilder.addParameter(clsName_rpcGenericContext, "rpcCtx");
        }
        // 正常参数
        if (method.getArgType() != null) {
            ClassName argType = classNameOfType(method.getArgType());
            methodBuilder.addParameter(argType, method.getArgName());
        }
    }

    private void buildWithFutureMode(PBMethod method, MethodSpec.Builder methodBuilder) {
        // 返回值类型封装为future
        TypeName returnType;
        if (method.getResultType() != null) {
            ClassName resultType = classNameOfType(method.getResultType());
            if (options.isUseCompleteStage()) {
                returnType = ParameterizedTypeName.get(GenClassUtils.CLSNAME_STAGE, resultType);
            } else {
                returnType = ParameterizedTypeName.get(GenClassUtils.CLSNAME_FUTURE, resultType);
            }
        } else {
            returnType = TypeName.VOID;
        }
        methodBuilder.returns(returnType);

        // 是否需要context参数
        if (method.isCtx()) {
            methodBuilder.addParameter(clsName_rpcGenericContext, "rpcCtx");
        }
        // 正常参数
        if (method.getArgType() != null) {
            ClassName argType = classNameOfType(method.getArgType());
            methodBuilder.addParameter(argType, method.getArgName());
        }
    }

    private void buildWithContextMode(PBMethod method, MethodSpec.Builder methodBuilder) {
        // 返回值类型封装到context
        TypeName rpcCtxArg;
        if (method.getResultType() != null) {
            ClassName resultType = classNameOfType(method.getResultType());
            rpcCtxArg = ParameterizedTypeName.get(clsName_rpcContext, resultType);
        } else {
            rpcCtxArg = ParameterizedTypeName.get(clsName_rpcContext, TypeName.OBJECT);
        }
        methodBuilder.returns(TypeName.VOID)
                .addParameter(rpcCtxArg, "rpcCtx");

        // 正常参数
        if (method.getArgType() != null) {
            ClassName argType = classNameOfType(method.getArgType());
            methodBuilder.addParameter(argType, method.getArgName());
        }
    }

}