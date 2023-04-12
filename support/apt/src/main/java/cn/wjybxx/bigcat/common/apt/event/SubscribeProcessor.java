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

package cn.wjybxx.bigcat.common.apt.event;

import cn.wjybxx.bigcat.common.apt.AptUtils;
import cn.wjybxx.bigcat.common.apt.MyAbstractProcessor;
import com.google.auto.service.AutoService;
import com.squareup.javapoet.ClassName;
import com.squareup.javapoet.MethodSpec;
import com.squareup.javapoet.TypeName;
import com.squareup.javapoet.TypeSpec;

import javax.annotation.Nonnull;
import javax.annotation.processing.Processor;
import javax.annotation.processing.RoundEnvironment;
import javax.lang.model.element.*;
import javax.lang.model.type.DeclaredType;
import javax.lang.model.type.TypeKind;
import javax.lang.model.type.TypeMirror;
import javax.lang.model.util.Elements;
import javax.lang.model.util.SimpleTypeVisitor8;
import javax.tools.Diagnostic;
import java.util.*;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date 2023/4/7
 */
@AutoService(Processor.class)
public class SubscribeProcessor extends MyAbstractProcessor {

    private static final String SUBSCRIBE_CANONICAL_NAME = "cn.wjybxx.bigcat.common.eventbus.Subscribe";
    private static final String GENERIC_EVENT_CANONICAL_NAME = "cn.wjybxx.bigcat.common.eventbus.GenericEvent";
    private static final String HANDLER_REGISTRY_CANONICAL_NAME = "cn.wjybxx.bigcat.common.eventbus.EventHandlerRegistry";

    private static final String CHILD_EVENTS_PROPERTY_NAME = "childEvents";
    private static final String CHILD_DECLARED_PROPERTY_NAME = "childDeclared";
    private static final String CHILD_KEYS_PROPERTY_NAME = "childKeys";
    private static final String CUSTOM_DATA_PROPERTY_NAME = "customData";

    private TypeElement subscribeTypeElement;
    private TypeMirror subscribeTypeMirror;

    private TypeMirror genericEventTypeMirror;
    private TypeName handlerRegistryTypeName;

    @Override
    public Set<String> getSupportedAnnotationTypes() {
        return Collections.singleton(SUBSCRIBE_CANONICAL_NAME);
    }

    /**
     * 尝试初始化环境，也就是依赖的类都已经出现
     */
    @Override
    protected void ensureInited() {
        if (subscribeTypeElement != null) {
            // 已初始化
            return;
        }

        subscribeTypeElement = elementUtils.getTypeElement(SUBSCRIBE_CANONICAL_NAME);
        subscribeTypeMirror = subscribeTypeElement.asType();

        genericEventTypeMirror = typeUtils.getDeclaredType(elementUtils.getTypeElement(GENERIC_EVENT_CANONICAL_NAME));
        handlerRegistryTypeName = ClassName.get(elementUtils.getTypeElement(HANDLER_REGISTRY_CANONICAL_NAME));
    }

    @Override
    protected boolean doProcess(Set<? extends TypeElement> annotations, RoundEnvironment roundEnv) {
        // 注解标记的是方法，因此筛选出来的是Method，需要按类归组再生成
        Map<Element, List<Element>> class2MethodsMap = roundEnv.getElementsAnnotatedWith(subscribeTypeElement).stream()
                .collect(Collectors.groupingBy(Element::getEnclosingElement));

        for (Map.Entry<Element, List<Element>> entry : class2MethodsMap.entrySet()) {
            Element element = entry.getKey();
            List<Element> methodList = entry.getValue();
            try {
                genProxyClass((TypeElement) element, methodList);
            } catch (Throwable e) {
                messager.printMessage(Diagnostic.Kind.ERROR, AptUtils.getStackTrace(e), element);
            }
        }
        return true;
    }

    private void genProxyClass(TypeElement typeElement, List<? extends Element> methodList) {
        final TypeSpec.Builder typeBuilder = TypeSpec.classBuilder(getProxyClassName(elementUtils, typeElement))
                .addModifiers(Modifier.PUBLIC, Modifier.FINAL)
                .addAnnotation(AptUtils.SUPPRESS_UNCHECKED_ANNOTATION)
                .addAnnotation(processorInfoAnnotation)
                .addAnnotation(AptUtils.newSourceFileRefAnnotation(ClassName.get(typeElement)))
                .addMethod(genRegisterMethod(typeElement, methodList));

        // 写入文件
        AptUtils.writeToFile(typeElement, typeBuilder, elementUtils, messager, filer);
    }

    private static String getProxyClassName(Elements elementUtils, TypeElement typeElement) {
        return AptUtils.getProxyClassName(elementUtils, typeElement, "BusRegister");
    }

    /** @param methodList 当前类的事件方法 */
    private MethodSpec genRegisterMethod(TypeElement typeElement, List<? extends Element> methodList) {
        final MethodSpec.Builder builder = MethodSpec.methodBuilder("register")
                .addModifiers(Modifier.PUBLIC, Modifier.STATIC)
                .addParameter(handlerRegistryTypeName, "registry")
                .addParameter(TypeName.get(typeElement.asType()), "instance");

        for (Element element : methodList) {
            final ExecutableElement method = (ExecutableElement) element;
            if (method.getModifiers().contains(Modifier.STATIC)) {
                messager.printMessage(Diagnostic.Kind.ERROR, "Subscribe method can't be static!", method);
                continue;
            }
            if (method.getModifiers().contains(Modifier.PRIVATE)) {
                messager.printMessage(Diagnostic.Kind.ERROR, "Subscribe method can't be private!", method);
                continue;
            }
            if (method.getParameters().size() != 1) {
                messager.printMessage(Diagnostic.Kind.ERROR, "Subscribe method must have one and only one parameter!", method);
                continue;
            }

            final VariableElement eventParameter = method.getParameters().get(0);
            if (!isClassOrInterface(eventParameter.asType())) {
                // 事件参数必须是类或接口 (就不会是基本类型或泛型参数了，也排除了数组类型)
                messager.printMessage(Diagnostic.Kind.ERROR, "EventType must be class or interface!", method);
                continue;
            }

            // 生成注册代码
            final AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, method, subscribeTypeMirror).orElseThrow();
            final List<TypeMirror> childEventTypeMirrors = getChildEventTypeMirrors(method, annotationMirror);
            if (isGenericEvent(eventParameter)) {
                registerGenericChildHandlers(builder, method, annotationMirror, childEventTypeMirrors);
            } else if (childEventTypeMirrors.size() > 0) {
                registerMultiMasterHandlers(builder, method, annotationMirror, childEventTypeMirrors);
            } else {
                registerStringChildHandlers(builder, method, annotationMirror);
            }
        }
        return builder.build();
    }

    /**
     * 判断是否是接口或类型
     */
    private static boolean isClassOrInterface(TypeMirror typeMirror) {
        return typeMirror.getKind() == TypeKind.DECLARED;
    }

    /**
     * 判断监听的是否是泛型事件类型
     */
    private boolean isGenericEvent(VariableElement eventParameter) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, eventParameter.asType(), genericEventTypeMirror);
    }

    /**
     * 注册泛型事件处理器
     */
    private void registerGenericChildHandlers(MethodSpec.Builder builder, ExecutableElement method, AnnotationMirror annotationMirror,
                                              List<TypeMirror> childEventsList) {
        final VariableElement eventParameter = method.getParameters().get(0);
        final TypeName eventTParamTypeName = TypeName.get(typeUtils.erasure(eventParameter.asType()));
        final String methodName = method.getSimpleName().toString();
        final String customData = AptUtils.getAnnotationValueValue(annotationMirror, CUSTOM_DATA_PROPERTY_NAME);
        if (childEventsList.size() > 0) {
            // 声明了子事件，如果泛型参数不是通配符，子事件必须是泛型参数的子类
            if (!isTypeParameterWildCard(eventParameter)) {
                TypeMirror firstTypeArgument = checkedGetFirstTypeArgument(eventParameter, genericEventTypeMirror);
                checkChildEvents(method, firstTypeArgument, childEventsList);
            }
            for (TypeMirror typeMirror : childEventsList) {
                builder.addStatement("registry.register($T.class, $T.class, event -> instance.$L(event), $S)",
                        eventTParamTypeName, TypeName.get(typeUtils.erasure(typeMirror)), methodName, customData);
            }
        } else {
            // 未声明子事件，如果参数是具体类型（非通配符），则订阅泛型参数子键
            if (!isTypeParameterWildCard(eventParameter)) {
                TypeMirror firstTypeArgument = checkedGetFirstTypeArgument(eventParameter, genericEventTypeMirror);
                builder.addStatement("registry.register($T.class, $T.class, event -> instance.$L(event), $S)",
                        eventTParamTypeName, TypeName.get(typeUtils.erasure(firstTypeArgument)), methodName, customData);
            } else {
                // 退化为普通事件
                builder.addStatement("registry.register($T.class, null, event -> instance.$L(event), $S)",
                        eventTParamTypeName, methodName, customData);
            }
        }
    }

    /**
     * 通过childEvents属性订阅多个主事件
     */
    private void registerMultiMasterHandlers(MethodSpec.Builder builder, ExecutableElement method, AnnotationMirror annotationMirror,
                                             List<TypeMirror> childEventTypeMirrors) {
        final VariableElement eventParameter = method.getParameters().get(0);
        final TypeName eventTParamTypeName = TypeName.get(typeUtils.erasure(eventParameter.asType()));
        final String methodName = method.getSimpleName().toString();
        final String customData = AptUtils.getAnnotationValueValue(annotationMirror, CUSTOM_DATA_PROPERTY_NAME);

        checkChildEvents(method, eventParameter.asType(), childEventTypeMirrors);
        // 子类型需要显示转为超类型 - 否则可能导致重载问题
        for (TypeMirror typeMirror : childEventTypeMirrors) {
            builder.addStatement("registry.register($T.class, null, event -> instance.$L(($T) event), $S)",
                    TypeName.get(typeUtils.erasure(typeMirror)), methodName, eventTParamTypeName, customData);
        }
    }

    /**
     * 通过childKeys订阅多个子事件
     */
    private void registerStringChildHandlers(MethodSpec.Builder builder, ExecutableElement method, AnnotationMirror annotationMirror) {
        final VariableElement eventParameter = method.getParameters().get(0);
        final TypeName eventTParamTypeName = TypeName.get(typeUtils.erasure(eventParameter.asType()));
        final String methodName = method.getSimpleName().toString();
        final String customData = AptUtils.getAnnotationValueValue(annotationMirror, CUSTOM_DATA_PROPERTY_NAME);

        final List<? extends AnnotationValue> childKeyList = AptUtils.getAnnotationValueValue(annotationMirror, CHILD_KEYS_PROPERTY_NAME);
        if (childKeyList != null && !childKeyList.isEmpty()) {
            // 声明了childKeys，判断是类常量字段还是普通字符串
            final AnnotationValue childDeclared = AptUtils.getAnnotationValueValue(annotationMirror, CHILD_DECLARED_PROPERTY_NAME);
            final TypeMirror childKeyDeclaredTypeMirror = childDeclared == null ? null : AptUtils.getAnnotationValueTypeMirror(childDeclared);
            if (childKeyDeclaredTypeMirror != null) {
                final TypeName declaredTypeName = TypeName.get(childKeyDeclaredTypeMirror);
                for (AnnotationValue annotationValue : childKeyList) {
                    final String value = (String) annotationValue.getValue();
                    builder.addStatement("registry.register($T.class, $T.$L, event -> instance.$L(event), $S)",
                            eventTParamTypeName, declaredTypeName, value, methodName, customData);
                }
            } else {
                for (AnnotationValue annotationValue : childKeyList) {
                    final String value = (String) annotationValue.getValue();
                    builder.addStatement("registry.register($T.class, $S, event -> instance.$L(event), $S)",
                            eventTParamTypeName, value, methodName, customData);
                }
            }
        } else {
            // 未声明子事件，只注册方法参数本身
            builder.addStatement("registry.register($T.class, null, event -> instance.$L(event), $S)",
                    eventTParamTypeName, methodName, customData);
        }
    }

    /** 获取所有的子事件类型 */
    @Nonnull
    private List<TypeMirror> getChildEventTypeMirrors(ExecutableElement method, AnnotationMirror annotationMirror) {
        final List<? extends AnnotationValue> childEventsList = AptUtils.getAnnotationValueValue(annotationMirror, SubscribeProcessor.CHILD_EVENTS_PROPERTY_NAME);
        if (childEventsList == null || childEventsList.isEmpty()) {
            return List.of();
        }
        List<TypeMirror> result = new ArrayList<>(childEventsList.size());
        for (final AnnotationValue annotationValue : childEventsList) {
            final TypeMirror subEventTypeMirror = AptUtils.getAnnotationValueTypeMirror(annotationValue);
            if (null == subEventTypeMirror) {
                messager.printMessage(Diagnostic.Kind.ERROR, "Unsupported type " + annotationValue, method);
                continue;
            }
            result.add(subEventTypeMirror);
        }
        return result;
    }

    /** 检查子事件的合法性 */
    private void checkChildEvents(final ExecutableElement method, TypeMirror parentTypeMirror, List<TypeMirror> childEventMirrors) {
        for (TypeMirror childEventTypeMirror : childEventMirrors) {
            if (!AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, childEventTypeMirror, parentTypeMirror)) {
                messager.printMessage(Diagnostic.Kind.ERROR, "childEvent must be sub type of parent" + parentTypeMirror, method);
            }
        }
    }

    /**
     * 判断泛型参数是否是通配符
     */
    private boolean isTypeParameterWildCard(final VariableElement genericEventVariableElement) {
        return genericEventVariableElement.asType().accept(new SimpleTypeVisitor8<Boolean, Void>() {

            @Override
            protected Boolean defaultAction(TypeMirror e, Void aVoid) {
                return false;
            }

            @Override
            public Boolean visitDeclared(DeclaredType t, Void aVoid) {
                final List<? extends TypeMirror> typeArguments = t.getTypeArguments();
                if (typeArguments.isEmpty()) {
                    return false;
                }

                final TypeMirror typeMirror = typeArguments.get(0);
                return typeMirror.getKind() == TypeKind.WILDCARD;
            }
        }, null);
    }

    /**
     * 获取泛型事件的泛型参数
     */
    private TypeMirror checkedGetFirstTypeArgument(final VariableElement parameterizedType, TypeMirror targetType) {
        final DeclaredType superTypeMirror = AptUtils.upwardToSuperTypeMirror(typeUtils, parameterizedType.asType(), targetType);
        final TypeMirror firstParameterActualType = AptUtils.findFirstTypeParameter(superTypeMirror);
        if (firstParameterActualType == null || firstParameterActualType.getKind() != TypeKind.DECLARED) {
            messager.printMessage(Diagnostic.Kind.ERROR, "GenericEvent has a bad type parameter!", parameterizedType);
        }
        return firstParameterActualType;
    }
}