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
import java.util.*;
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
    private static final String PNAME_SERVICE_ID = "serviceId";
    private static final String PNAME_GEN_EXPORTER = "genExporter";
    private static final String PNAME_GEN_PROXY = "genProxy";

    private static final String CNAME_RPC_METHOD = "cn.wjybxx.common.rpc.RpcMethod";
    private static final String PNAME_METHOD_ID = "methodId";
    private static final String PNAME_ARG_SHARABLE = "argSharable";
    private static final String PNAME_RESULT_SHARABLE = "resultSharable";
    private static final String PNAME_CUSTOM_DATA = "customData";

    private static final String CNAME_METHOD_SPEC = "cn.wjybxx.common.rpc.RpcMethodSpec";
    private static final String CNAME_METHOD_REGISTRY = "cn.wjybxx.common.rpc.RpcRegistry";
    private static final String CNAME_CONTEXT = "cn.wjybxx.common.rpc.RpcContext";
    private static final String CNAME_GENERIC_CONTEXT = "cn.wjybxx.common.rpc.RpcGenericContext";

    private static final String CNAME_PROTOBUF_MESSAGE = "com.google.protobuf.Message";
    private static final int MAX_PARAMETER_COUNT = 5;

    private TypeElement anno_rpcServiceElement;
    private TypeElement anno_rpcMethodElement;

    TypeElement methodSpecElement;
    ClassName methodSpecRawTypeName;
    ClassName methodRegistryTypeName;

    ClassName contextRawTypeName;
    TypeMirror contextTypeMirror;
    TypeMirror genericContextTypeMirror;

    TypeMirror boxedVoidTypeMirror;
    TypeMirror objectTypeMirror;
    TypeMirror stringTypeMirror;
    List<TypeMirror> futureTypeMirrors = new ArrayList<>(2);

    /** 不可变类型，不包含基础类型 */
    Set<TypeMirror> immutableTypeMirrors = new HashSet<>(36);

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
        genericContextTypeMirror = elementUtils.getTypeElement(CNAME_GENERIC_CONTEXT).asType();

        boxedVoidTypeMirror = AptUtils.getTypeElementOfClass(elementUtils, Void.class).asType();
        objectTypeMirror = AptUtils.getTypeElementOfClass(elementUtils, Object.class).asType();
        stringTypeMirror = AptUtils.getTypeElementOfClass(elementUtils, String.class).asType();

        futureTypeMirrors.add(AptUtils.getTypeElementOfClass(elementUtils, CompletableFuture.class).asType());
        futureTypeMirrors.add(AptUtils.getTypeElementOfClass(elementUtils, CompletionStage.class).asType());

        for (TypeKind typeKind : TypeKind.values()) {
            if (!typeKind.isPrimitive()) continue;
            PrimitiveType primitiveType = typeUtils.getPrimitiveType(typeKind);
            TypeElement typeElement = typeUtils.boxedClass(primitiveType);
            immutableTypeMirrors.add(typeElement.asType());
        }
        immutableTypeMirrors.add(stringTypeMirror);
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
        List<ExecutableElement> allMethodList = new ArrayList<>(BeanUtils.getAllMethodsWithInherit(typeElement));
        allMethodList.addAll(findInterfaceMethods(typeElement));

        final List<ExecutableElement> rpcMethodList = new ArrayList<>();
        final Set<Integer> methodIdSet = new HashSet<>();
        for (final ExecutableElement method : allMethodList) {
            Map<String, AnnotationValue> annoValueMap = getMethodAnnoValueMap(method);
            if (annoValueMap == null) {  // 不是rpc方法
                continue;
            }
            final int methodId = getMethodId(method, annoValueMap);
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
        FirstArgType firstArgType = firstArgType(method);
        // 如果声明了context，方法的返回值必须是void
        if (firstArgType == FirstArgType.CONTEXT && method.getReturnType().getKind() != TypeKind.VOID) {
            messager.printMessage(Diagnostic.Kind.ERROR, "the return type of method(context) must be void", method);
        }

        // 检测方法参数个数
        int maxParameterCount = firstArgType.noCounting() ? MAX_PARAMETER_COUNT + 1 : MAX_PARAMETER_COUNT;
        if (parameters.size() > maxParameterCount) {
            messager.printMessage(Diagnostic.Kind.ERROR, "method has too many parameters!", method);
        }
        // 检查后续是否存在context参数 -- 意义不是很大
        for (int idx = firstArgType.noCounting() ? 1 : 0; idx < parameters.size(); idx++) {
            VariableElement variableElement = parameters.get(idx);
            if (isContext(variableElement.asType()) || isGenericContext(variableElement.asType())) {
                messager.printMessage(Diagnostic.Kind.ERROR, "context and request must be declared as the first parameter!", method);
                continue;
            }
        }
    }

    private void genProxyClass(TypeElement typeElement, List<ExecutableElement> rpcMethodList) {
        AnnotationMirror serviceAnnoMirror = AptUtils.findAnnotation(typeUtils, typeElement, anno_rpcServiceElement.asType())
                .orElseThrow();

        final int serviceId = AptUtils.getAnnotationValueValue(serviceAnnoMirror, PNAME_SERVICE_ID, null);
        if (AptUtils.getAnnotationValueValue(serviceAnnoMirror, PNAME_GEN_EXPORTER, Boolean.TRUE)) {
            genServerProxy(typeElement, serviceId, rpcMethodList);
        }
        if (AptUtils.getAnnotationValueValue(serviceAnnoMirror, PNAME_GEN_PROXY, Boolean.TRUE)) {
            genClientProxy(typeElement, serviceId, rpcMethodList);
        }
    }

    Integer getServiceId(TypeElement typeElement) {
        // 基本类型会被包装，Object不能直接转int
        return (Integer) AptUtils.findAnnotation(typeUtils, typeElement, anno_rpcServiceElement.asType())
                .map(annotationMirror -> AptUtils.getAnnotationValueValue(annotationMirror, PNAME_SERVICE_ID))
                .orElseThrow();
    }

    Map<String, AnnotationValue> getMethodAnnoValueMap(ExecutableElement method) {
        var annotationMirror = AptUtils.findAnnotation(typeUtils, method, anno_rpcMethodElement.asType())
                .orElse(null);
        if (annotationMirror == null) {
            return null;
        }
        return AptUtils.getAnnotationValuesMap(annotationMirror);
    }

    int getMethodId(ExecutableElement method, Map<String, AnnotationValue> annoValueMap) {
        return (Integer) annoValueMap.get(PNAME_METHOD_ID).getValue();
    }

    String getCustomData(ExecutableElement method, Map<String, AnnotationValue> annoValueMap) {
        AnnotationValue annotationValue = annoValueMap.get(PNAME_CUSTOM_DATA);
        if (annotationValue == null) {
            return null;
        }
        return (String) annotationValue.getValue();
    }

    /** 方法参数是否可共享 */
    boolean isArgSharable(ExecutableElement method, Map<String, AnnotationValue> annoValueMap) {
        // 指定了属性则以属性为准
        AnnotationValue annotationValue = annoValueMap.get(PNAME_ARG_SHARABLE);
        if (annotationValue != null) {
            return (Boolean) annotationValue.getValue();
        }

        // 如果所有参数都是不可变的，则默认true
        List<? extends VariableElement> parameters = method.getParameters();
        for (VariableElement parameter : parameters) {
            if (isContext(parameter.asType()) || isGenericContext(parameter.asType())) {
                continue;
            }
            if (!isImmutableType(parameter.asType())) {
                return false;
            }
        }
        return true;
    }

    /** 方法结果是否可共享 */
    boolean isResultSharable(ExecutableElement method, Map<String, AnnotationValue> annoValueMap) {
        // 指定了属性则以属性为准
        AnnotationValue annotationValue = annoValueMap.get(PNAME_RESULT_SHARABLE);
        if (annotationValue != null) {
            return (Boolean) annotationValue.getValue();
        }
        // 如果所有参数都是不可变的，则默认true
        List<? extends VariableElement> parameters = method.getParameters();
        for (VariableElement parameter : parameters) {
            if (isContext(parameter.asType()) || isGenericContext(parameter.asType())) {
                continue;
            }
            if (!isImmutableType(parameter.asType())) {
                return false;
            }
        }
        return true;
    }

    // 默认只对基础类型，包装类型，String做自动的判别
    private boolean isImmutableType(TypeMirror typeMirror) {
        if (typeMirror.getKind().isPrimitive()) {
            return true;
        }
        return immutableTypeMirrors.contains(typeMirror);
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

    /** 是否是通用Context */
    boolean isGenericContext(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, genericContextTypeMirror);
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

    FirstArgType firstArgType(ExecutableElement method) {
        List<? extends VariableElement> parameters = method.getParameters();
        if (parameters.size() == 0) return FirstArgType.NONE;

        TypeMirror typeMirror = parameters.get(0).asType();
        if (isContext(typeMirror)) return FirstArgType.CONTEXT;
        if (isGenericContext(typeMirror)) return FirstArgType.GENERIC_CONTEXT; // 需放在后面判断
        return FirstArgType.OTHER;
    }

}