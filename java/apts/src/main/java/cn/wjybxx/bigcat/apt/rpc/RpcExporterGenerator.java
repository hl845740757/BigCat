/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.bigcat.apt.rpc;

import cn.wjybxx.apt.AbstractGenerator;
import cn.wjybxx.apt.AptUtils;
import cn.wjybxx.apt.BeanUtils;
import com.squareup.javapoet.*;

import javax.lang.model.element.*;
import javax.lang.model.type.TypeKind;
import javax.lang.model.type.TypeMirror;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/4/12
 */
public class RpcExporterGenerator extends AbstractGenerator<RpcServiceProcessor> {

    private static final String varName_registry = "registry";
    private static final String varName_instance = "instance";
    private static final String varName_context = "context";
    private static final String varName_parameter = "parameter";

    private final int serviceId;
    private final List<ExecutableElement> rpcMethods;

    RpcExporterGenerator(RpcServiceProcessor processor, TypeElement typeElement, int serviceId, List<ExecutableElement> rpcMethods) {
        super(processor, typeElement);
        this.serviceId = serviceId;
        this.rpcMethods = rpcMethods;
    }

    @Override
    public void execute() {
        final TypeSpec.Builder typeBuilder = TypeSpec.classBuilder(getServerProxyClassName(typeElement))
                .addModifiers(Modifier.PUBLIC, Modifier.FINAL)
                .addAnnotation(AptUtils.SUPPRESS_UNCHECKED_RAWTYPES)
                .addAnnotation(processorInfoAnnotation)
                .addAnnotation(AptUtils.newSourceFileRefAnnotation(ClassName.get(typeElement)));

        final List<MethodSpec> serverMethodProxyList = new ArrayList<>(rpcMethods.size());
        // 生成代理方法
        for (final ExecutableElement method : rpcMethods) {
            serverMethodProxyList.add(genServerMethodProxy(typeElement, serviceId, method));
        }
        typeBuilder.addMethods(serverMethodProxyList);

        // 生成注册方法
        typeBuilder.addMethod(genRegisterMethod(typeElement, serverMethodProxyList));

        // 写入文件
        AptUtils.writeToFile(typeElement, typeBuilder, elementUtils, messager, filer);
    }

    private String getServerProxyClassName(TypeElement typeElement) {
        return typeElement.getSimpleName().toString() + "Exporter";
    }

    /**
     * 生成注册方法
     * <pre>{@code
     * public static void export(RpcProxyRegistry registry, T instance) {
     *     exportMethod1(registry, instance);
     *     exportMethod2(registry, instance);
     * }
     * </pre>
     *
     * @param serverProxyMethodList 被代理的服务器方法
     */
    private MethodSpec genRegisterMethod(TypeElement typeElement, List<MethodSpec> serverProxyMethodList) {
        MethodSpec.Builder builder = MethodSpec.methodBuilder("export")
                .addModifiers(Modifier.PUBLIC, Modifier.STATIC)
                .returns(TypeName.VOID)
                .addParameter(processor.methodRegistryTypeName, varName_registry)
                .addParameter(TypeName.get(typeElement.asType()), varName_instance);

        // 添加调用
        for (MethodSpec method : serverProxyMethodList) {
            builder.addStatement("$L($L, $L)", method.name, varName_registry, varName_instance);
        }

        return builder.build();
    }

    /**
     * 为某个具体方法生成注册方法，方法分为两类
     * 1. 有返回值的，直接返回方法执行结果; 如果方法签名中包含context，需要传入；
     * <pre>
     * {@code
     * 		private static void exportMethod1(RpcProxyRegistry registry, T instance) {
     * 		    registry.<R>register(10001, (context, parameter) -> {
     * 		        R r = instance.method10001(context, (T) parameter);
     * 		        if (context.isManualReturn()) return;
     * 		        context.sendResult(r);
     *         }
     *     }
     * }
     * {@code
     * 		private static void exportMethod1(RpcProxyRegistry registry, T instance) {
     * 		    registry.<R>register(10001, (context, parameter) -> {
     * 		        IFuture<R> r = instance.method10001(context, (T) parameter);
     * 		        if (context.isManualReturn()) return;
     * 		        context.sendAsyncResult(r);
     *         }
     *     }
     * }
     * </pre>
     * 2. 无返回值的，代理执行完之后直接返回null；如果方法签名中包含context，需要传入
     * <pre>
     * {@code
     *      private static void exportMethod2(RpcProxyRegistry registry, T instance) {
     * 		    registry.<Object>register(10002, (context, parameter) -> {
     * 		        instance.method10001(context, (T) parameter);
     *          }
     *     }
     * }
     * </pre>
     */
    private MethodSpec genServerMethodProxy(TypeElement typeElement, int serviceId, ExecutableElement method) {
        final Map<String, AnnotationValue> annoValueMap = processor.getMethodAnnoValueMap(method);
        final int methodId = processor.getMethodId(method, annoValueMap);
        final MethodSpec.Builder builder = MethodSpec.methodBuilder(getServerProxyMethodName(methodId, method))
                .addModifiers(Modifier.PRIVATE, Modifier.STATIC)
                .returns(TypeName.VOID)
                .addParameter(processor.methodRegistryTypeName, varName_registry)
                .addParameter(TypeName.get(typeElement.asType()), varName_instance);
        // 拷贝泛型参数
        AptUtils.copyTypeVariables(builder, method);
        // 注册方法代理
        builder.addCode(genMethodProxy(serviceId, method, methodId, annoValueMap).build());
        // 注册切面数据
        String customData = processor.getCustomData(method, annoValueMap);
        if (customData != null) {
            builder.addStatement("$L.setProxyData($L, $L, $S)", varName_registry, serviceId, methodId, customData);
        }
        return builder.build();
    }

    /** 生成方法代理 */
    private CodeBlock.Builder genMethodProxy(int serviceId, ExecutableElement method, int methodId,
                                             Map<String, AnnotationValue> annoValueMap) {
        CodeBlock.Builder codeBuilder = CodeBlock.builder();
        // registry -- 传入泛型参数，可以避免不必要的类型转换
        TypeMirror rpcReturnType = processor.rpcReturnType(method, true);
        codeBuilder.beginControlFlow("$L.<$T>register($L, $L, (context, $L) ->",
                varName_registry, rpcReturnType, serviceId, methodId, varName_parameter);
        // 可变性设置
        if (processor.isResultSharable(method, annoValueMap)) {
            codeBuilder.addStatement("context.setSharable(true)");
        }
        if (processor.isManualReturn(method, annoValueMap)) {
            codeBuilder.addStatement("context.setManualReturn(true)");
        }
        // 执行方法调用 -- 这里测试方法的直接返回值
        if (method.getReturnType().getKind() == TypeKind.VOID) {
            StringBuilder format = new StringBuilder(32);
            List<Object> params = new ArrayList<>(4);
            genInvokeStatement(method, format, params);
            codeBuilder.addStatement(format.toString(), params.toArray()); // 需要ToArray
        } else {
            StringBuilder format = new StringBuilder(32);
            List<Object> params = new ArrayList<>(4);
            {
                format.append("$T tempR = ");
                params.add(TypeName.get(method.getReturnType()));
            }
            genInvokeStatement(method, format, params);
            codeBuilder.addStatement(format.toString(), params.toArray()); // 需要ToArray

            codeBuilder.addStatement("if (context.isManualReturn()) return");
            if (processor.isFuture(method.getReturnType())) {
                codeBuilder.addStatement("context.sendAsyncResult(tempR)");
            } else {
                codeBuilder.addStatement("context.sendResult(tempR)");
            }
        }
        codeBuilder.unindent(); // endControlFlow会拼入空格...
        codeBuilder.addStatement("})");
        return codeBuilder;
    }

    /**
     * 获取代理方法的名字
     */
    private static String getServerProxyMethodName(int methodId, ExecutableElement method) {
        // 加上methodId防止重复
        return "_export" + BeanUtils.firstCharToUpperCase(method.getSimpleName().toString()) + "_" + methodId;
    }

    /**
     * 生成方法调用代码，没有分号和换行符。
     * {@code instance.rpcMethod(a, b, c)}
     */
    private void genInvokeStatement(ExecutableElement method, StringBuilder format, List<Object> params) {
        // 调用方法
        format.append("$L.$L(");
        params.add(varName_instance);
        params.add(method.getSimpleName().toString());

        // 去除context -- Context的类型转换已在上面统一处理
        List<? extends VariableElement> parameters = method.getParameters();
        if (parameters.size() > 0 && processor.isContext(parameters.get(0).asType())) {
            format.append("context");
            if (parameters.size() > 1) {
                format.append(", ");
            }
            parameters = method.getParameters().subList(1, parameters.size());
        }
        // 方法参数已限定为最多1个，Object向下转换
        if (parameters.size() > 0) {
            VariableElement variableElement = parameters.get(0);
            final TypeName parameterTypeName = TypeName.get(variableElement.asType());

            format.append("($T) $L");
            params.add(parameterTypeName);
            params.add(varName_parameter);
        }
        format.append(")");
    }
}