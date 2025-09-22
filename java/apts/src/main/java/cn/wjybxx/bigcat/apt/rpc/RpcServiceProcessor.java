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

import cn.wjybxx.apt.AptUtils;
import cn.wjybxx.apt.BeanUtils;
import cn.wjybxx.apt.MyAbstractProcessor;
import com.google.auto.service.AutoService;
import com.squareup.javapoet.ClassName;
import com.squareup.javapoet.TypeSpec;

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
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date 2023/4/12
 */
@AutoService(Processor.class)
public class RpcServiceProcessor extends MyAbstractProcessor {

    private static final String CNAME_RPC_SERVICE = "cn.wjybxx.bigcat.fx.RpcService";
    private static final String PNAME_SERVICE_ID = "serviceId";

    private static final String CNAME_RPC_METHOD = "cn.wjybxx.bigcat.fx.RpcMethod";
    private static final String PNAME_METHOD_ID = "methodId";
    private static final String PNAME_ARG_SHARABLE = "argSharable";
    private static final String PNAME_RESULT_SHARABLE = "resultSharable";
    private static final String PNAME_MANUAL_RETURN = "manualReturn";
    private static final String PNAME_CUSTOM_DATA = "customData";
    private static final String PNAME_BUILDER_PATTERN = "builderPattern";

    private static final String CNAME_METHOD_SPEC = "cn.wjybxx.bigcat.fx.RpcMethodSpec";
    private static final String CNAME_METHOD_REGISTRY = "cn.wjybxx.bigcat.fx.RpcMethodRegistry";
    private static final String CNAME_CONTEXT = "cn.wjybxx.bigcat.fx.RpcContext";

    private static final String CNAME_MY_FUTURE = "cn.wjybxx.concurrent.IFuture";
    private static final String CNAME_PROTOBUF_MESSAGE = "com.google.protobuf.AbstractMessage";
    private static final int MAX_PARAMETER_COUNT = 1; // 限制最大一个参数

    private TypeElement anno_rpcServiceElement;
    private TypeElement anno_rpcMethodElement;

    TypeElement type_MethodSpec;
    ClassName typeName_MethodSpecRaw;
    ClassName typeName_MethodRegistry;

    TypeMirror typeMirror_context;
    ClassName typeName_ContextRaw;

    TypeMirror typeMirror_BoxedVoid;
    TypeMirror typeMirror_Object;
    TypeMirror typeMirror_String;
    TypeMirror typeMirror_Message;

    /** 支持的future类型 */
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
        // methodSpc
        type_MethodSpec = elementUtils.getTypeElement(CNAME_METHOD_SPEC);
        typeName_MethodSpecRaw = AptUtils.classNameOfCanonicalName(CNAME_METHOD_SPEC);
        typeName_MethodRegistry = AptUtils.classNameOfCanonicalName(CNAME_METHOD_REGISTRY);
        // ctx
        typeMirror_context = elementUtils.getTypeElement(CNAME_CONTEXT).asType();
        typeName_ContextRaw = AptUtils.classNameOfCanonicalName(CNAME_CONTEXT);

        typeMirror_BoxedVoid = AptUtils.getTypeElementOfClass(elementUtils, Void.class).asType();
        typeMirror_Object = AptUtils.getTypeElementOfClass(elementUtils, Object.class).asType();
        typeMirror_String = AptUtils.getTypeElementOfClass(elementUtils, String.class).asType();
        try {
            typeMirror_Message = elementUtils.getTypeElement(CNAME_PROTOBUF_MESSAGE).asType();
        } catch (Exception ignore) {
        }

        futureTypeMirrors.add(AptUtils.getTypeElementOfClass(elementUtils, CompletableFuture.class).asType());
        futureTypeMirrors.add(elementUtils.getTypeElement(CNAME_MY_FUTURE).asType());

        for (TypeKind typeKind : TypeKind.values()) {
            if (!typeKind.isPrimitive()) continue;
            PrimitiveType primitiveType = typeUtils.getPrimitiveType(typeKind);
            TypeElement typeElement = typeUtils.boxedClass(primitiveType);
            immutableTypeMirrors.add(typeElement.asType());
        }
        immutableTypeMirrors.add(typeMirror_String);
    }

    @Override
    protected boolean doProcess(Set<? extends TypeElement> annotations, RoundEnvironment roundEnv) {
        Set<TypeElement> typeElementSet = AptUtils.selectSourceFile(roundEnv, elementUtils, anno_rpcServiceElement);
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
    // region check

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
            if (methodId == 0) { // 未正确初始化
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
            if (!methodIdSet.add(methodId)) { // 同一个类中的方法id不可以重复
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
        return (TypeElement) declaredType.asElement();
    }

    private void checkParameters(ExecutableElement method) {
        List<? extends VariableElement> parameters = method.getParameters();
        if (parameters.size() == 0) {
            return;
        }
        FirstArgType firstArgType = getFirstArgType(method);
        // 检测方法参数个数
        int maxParameterCount = firstArgType == FirstArgType.CONTEXT ? MAX_PARAMETER_COUNT + 1 : MAX_PARAMETER_COUNT;
        if (parameters.size() > maxParameterCount) {
            messager.printMessage(Diagnostic.Kind.ERROR, "method has too many parameters!", method);
        }
        // 泛型参数建议使用object代替void
        if (firstArgType == FirstArgType.CONTEXT) {
            TypeMirror typeArgument = findFirstTypeArgument(parameters.get(0).asType(), method);
            if (typeUtils.isSameType(typeArgument, typeMirror_BoxedVoid)) {
                messager.printMessage(Diagnostic.Kind.WARNING, "please use object instead of void", method);
            }
        }
        // 检查后续是否存在context参数
        for (int idx = firstArgType == FirstArgType.CONTEXT ? 1 : 0; idx < parameters.size(); idx++) {
            VariableElement variableElement = parameters.get(idx);
            if (isContext(variableElement.asType())) {
                messager.printMessage(Diagnostic.Kind.ERROR, "context must be declared as the first parameter!", method);
            }
            // 其实本地还是支持基本类型的
//            if (variableElement.asType().getKind().isPrimitive()) {
//                messager.printMessage(Diagnostic.Kind.ERROR, "rpc no longer support primitive types!", method);
//            }
        }
    }

    private void checkReturnType(ExecutableElement method) {
        // 其实本地还是支持基本类型的
//        TypeMirror returnType = rpcReturnType(method, false);
//        if (returnType.getKind() != TypeKind.VOID && returnType.getKind() != TypeKind.DECLARED) {
//            messager.printMessage(Diagnostic.Kind.ERROR, "rpc returnType must be void or class!", method);
//        }
        // 泛型参数建议使用object代替void
        if (isFuture(method.getReturnType())) {
            TypeMirror typeArgument = findFirstTypeArgument(method.getReturnType(), method);
            if (typeUtils.isSameType(typeArgument, typeMirror_BoxedVoid)) {
                messager.printMessage(Diagnostic.Kind.WARNING, "please use object instead of void", method);
            }
        }
    }
    // endregion

    // region gen

    private void genProxyClass(TypeElement typeElement, List<ExecutableElement> rpcMethodList) {
        AnnotationMirror serviceAnnoMirror = AptUtils.findAnnotation(typeUtils, typeElement, anno_rpcServiceElement.asType());
        final int serviceId = AptUtils.getAnnotationValueValue(serviceAnnoMirror, PNAME_SERVICE_ID, null);
        TypeSpec.Builder builder = new RpcProxyGenerator(this, typeElement, serviceId, rpcMethodList)
                .execute2();
        new RpcExporterGenerator(this, typeElement, serviceId, rpcMethodList)
                .execute2(builder);
        AptUtils.writeToFile(typeElement, builder, elementUtils, messager, filer);
    }

    // endregion

    // region 注解解析

    Map<String, AnnotationValue> getMethodAnnoValueMap(ExecutableElement method) {
        AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, method, anno_rpcMethodElement.asType());
        if (annotationMirror == null) {
            return null;
        }
        return AptUtils.getAnnotationValuesMap(annotationMirror);
    }

    int getMethodId(ExecutableElement method, Map<String, AnnotationValue> annoValueMap) {
        return (Integer) annoValueMap.get(PNAME_METHOD_ID).getValue();
    }

    /** 获取方法切面数据 */
    String getCustomData(ExecutableElement method, Map<String, AnnotationValue> annoValueMap) {
        AnnotationValue annotationValue = annoValueMap.get(PNAME_CUSTOM_DATA);
        if (annotationValue == null) {
            return null;
        }
        return (String) annotationValue.getValue();
    }

    /** 使用使用builder模式 */
    boolean isBuilderPattern(ExecutableElement method, Map<String, AnnotationValue> annoValueMap) {
        AnnotationValue annotationValue = annoValueMap.get(PNAME_BUILDER_PATTERN);
        if (annotationValue == null) {
            return false;
        }
        return (Boolean) annotationValue.getValue();
    }

    /** 是否手动返回结果 */
    boolean isManualReturn(ExecutableElement method, Map<String, AnnotationValue> annoValueMap) {
        AnnotationValue annotationValue = annoValueMap.get(PNAME_MANUAL_RETURN);
        if (annotationValue == null) {
            return false;
        }
        return (boolean) annotationValue.getValue();
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
            if (isContext(parameter.asType())) {
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
        TypeMirror returnType = rpcReturnType(method);
        return isImmutableType(returnType);
    }

    // 默认只对基础类型，包装类型，String做自动的判别
    private boolean isImmutableType(TypeMirror typeMirror) {
        if (typeMirror.getKind().isPrimitive() || typeMirror.getKind() == TypeKind.VOID) {
            return true;
        }
        if (immutableTypeMirrors.contains(typeMirror)) {
            return true;
        }
        if (typeMirror_Message != null
                && AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, typeMirror_Message)) {
            return true;
        }
        return false;
    }
    // endregion

    /** 是否是context参数 */
    boolean isContext(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, typeMirror_context);
    }

    /** 是否是future类型 */
    boolean isFuture(TypeMirror typeMirror) {
        for (TypeMirror futureTypeMirror : futureTypeMirrors) {
            if (AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, futureTypeMirror)) {
                return true;
            }
        }
        return false;
    }

    /** 获取rpc方法第一个参数的类型 */
    FirstArgType getFirstArgType(ExecutableElement method) {
        List<? extends VariableElement> parameters = method.getParameters();
        if (parameters.size() == 0) return FirstArgType.NONE;

        TypeMirror typeMirror = parameters.get(0).asType();
        if (isContext(typeMirror)) return FirstArgType.CONTEXT;
        return FirstArgType.OTHER;
    }

    /**
     * 解析Rpc方法的返回值类型
     * 如果是基本类型，会进行装箱；(不再支持基本类型)
     * 如果是void，会转为Object
     * 如果是Future，会解析泛型参数；
     * 如果是RpcContext，会解析泛型参数
     */
    TypeMirror rpcReturnType(ExecutableElement method) {
        TypeMirror returnType = method.getReturnType();
        if (returnType.getKind() == TypeKind.VOID) {
            // 包含context时，context的泛型值作为返回值类型
            List<? extends VariableElement> parameters = method.getParameters();
            if (parameters.size() > 0 && isContext(parameters.get(0).asType())) {
                return findFirstTypeArgument(parameters.get(0).asType(), method);
            }
            // void转object
            return typeMirror_Object;
        }
        // future类型，future的泛型值作为返回值类型
        if (isFuture(returnType)) {
            return findFirstTypeArgument(returnType, method);
        } else {
            return returnType;
        }
    }

    /** @param method 用于打印错误 */
    TypeMirror findFirstTypeArgument(TypeMirror typeMirror, ExecutableElement method) {
        TypeMirror firstTypeParameter = AptUtils.findFirstTypeParameter(typeMirror);
        if (firstTypeParameter == null) {
            messager.printMessage(Diagnostic.Kind.WARNING, "Future missing type parameter!", method);
            return typeMirror_Object;
        } else {
            return firstTypeParameter;
        }
    }
}