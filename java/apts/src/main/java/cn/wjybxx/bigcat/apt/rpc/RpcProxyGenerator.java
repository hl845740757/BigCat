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
import com.squareup.javapoet.*;

import javax.lang.model.element.AnnotationValue;
import javax.lang.model.element.ExecutableElement;
import javax.lang.model.element.Modifier;
import javax.lang.model.element.TypeElement;
import javax.lang.model.type.DeclaredType;
import javax.lang.model.type.TypeMirror;
import java.util.List;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/4/12
 */
class RpcProxyGenerator extends AbstractGenerator<RpcServiceProcessor> {

    private final int serviceId;
    private final List<ExecutableElement> rpcMethods;
    private final ClassName serviceTypeName;

    RpcProxyGenerator(RpcServiceProcessor processor, TypeElement typeElement, int serviceId, List<ExecutableElement> rpcMethods) {
        super(processor, typeElement);
        this.serviceId = serviceId;
        this.rpcMethods = rpcMethods;
        this.serviceTypeName = ClassName.get(typeElement);
    }

    @Override
    public void execute() {
        final TypeSpec.Builder typeBuilder = TypeSpec.classBuilder(getClientProxyClassName(typeElement))
                .addModifiers(Modifier.PUBLIC, Modifier.FINAL)
                .addAnnotation(processorInfoAnnotation)
                .addAnnotation(AptUtils.newSourceFileRefAnnotation(ClassName.get(typeElement)));

        // 生成代理方法
        for (final ExecutableElement method : rpcMethods) {
            Map<String, AnnotationValue> annoValueMap = processor.getMethodAnnoValueMap(method);
            MethodSpec proxyMethodSpec = genClientMethodProxy(method, annoValueMap, false);
            typeBuilder.addMethod(proxyMethodSpec);
            // 生成Builder的重载
            if (proxyMethodSpec.parameters.size() > 0 && processor.isBuilderPattern(method, annoValueMap)) {
                MethodSpec methodSpec2 = genClientMethodProxy(method, annoValueMap, true);
                typeBuilder.addMethod(methodSpec2);
            }
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
     * 		public static MethodSpec<Response> method1(Request request) {
     * 			return new RpcMethodSpec<>(1, 2, request, false);
     *        }
     * }
     * </pre>
     */
    private MethodSpec genClientMethodProxy(ExecutableElement method, Map<String, AnnotationValue> annoValueMap, boolean isBuildPattern) {
        final MethodSpec.Builder builder = MethodSpec.methodBuilder(method.getSimpleName().toString())
                .addModifiers(Modifier.PUBLIC, Modifier.STATIC);
        // 拷贝泛型参数
        AptUtils.copyTypeVariables(builder, method);

        // 添加返回类型 - 带泛型
        final TypeMirror rpcReturnType = processor.rpcReturnType(method);
        final DeclaredType proxyReturnType = typeUtils.getDeclaredType(processor.type_MethodSpec, rpcReturnType);
        final TypeName proxyReturnTypeName = TypeName.get(proxyReturnType);
        builder.returns(proxyReturnTypeName);

        // 拷贝方法参数
        AptUtils.copyParameters(builder, method);
//        builder.varargs(method.isVarArgs());

        // 去除context参数
        final List<ParameterSpec> parameters = builder.parameters;
        FirstArgType firstArgType = processor.getFirstArgType(method);
        if (firstArgType == FirstArgType.CONTEXT) {
            parameters.remove(0);
        }
        // 替换Builder参数
        if (parameters.size() > 0 && isBuildPattern) {
            ParameterSpec messageParameter = parameters.get(0);
            ClassName messageClassName = (ClassName) messageParameter.type;
            ClassName builderClassName = messageClassName.nestedClass("Builder");
            ParameterSpec builderParameter = ParameterSpec.builder(builderClassName, messageParameter.name).build();
            // 替换方法参数
            builder.parameters.set(0, builderParameter);
        }
        if (parameters.size() == 0) {
            // 无参(serviceId, methodId, null, true)
            builder.addStatement("return new $T<>($L, $L, null, true)",
                    processor.typeName_MethodSpecRaw,
                    serviceId, processor.getMethodId(method, annoValueMap));
        } else {
            // 1个参数(serviceId, methodId, parameter, sharable) -- builder强制不可共享
            builder.addStatement("return new $T<>($L, $L, $L, $L)",
                    processor.typeName_MethodSpecRaw,
                    serviceId, processor.getMethodId(method, annoValueMap),
                    parameters.get(0).name,
                    !isBuildPattern && processor.isArgSharable(method, annoValueMap));
        }
        // 添加一个引用，方便定位 -- 不完全准确，但胜过没有
        builder.addJavadoc("{@link $T#$L}", serviceTypeName, method.getSimpleName().toString());
        return builder.build();
    }

}