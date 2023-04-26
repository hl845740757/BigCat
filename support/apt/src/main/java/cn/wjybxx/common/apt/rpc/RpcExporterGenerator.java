/*
 * Copyright 2023 wjybxx
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

package cn.wjybxx.common.apt.rpc;

import cn.wjybxx.common.apt.AbstractGenerator;
import cn.wjybxx.common.apt.AptUtils;
import cn.wjybxx.common.apt.BeanUtils;
import com.squareup.javapoet.ClassName;
import com.squareup.javapoet.MethodSpec;
import com.squareup.javapoet.TypeName;
import com.squareup.javapoet.TypeSpec;

import javax.lang.model.element.ExecutableElement;
import javax.lang.model.element.Modifier;
import javax.lang.model.element.TypeElement;
import javax.lang.model.element.VariableElement;
import javax.lang.model.type.TypeKind;
import javax.lang.model.type.TypeMirror;
import java.util.ArrayList;
import java.util.EnumMap;
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
    private static final String varName_methodSpec = "methodSpec";

    private static final String GET_OBJECT_METHOD_NAME = "getObject";
    private static final Map<TypeKind, String> primitiveGetParamMethodName = new EnumMap<>(TypeKind.class);

    private final short serviceId;
    private final List<ExecutableElement> rpcMethods;

    static {
        for (TypeKind typeKind : TypeKind.values()) {
            if (!typeKind.isPrimitive()) {
                continue;
            }
            final String name = BeanUtils.firstCharToUpperCase(typeKind.name().toLowerCase());
            primitiveGetParamMethodName.put(typeKind, "get" + name);
        }
    }

    RpcExporterGenerator(RpcServiceProcessor processor, TypeElement typeElement, short serviceId, List<ExecutableElement> rpcMethods) {
        super(processor, typeElement);
        this.serviceId = serviceId;
        this.rpcMethods = rpcMethods;
    }

    @Override
    public void execute() {
        final TypeSpec.Builder typeBuilder = TypeSpec.classBuilder(getServerProxyClassName(typeElement))
                .addModifiers(Modifier.PUBLIC, Modifier.FINAL)
                .addAnnotation(AptUtils.SUPPRESS_UNCHECKED_ANNOTATION)
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
     * public static void export(RpcFunctionRegistry registry, T instance) {
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
     * 1. 有返回值的，直接返回方法执行结果（任意值）
     * <pre>
     * {@code
     * 		private static void exportMethod1(RpcFunctionRegistry registry, T instance) {
     * 		    registry.register(10001, (context, methodSpec) -> {
     * 		        return instance.method10001(methodSpec.getInt(0), methodSpec.getLong(1));
     *         }
     *     }
     * }
     * </pre>
     * 2. 无返回值的，代理执行完之后直接返回null
     * <pre>
     * {@code
     * 		private static void exportMethod2(RpcFunctionRegistry registry, T instance) {
     * 		    registry.register(10002, (context, methodSpec) -> {
     * 		        instance.method10002(methodSpec.getInt(0), methodSpec.getLong(1));
     * 		        return null;
     *          }
     *     }
     * }
     * </pre>
     */
    private MethodSpec genServerMethodProxy(TypeElement typeElement, short serviceId, ExecutableElement method) {
        final Short methodId = processor.getMethodId(method);
        final MethodSpec.Builder builder = MethodSpec.methodBuilder(getServerProxyMethodName(methodId, method))
                .addModifiers(Modifier.PRIVATE, Modifier.STATIC)
                .returns(TypeName.VOID)
                .addParameter(processor.methodRegistryTypeName, varName_registry)
                .addParameter(TypeName.get(typeElement.asType()), varName_instance);
        // 拷贝泛型参数
        AptUtils.copyTypeVariables(builder, method);

        // registry中的方法
        builder.addCode("$L.register((short)$L, (short)$L, ($L, $L) -> {\n",
                varName_registry, serviceId, methodId, varName_context, varName_methodSpec);

        final InvokeStatement invokeStatement = genInvokeStatement(method);
        if (method.getReturnType().getKind() != TypeKind.VOID) {
            builder.addStatement("    return " + invokeStatement.format, invokeStatement.params.toArray());
        } else {
            builder.addStatement("    " + invokeStatement.format, invokeStatement.params.toArray());
            builder.addStatement("    return null");
        }

        builder.addStatement("})");
        return builder.build();
    }

    /**
     * 获取代理方法的名字
     */
    private static String getServerProxyMethodName(short methodId, ExecutableElement method) {
        // 加上methodId防止重复
        return "_export" + BeanUtils.firstCharToUpperCase(method.getSimpleName().toString()) + "_" + methodId;
    }

    /**
     * 生成方法调用代码，没有分号和换行符。
     * {@code instance.rpcMethod(a, b, c)}
     */
    private InvokeStatement genInvokeStatement(ExecutableElement method) {
        final StringBuilder format = new StringBuilder();
        final List<Object> params = new ArrayList<>(method.getParameters().size());

        // 调用方法
        format.append("$L.$L(");
        params.add(varName_instance);
        params.add(method.getSimpleName().toString());

        // 填充参数
        final List<? extends VariableElement> parameters = method.getParameters();
        for (int index = 0; index < parameters.size(); index++) {
            VariableElement variableElement = parameters.get(index);
            if (index > 0) {
                format.append(", ");
            }

            final TypeMirror paramTypeMirror = variableElement.asType();
            if (paramTypeMirror.getKind().isPrimitive()) {
                final String getParamMethodName = primitiveGetParamMethodName.get(paramTypeMirror.getKind());
                // methodSpec.getInt(0)
                format.append("$L.$L($L)");
                params.add(varName_methodSpec);
                params.add(getParamMethodName);
                params.add(index);
            } else {
                final TypeName parameterTypeName = TypeName.get(paramTypeMirror);
                final String getParamMethodName = GET_OBJECT_METHOD_NAME;
                // (String) methodSpec.getObject(0)
                format.append("($T) $L.$L($L)");
                params.add(parameterTypeName);
                params.add(varName_methodSpec);
                params.add(getParamMethodName);
                params.add(index);
            }
        }
        format.append(")");
        return new InvokeStatement(format.toString(), params);
    }

    private static class InvokeStatement {

        private final String format;
        private final List<Object> params;

        private InvokeStatement(String format, List<Object> params) {
            this.format = format;
            this.params = params;
        }
    }
}