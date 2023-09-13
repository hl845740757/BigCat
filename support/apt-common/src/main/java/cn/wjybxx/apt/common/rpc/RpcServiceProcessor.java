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

import cn.wjybxx.apt.AptUtils;
import cn.wjybxx.apt.BeanUtils;
import cn.wjybxx.apt.MyAbstractProcessor;
import com.google.auto.service.AutoService;
import com.squareup.javapoet.ClassName;

import javax.annotation.processing.Processor;
import javax.annotation.processing.RoundEnvironment;
import javax.lang.model.element.*;
import javax.lang.model.type.DeclaredType;
import javax.lang.model.type.PrimitiveType;
import javax.lang.model.type.TypeKind;
import javax.lang.model.type.TypeMirror;
import javax.tools.Diagnostic;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.CompletionStage;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date 2023/4/12
 */
@AutoService(Processor.class)
public class RpcServiceProcessor extends MyAbstractProcessor {

    private static final String CNAME_RPC_SERVICE = "cn.wjybxx.common.rpc.RpcService";
    private static final String CNAME_RPC_METHOD = "cn.wjybxx.common.rpc.RpcMethod";
    private static final String PNAME_SERVICE_ID = "serviceId";
    private static final String PNAME_METHOD_ID = "methodId";

    private static final String CNAME_METHOD_SPEC = "cn.wjybxx.common.rpc.RpcMethodSpec";
    private static final String CNAME_METHOD_REGISTRY = "cn.wjybxx.common.rpc.RpcRegistry";
    private static final String CNAME_CONTEXT = "cn.wjybxx.common.rpc.RpcContext";

    private static final int MAX_PARAMETER_COUNT = 5;

    private TypeElement anno_rpcServiceElement;
    private TypeElement anno_rpcMethodElement;

    TypeElement methodSpecElement;
    ClassName methodSpecRawTypeName;
    ClassName methodRegistryTypeName;

    ClassName contextRawTypeName;
    TypeMirror contextTypeMirror;

    TypeMirror boxedVoidTypeMirror;
    TypeMirror objectTypeMirror;
    TypeMirror stringTypeMirror;
    List<TypeMirror> futureTypeMirrors;

    public RpcServiceProcessor() {
    }

    @Override
    public Set<String> getSupportedAnnotationTypes() {
        return Set.of(CNAME_RPC_SERVICE);
    }

    @Override
    protected void ensureInited() {
        if (anno_rpcServiceElement != null) {
            // 已初始化
            return;
        }
        anno_rpcServiceElement = elementUtils.getTypeElement(CNAME_RPC_SERVICE);
        anno_rpcMethodElement = elementUtils.getTypeElement(CNAME_RPC_METHOD);

        methodSpecElement = elementUtils.getTypeElement(CNAME_METHOD_SPEC);
        methodSpecRawTypeName = ClassName.get(methodSpecElement);
        methodRegistryTypeName = ClassName.get(elementUtils.getTypeElement(CNAME_METHOD_REGISTRY));

        TypeElement contextTypeElement = elementUtils.getTypeElement(CNAME_CONTEXT);
        contextRawTypeName = ClassName.get(contextTypeElement);
        contextTypeMirror = contextTypeElement.asType();

        objectTypeMirror = AptUtils.getTypeElementOfClass(elementUtils, Object.class).asType();
        stringTypeMirror = AptUtils.getTypeElementOfClass(elementUtils, String.class).asType();

        futureTypeMirrors = new ArrayList<>(2);
        futureTypeMirrors.add(AptUtils.getTypeElementOfClass(elementUtils, CompletableFuture.class).asType());
        futureTypeMirrors.add(AptUtils.getTypeElementOfClass(elementUtils, CompletionStage.class).asType());
    }

    @Override
    protected boolean doProcess(Set<? extends TypeElement> annotations, RoundEnvironment roundEnv) {
        @SuppressWarnings("unchecked")
        Set<TypeElement> typeElementSet = (Set<TypeElement>) roundEnv.getElementsAnnotatedWith(anno_rpcServiceElement);
        for (TypeElement typeElement : typeElementSet) {
            try {
                List<ExecutableElement> rpcMethodList = checkBase(typeElement);
                genProxyClass(typeElement, rpcMethodList);
            } catch (Throwable e) {
                messager.printMessage(Diagnostic.Kind.ERROR, AptUtils.getStackTrace(e), typeElement);
            }
        }
        return true;
    }

    /** @return rpc方法 - 避免每次查找，开销较大 */
    private List<ExecutableElement> checkBase(TypeElement typeElement) {
        // 新设计下，允许serviceId小于0，表示本地服务(Local Service)
//        final int serviceId = getServiceId(typeElement);
//        if (serviceId <= 0) {
//            messager.printMessage(Diagnostic.Kind.ERROR, " serviceId " + serviceId + " must greater than 0!", typeElement);
//            return List.of();
//        }

        List<ExecutableElement> allMethodList = new ArrayList<>(BeanUtils.getAllMethodsWithInherit(typeElement));
        allMethodList.addAll(findInterfaceMethods(typeElement));

        final List<ExecutableElement> rpcMethodList = new ArrayList<>();
        final Set<Integer> methodIdSet = new HashSet<>();
        for (final ExecutableElement method : allMethodList) {
            final Integer methodId = getMethodId(method);
            if (methodId == null) { // 不是rpc方法
                continue;
            }

            if (method.getModifiers().contains(Modifier.STATIC)) { // 不可以是静态的
                messager.printMessage(Diagnostic.Kind.ERROR, "RpcMethod method can't be static！", method);
                continue;
            }

            if (!method.getModifiers().contains(Modifier.PUBLIC)) { // 必须是public
                messager.printMessage(Diagnostic.Kind.ERROR, "RpcMethod method must be public！", method);
                continue;
            }

            if (methodId < 0 || methodId > 9999) {
                messager.printMessage(Diagnostic.Kind.ERROR, " methodId " + methodId + " must between [0,9999]!", method);
                continue;
            }
            if (!methodIdSet.add(methodId)) { // 同一个类中的方法id不可以重复 - 它保证了本模块中方法id不会重复
                messager.printMessage(Diagnostic.Kind.ERROR, " methodId " + methodId + " is duplicate!", method);
                continue;
            }

            checkParameters(method);
            checkReturnType(method);
            rpcMethodList.add(method);
        }
        return rpcMethodList;
    }

    private List<ExecutableElement> findInterfaceMethods(TypeElement typeElement) {
        return AptUtils.findAllInterfaces(typeUtils, elementUtils, typeElement).stream()
                .map(RpcServiceProcessor::castTypeMirror2TypeElement)
                .flatMap(e -> e.getEnclosedElements().stream())
                .filter(e -> e.getKind() == ElementKind.METHOD)
                .map(e -> (ExecutableElement) e)
                .collect(Collectors.toList());
    }

    private static TypeElement castTypeMirror2TypeElement(TypeMirror typeMirror) {
        DeclaredType declaredType = (DeclaredType) typeMirror;
        Element element = declaredType.asElement();
        return (TypeElement) element;
    }

    private void checkParameters(ExecutableElement method) {
        List<? extends VariableElement> parameters = method.getParameters();
        if (parameters.size() == 0) {
            return;
        }
        // 检测方法参数个数
        boolean firstArgIsContext = isContext(parameters.get(0).asType());
        int maxParameterCount = firstArgIsContext ? MAX_PARAMETER_COUNT + 1 : MAX_PARAMETER_COUNT;
        if (parameters.size() > maxParameterCount) {
            messager.printMessage(Diagnostic.Kind.ERROR, "method has too many parameters!", method);
        }
        // 检查后续是否存在context参数
        for (int idx = firstArgIsContext ? 1 : 0; idx < parameters.size(); idx++) {
            VariableElement variableElement = parameters.get(idx);
            if (isContext(variableElement.asType())) {
                messager.printMessage(Diagnostic.Kind.ERROR, "context must be declared as the first parameter!", method);
                continue;
            }
        }
    }

    private void checkReturnType(ExecutableElement method) {

    }

    private void genProxyClass(TypeElement typeElement, List<ExecutableElement> rpcMethodList) {
        final int serviceId = getServiceId(typeElement);
        // 客户端代理
        genClientProxy(typeElement, serviceId, rpcMethodList);
        // 服务器代理
        genServerProxy(typeElement, serviceId, rpcMethodList);
    }

    Integer getServiceId(TypeElement typeElement) {
        // 基本类型会被包装，Object不能直接转int
        return (Integer) AptUtils.findAnnotation(typeUtils, typeElement, anno_rpcServiceElement.asType())
                .map(annotationMirror -> AptUtils.getAnnotationValueValue(annotationMirror, PNAME_SERVICE_ID))
                .orElseThrow();
    }

    Integer getMethodId(ExecutableElement method) {
        return (Integer) AptUtils.findAnnotation(typeUtils, method, anno_rpcMethodElement.asType())
                .map(annotationMirror -> AptUtils.getAnnotationValueValue(annotationMirror, PNAME_METHOD_ID))
                .orElse(null);
    }

    /**
     * 为客户端生成代理文件
     * XXXProxy
     */
    private void genClientProxy(TypeElement typeElement, int serviceId, List<ExecutableElement> rpcMethods) {
        new RpcProxyGenerator(this, typeElement, serviceId, rpcMethods)
                .execute();
    }

    /**
     * 为服务器生成代理文件
     * XXXExporter
     */
    private void genServerProxy(TypeElement typeElement, int serviceId, List<ExecutableElement> rpcMethods) {
        new RpcExporterGenerator(this, typeElement, serviceId, rpcMethods)
                .execute();
    }

    boolean isContext(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, contextTypeMirror);
    }

    boolean isString(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, stringTypeMirror);
    }

    boolean isFuture(TypeMirror typeMirror) {
        for (TypeMirror futureTypeMirror : futureTypeMirrors) {
            if (AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, futureTypeMirror)) {
                return true;
            }
        }
        return false;
    }

    boolean firstArgIsContext(ExecutableElement method) {
        return method.getParameters().size() > 0 && isContext(method.getParameters().get(0).asType());
    }

}