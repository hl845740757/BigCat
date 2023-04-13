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

package cn.wjybxx.bigcat.common.apt.rpc;

import cn.wjybxx.bigcat.common.apt.AptUtils;
import cn.wjybxx.bigcat.common.apt.BeanUtils;
import cn.wjybxx.bigcat.common.apt.MyAbstractProcessor;
import com.google.auto.service.AutoService;
import com.squareup.javapoet.ClassName;

import javax.annotation.processing.Processor;
import javax.annotation.processing.RoundEnvironment;
import javax.lang.model.element.*;
import javax.lang.model.type.DeclaredType;
import javax.lang.model.type.TypeMirror;
import javax.tools.Diagnostic;
import java.util.*;
import java.util.concurrent.Future;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date 2023/4/12
 */
@AutoService(Processor.class)
public class RpcServiceProcessor extends MyAbstractProcessor {

    private static final String RPC_SERVICE_CANONICAL_NAME = "cn.wjybxx.bigcat.common.rpc.RpcService";
    private static final String RPC_METHOD_CANONICAL_NAME = "cn.wjybxx.bigcat.common.rpc.RpcMethod";

    private static final String SERVICE_ID_PROPERTY_NAME = "serviceId";
    private static final String METHOD_ID_PROPERTY_NAME = "methodId";

    private static final String METHOD_SPEC_CANONICAL_NAME = "cn.wjybxx.bigcat.common.rpc.RpcMethodSpec";
    private static final String DEFAULT_METHOD_SPEC_CANONICAL_NAME = "cn.wjybxx.bigcat.common.rpc.DefaultRpcMethodSpec";
    private static final String METHOD_REGISTRY_CANONICAL_NAME = "cn.wjybxx.bigcat.common.rpc.RpcMethodProxyRegistry";

    private static final String CONTEXT_CANONICAL_NAME = "cn.wjybxx.bigcat.common.rpc.RpcProcessContext";
    private static final String FUTURE_CANONICAL_NAME = "cn.wjybxx.bigcat.common.async.FluentFuture";

    private TypeElement anno_rpcServiceElement;
    private TypeElement anno_rpcMethodElement;

    TypeElement methodSpecElement;
    ClassName defaultMethodSpecRawTypeName;
    ClassName methodRegistryTypeName;

    private TypeMirror futureTypeMirror;
    private TypeMirror jdkFutureTypeMirror;

    TypeMirror objectTypeMirror;
    private TypeMirror mapTypeMirror;
    private TypeMirror linkedHashMapTypeMirror;
    private TypeMirror collectionTypeMirror;
    private TypeMirror arrayListTypeMirror;

    ClassName arrayListTypeName;

    public RpcServiceProcessor() {
    }

    @Override
    public Set<String> getSupportedAnnotationTypes() {
        return Set.of(RPC_SERVICE_CANONICAL_NAME);
    }

    @Override
    protected void ensureInited() {
        if (anno_rpcServiceElement != null) {
            // 已初始化
            return;
        }
        anno_rpcServiceElement = elementUtils.getTypeElement(RPC_SERVICE_CANONICAL_NAME);
        anno_rpcMethodElement = elementUtils.getTypeElement(RPC_METHOD_CANONICAL_NAME);

        methodSpecElement = elementUtils.getTypeElement(METHOD_SPEC_CANONICAL_NAME);
        defaultMethodSpecRawTypeName = ClassName.get(elementUtils.getTypeElement(DEFAULT_METHOD_SPEC_CANONICAL_NAME));
        methodRegistryTypeName = ClassName.get(elementUtils.getTypeElement(METHOD_REGISTRY_CANONICAL_NAME));

        futureTypeMirror = elementUtils.getTypeElement(FUTURE_CANONICAL_NAME).asType();
        jdkFutureTypeMirror = AptUtils.getTypeElementOfClass(elementUtils, Future.class).asType();

        objectTypeMirror = AptUtils.getTypeElementOfClass(elementUtils, Object.class).asType();
        mapTypeMirror = elementUtils.getTypeElement(Map.class.getCanonicalName()).asType();
        linkedHashMapTypeMirror = elementUtils.getTypeElement(LinkedHashMap.class.getCanonicalName()).asType();
        collectionTypeMirror = elementUtils.getTypeElement(Collection.class.getCanonicalName()).asType();
        arrayListTypeMirror = elementUtils.getTypeElement(ArrayList.class.getCanonicalName()).asType();

        arrayListTypeName = ClassName.get(ArrayList.class);
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
        final Short serviceId = getServiceId(typeElement);
        if (serviceId <= 0) {
            messager.printMessage(Diagnostic.Kind.ERROR, " serviceId " + serviceId + " must greater than 0!", typeElement);
            return List.of();
        }

        List<ExecutableElement> allMethodList = new ArrayList<>(BeanUtils.getAllMethodsWithInherit(typeElement));
        allMethodList.addAll(findInterfaceMethods(typeElement));

        final List<ExecutableElement> rpcMethodList = new ArrayList<>();
        final Set<Short> methodIdSet = new HashSet<>();
        for (final ExecutableElement method : allMethodList) {
            final Short methodId = getMethodId(method);
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
        List<ExecutableElement> interfaceMethodList = AptUtils.findAllInterfaces(typeUtils, elementUtils, typeElement).stream()
                .map(RpcServiceProcessor::castTypeMirror2TypeElement)
                .flatMap(e -> e.getEnclosedElements().stream())
                .filter(e -> e.getKind() == ElementKind.METHOD)
                .map(e -> (ExecutableElement) e)
                .collect(Collectors.toList());
        return interfaceMethodList;
    }

    private static TypeElement castTypeMirror2TypeElement(TypeMirror typeMirror) {
        DeclaredType declaredType = (DeclaredType) typeMirror;
        Element element = declaredType.asElement();
        return (TypeElement) element;
    }

    private void checkParameters(ExecutableElement method) {
        for (VariableElement variableElement : method.getParameters()) {
            if (isMap(variableElement.asType())) {
                checkMap(variableElement, variableElement.asType());
                continue;
            }
            if (isCollection(variableElement.asType())) {
                checkCollection(variableElement, variableElement.asType());
                continue;
            }
        }
    }

    private void checkReturnType(ExecutableElement method) {
        TypeMirror returnType = method.getReturnType();
        if (isMap(returnType)) {
            checkMap(method, returnType);
        } else if (isCollection(returnType)) {
            checkCollection(method, returnType);
        }
    }

    private void checkMap(Element element, TypeMirror typeMirror) {
        if (!isAssignableFromLinkedHashMap(typeMirror)) {
            messager.printMessage(Diagnostic.Kind.ERROR,
                    "Unsupported map type, Map parameter/return only support LinkedHashMap's parent, " +
                            "refer to the annotation '@FieldImpl' for more help",
                    element);
        }
    }

    private void checkCollection(Element element, TypeMirror typeMirror) {
        if (!isAssignableFormArrayList(typeMirror)) {
            messager.printMessage(Diagnostic.Kind.ERROR,
                    "Unsupported collection type, Collection parameter/return only support ArrayList's parent," +
                            " refer to the annotation '@FieldImpl' for more help",
                    element);
        }
    }

    private void genProxyClass(TypeElement typeElement, List<ExecutableElement> rpcMethodList) {
        final Short serviceId = getServiceId(typeElement);
        // 客户端代理
        genClientProxy(typeElement, serviceId, rpcMethodList);
        // 服务器代理
        genServerProxy(typeElement, serviceId, rpcMethodList);
    }

    private Short getServiceId(TypeElement typeElement) {
        // 基本类型会被包装，Object不能直接转short
        return (Short) AptUtils.findAnnotation(typeUtils, typeElement, anno_rpcServiceElement.asType())
                .map(annotationMirror -> AptUtils.getAnnotationValueValue(annotationMirror, SERVICE_ID_PROPERTY_NAME))
                .orElseThrow();
    }

    Short getMethodId(ExecutableElement method) {
        return (Short) AptUtils.findAnnotation(typeUtils, method, anno_rpcMethodElement.asType())
                .map(annotationMirror -> AptUtils.getAnnotationValueValue(annotationMirror, METHOD_ID_PROPERTY_NAME))
                .orElse(null);
    }

    /**
     * 为客户端生成代理文件
     * XXXProxy
     */
    private void genClientProxy(TypeElement typeElement, Short serviceId, List<ExecutableElement> rpcMethods) {
        new RpcProxyGenerator(this, typeElement, serviceId, rpcMethods)
                .execute();
    }

    /**
     * 为服务器生成代理文件
     * XXXExporter
     */
    private void genServerProxy(TypeElement typeElement, Short serviceId, List<ExecutableElement> rpcMethods) {
        new RpcExporterGenerator(this, typeElement, serviceId, rpcMethods)
                .execute();
    }

    private boolean isMap(TypeMirror typeMirror) {
        return !typeMirror.getKind().isPrimitive() &&
                AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, mapTypeMirror);
    }

    private boolean isAssignableFromLinkedHashMap(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, linkedHashMapTypeMirror, typeMirror);
    }

    private boolean isCollection(TypeMirror typeMirror) {
        return !typeMirror.getKind().isPrimitive() &&
                AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, collectionTypeMirror);
    }

    private boolean isAssignableFormArrayList(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, arrayListTypeMirror, typeMirror);
    }

    boolean isFuture(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, futureTypeMirror)
                || AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, jdkFutureTypeMirror);
    }

}