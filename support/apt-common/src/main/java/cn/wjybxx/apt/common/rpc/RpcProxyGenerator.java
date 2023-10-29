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

package cn.wjybxx.apt.common.rpc;

import cn.wjybxx.apt.AbstractGenerator;
import cn.wjybxx.apt.AptUtils;
import com.squareup.javapoet.*;

import javax.lang.model.element.*;
import javax.lang.model.type.DeclaredType;
import javax.lang.model.type.PrimitiveType;
import javax.lang.model.type.TypeKind;
import javax.lang.model.type.TypeMirror;
import javax.tools.Diagnostic;
import java.util.List;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/4/12
 */
class RpcProxyGenerator extends AbstractGenerator<RpcServiceProcessor> {

    private final int serviceId;
    private final List<ExecutableElement> rpcMethods;
    private final ClassName typeClassName;

    RpcProxyGenerator(RpcServiceProcessor processor, TypeElement typeElement, int serviceId, List<ExecutableElement> rpcMethods) {
        super(processor, typeElement);
        this.serviceId = serviceId;
        this.rpcMethods = rpcMethods;
        this.typeClassName = ClassName.get(typeElement);
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
     * 			List<Object> _parameters = new ArrayList<>(2);
     * 			_parameters.add(id);
     * 			_parameters.add(param);
     * 			return new DefaultRpcMethodSpec<>(1, 2, _parameters, 0, 0);
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

        // 去除context和request参数
        final List<ParameterSpec> parameters = builder.parameters;
        final FirstArgType firstArgType = processor.firstArgType(method);
        if (firstArgType.noCounting()) {
            parameters.remove(0);
        }

        Map<String, AnnotationValue> annoValueMap = processor.getMethodAnnoValueMap(method);
        if (parameters.size() == 0) {
            // 无参时，使用 List.of();
            builder.addStatement("return new $T<>($L, $L, $T.of(), true)",
                    processor.methodSpecRawTypeName,
                    serviceId, processor.getMethodId(method, annoValueMap),
                    AptUtils.CLSNAME_LIST);
        } else {
            // ArrayList<Object> _parameters = new ArrayList<>(2);
            ClassName arrayListTypeName = AptUtils.CLSNAME_ARRAY_LIST;
            builder.addStatement("$T<Object> _parameters = new $T<>($L)", arrayListTypeName, arrayListTypeName, parameters.size());
            for (ParameterSpec parameterSpec : parameters) {
                builder.addStatement("_parameters.add($L)", parameterSpec.name);
            }
            builder.addStatement("return new $T<>($L, $L, _parameters, $L)",
                    processor.methodSpecRawTypeName,
                    serviceId, processor.getMethodId(method, annoValueMap),
                    processor.isArgSharable(method, annoValueMap));
        }

        // 添加一个引用，方便定位 -- 不完全准确，但胜过没有
        builder.addJavadoc("{@link $T#$L}", typeClassName, method.getSimpleName().toString());
        return builder.build();
    }

    /**
     * 获取返回类型，如果是基本类型，会进行装箱
     */
    private TypeMirror getBoxedReturnType(ExecutableElement method) {
        // context覆盖返回值类型
        List<? extends VariableElement> parameters = method.getParameters();
        if (parameters.size() > 0 && processor.isContext(parameters.get(0).asType())) {
            return findFutureTypeArgument(parameters.get(0).asType(), method);
        }
        // 基础类型和void
        TypeMirror returnType = method.getReturnType();
        if (returnType.getKind().isPrimitive()) {
            return typeUtils.boxedClass((PrimitiveType) returnType).asType();
        }
        if (returnType.getKind() == TypeKind.VOID) {
            return processor.boxedVoidTypeMirror;
        }
        // future类型
        if (processor.isFuture(returnType)) {
            return findFutureTypeArgument(returnType, method);
        } else {
            return returnType;
        }
    }

    private TypeMirror findFutureTypeArgument(TypeMirror typeMirror, ExecutableElement method) {
        TypeMirror firstTypeParameter = AptUtils.findFirstTypeParameter(typeMirror);
        if (firstTypeParameter == null) {
            messager.printMessage(Diagnostic.Kind.WARNING, "Future missing type parameter!", method);
            return processor.objectTypeMirror;
        } else {
            return firstTypeParameter;
        }
    }

}