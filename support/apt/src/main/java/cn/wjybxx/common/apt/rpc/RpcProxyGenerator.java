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
import com.squareup.javapoet.*;

import javax.lang.model.element.ExecutableElement;
import javax.lang.model.element.Modifier;
import javax.lang.model.element.TypeElement;
import javax.lang.model.type.DeclaredType;
import javax.lang.model.type.PrimitiveType;
import javax.lang.model.type.TypeKind;
import javax.lang.model.type.TypeMirror;
import javax.tools.Diagnostic;
import java.util.Collections;
import java.util.List;

/**
 * @author wjybxx
 * date 2023/4/12
 */
class RpcProxyGenerator extends AbstractGenerator<RpcServiceProcessor> {

    private final short serviceId;
    private final List<ExecutableElement> rpcMethods;

    RpcProxyGenerator(RpcServiceProcessor processor, TypeElement typeElement, short serviceId, List<ExecutableElement> rpcMethods) {
        super(processor, typeElement);
        this.serviceId = serviceId;
        this.rpcMethods = rpcMethods;
    }

    @Override
    public void execute() {
        final TypeSpec.Builder typeBuilder = TypeSpec.classBuilder(getClientProxyClassName(typeElement))
                .addModifiers(Modifier.PUBLIC, Modifier.FINAL)
                .addAnnotation(processorInfoAnnotation)
                .addAnnotation(AptUtils.newSourceFileRefAnnotation(ClassName.get(typeElement)));

        // 生成代理方法
        for (final ExecutableElement method : rpcMethods) {
            typeBuilder.addMethod(genClientMethodProxy(method));
        }

        // 写入文件
        AptUtils.writeToFile(typeElement, typeBuilder, elementUtils, messager, filer);
    }

    private static String getClientProxyClassName(TypeElement typeElement) {
        return typeElement.getSimpleName().toString() + "Proxy";
    }

    /**
     * 为客户端生成代理方法
     * <pre>{@code
     * 		public static MethodSpec<String> method1(int id, String param) {
     * 			List<Object> methodParams = new ArrayList<>(2);
     * 			methodParams.add(id);
     * 			methodParams.add(param);
     * 			return new DefaultRpcMethodSpec<>(1, 2, methodParams, 0, 0);
     *        }
     * }
     * </pre>
     */
    private MethodSpec genClientMethodProxy(ExecutableElement method) {
        final MethodSpec.Builder builder = MethodSpec.methodBuilder(method.getSimpleName().toString())
                .addModifiers(Modifier.PUBLIC, Modifier.STATIC);
        // 拷贝泛型参数
        AptUtils.copyTypeVariables(builder, method);

        // 添加返回类型 - 带泛型
        final TypeMirror originReturnType = getBoxedReturnType(method);
        final DeclaredType realReturnType = typeUtils.getDeclaredType(processor.methodSpecElement, originReturnType);
        builder.returns(TypeName.get(realReturnType));

        // 拷贝方法参数
        AptUtils.copyParameters(builder, method);
        builder.varargs(method.isVarArgs());

        final List<ParameterSpec> parameters = builder.parameters;
        if (parameters.size() == 0) {
            // 无参时，使用 Collections.emptyList();
            builder.addStatement("return new $T<>((short)$L, (short)$L, $T.emptyList())",
                    processor.defaultMethodSpecRawTypeName,
                    serviceId, processor.getMethodId(method),
                    Collections.class);
        } else {
            ClassName arrayListTypeName = processor.arrayListTypeName;
            // ArrayList<Object> methodParams = new ArrayList<>(2);
            builder.addStatement("$T<Object> methodParams = new $T<>($L)", arrayListTypeName, arrayListTypeName, parameters.size());
            for (ParameterSpec parameterSpec : parameters) {
                builder.addStatement("methodParams.add($L)", parameterSpec.name);
            }
            builder.addStatement("return new $T<>((short)$L, (short)$L, methodParams)",
                    processor.defaultMethodSpecRawTypeName,
                    serviceId, processor.getMethodId(method));
        }
        return builder.build();
    }

    /** 获取返回类型，如果丢失基本类型，会进行装箱 */
    private TypeMirror getBoxedReturnType(ExecutableElement method) {
        TypeMirror returnType = method.getReturnType();
        if (returnType.getKind().isPrimitive()) {
            return typeUtils.boxedClass((PrimitiveType) returnType).asType();
        }
        if (returnType.getKind() == TypeKind.WILDCARD
                || returnType.getKind() == TypeKind.VOID) {
            return processor.objectTypeMirror; // 通配符和void转为Object
        }

        if (processor.isFuture(returnType)) { // future类型，捕获泛型参数
            return findFutureTypeArgument(method);
        } else {
            return returnType;
        }
    }

    private TypeMirror findFutureTypeArgument(ExecutableElement method) {
        TypeMirror firstTypeParameter = AptUtils.findFirstTypeParameter(method.getReturnType());
        if (firstTypeParameter == null) {
            messager.printMessage(Diagnostic.Kind.WARNING, "Future missing type parameter!", method);
            return processor.objectTypeMirror;
        } else {
            return firstTypeParameter;
        }
    }

}