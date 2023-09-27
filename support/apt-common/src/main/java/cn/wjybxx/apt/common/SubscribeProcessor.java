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

package cn.wjybxx.apt.common;

import cn.wjybxx.apt.AptUtils;
import cn.wjybxx.apt.MyAbstractProcessor;
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

    private static final String CNAME_SUBSCRIBE = "cn.wjybxx.common.eventbus.Subscribe";
    private static final String CNAME_GENERIC_EVENT = "cn.wjybxx.common.eventbus.GenericEvent";
    private static final String CNAME_HANDLER_REGISTRY = "cn.wjybxx.common.eventbus.EventHandlerRegistry";

    private static final String PNAME_CHILD_EVENTS = "childEvents";
    private static final String PNAME_MASTER_DECLARED = "masterDeclared";
    private static final String PNAME_MASTER_KEY = "masterKey";
    private static final String PNAME_CHILD_DECLARED = "childDeclared";
    private static final String PNAME_CHILD_KEYS = "childKeys";
    private static final String PNAME_CUSTOM_DATA = "customData";

    private TypeElement anno_subscribeTypeElement;
    private TypeMirror genericEventTypeMirror;
    private TypeMirror objectTypeMirror;
    private TypeName handlerRegistryTypeName;

    @Override
    public Set<String> getSupportedAnnotationTypes() {
        return Collections.singleton(CNAME_SUBSCRIBE);
    }

    /**
     * 尝试初始化环境，也就是依赖的类都已经出现
     */
    @Override
    protected void ensureInited() {
        if (anno_subscribeTypeElement != null) {
            // 已初始化
            return;
        }

        anno_subscribeTypeElement = elementUtils.getTypeElement(CNAME_SUBSCRIBE);
        genericEventTypeMirror = typeUtils.getDeclaredType(elementUtils.getTypeElement(CNAME_GENERIC_EVENT));
        objectTypeMirror = typeUtils.getDeclaredType(elementUtils.getTypeElement(Object.class.getCanonicalName()));
        handlerRegistryTypeName = ClassName.get(elementUtils.getTypeElement(CNAME_HANDLER_REGISTRY));
    }

    @Override
    protected boolean doProcess(Set<? extends TypeElement> annotations, RoundEnvironment roundEnv) {
        // 注解标记的是方法，因此筛选出来的是Method，需要按类归组再生成
        Map<Element, List<Element>> class2MethodsMap = roundEnv.getElementsAnnotatedWith(anno_subscribeTypeElement).stream()
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
                .addAnnotation(AptUtils.SUPPRESS_UNCHECKED_RAWTYPES)
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
            final AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, method, anno_subscribeTypeElement.asType()).orElseThrow();
            final List<TypeMirror> childEventTypeMirrors = getChildEventTypeMirrors(method, annotationMirror);
            if (isGenericEvent(eventParameter)) {
                registerGenericChildHandlers(builder, method, annotationMirror, childEventTypeMirrors);
            } else {
                registerStringChildHandlers(builder, method, annotationMirror);
            }
        }
        return builder.build();
    }

    /** 判断是否是接口或Class */
    private static boolean isClassOrInterface(TypeMirror typeMirror) {
        return typeMirror.getKind() == TypeKind.DECLARED;
    }

    /** 判断监听的是否是泛型事件类型 */
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
        final String customData = AptUtils.getAnnotationValueValue(annotationMirror, PNAME_CUSTOM_DATA);
        if (childEventsList.size() > 0) {
            // 声明了子事件，如果泛型参数不是通配符，子事件必须是泛型参数的子类
            if (!isTypeParameterWildCardOrObject(eventParameter)) {
                TypeMirror firstTypeArgument = checkedGetFirstTypeArgument(eventParameter, genericEventTypeMirror);
                checkChildEvents(method, firstTypeArgument, childEventsList);
            }
            for (TypeMirror typeMirror : childEventsList) {
                builder.addStatement("registry.register($T.class, $T.class, event -> instance.$L(event), $S)",
                        eventTParamTypeName, TypeName.get(typeUtils.erasure(typeMirror)), methodName, customData);
            }
        } else {
            // 未声明子事件，如果参数是具体类型（非通配符），则订阅泛型参数子键
            if (!isTypeParameterWildCardOrObject(eventParameter)) {
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
     * 通过childKeys订阅多个子事件
     */
    private void registerStringChildHandlers(MethodSpec.Builder builder, ExecutableElement method, AnnotationMirror annotationMirror) {
        final VariableElement eventParameter = method.getParameters().get(0);
        final TypeName eventTParamTypeName = TypeName.get(typeUtils.erasure(eventParameter.asType()));
        final String methodName = method.getSimpleName().toString();
        final String customData = AptUtils.getAnnotationValueValue(annotationMirror, PNAME_CUSTOM_DATA);
        final List<String> childKeys = getChildKeys(annotationMirror);

        String masterKey = AptUtils.getAnnotationValueValue(annotationMirror, PNAME_MASTER_KEY, "");
        String masterKeyFormat;
        List<Object> masterKeyArgs = new ArrayList<>(3);
        if (!AptUtils.isBlank(masterKey)) {
            AnnotationValue masterDeclared = AptUtils.getAnnotationValue(annotationMirror, PNAME_MASTER_DECLARED);
            TypeMirror masterKeyDeclaredTypeMirror = masterDeclared == null ? null : AptUtils.getAnnotationValueTypeMirror(masterDeclared);
            if (masterKeyDeclaredTypeMirror != null) {
                masterKeyFormat = "registry.registerX($T.$L";
                masterKeyArgs.add(TypeName.get(masterKeyDeclaredTypeMirror));
                masterKeyArgs.add(masterKey);
            } else {
                masterKeyFormat = "registry.registerX($S";
                masterKeyArgs.add(masterKey);
            }
        } else {
            masterKeyFormat = "registry.register($T.class";
            masterKeyArgs.add(eventTParamTypeName);
        }
        final int masterKeyArgsSize = masterKeyArgs.size();
        if (childKeys.size() > 0) {
            // 声明了childKeys，判断是类常量字段还是普通字符串
            final AnnotationValue childDeclared = AptUtils.getAnnotationValue(annotationMirror, PNAME_CHILD_DECLARED);
            final TypeMirror childKeyDeclaredTypeMirror = childDeclared == null ? null : AptUtils.getAnnotationValueTypeMirror(childDeclared);
            if (childKeyDeclaredTypeMirror != null) {
                masterKeyArgs.add(TypeName.get(childKeyDeclaredTypeMirror));
                masterKeyArgs.add(""); // 预填充，因为其它参数不会变，预填充的效果更好
                masterKeyArgs.add(methodName);
                masterKeyArgs.add(customData);
                for (String childKey : childKeys) {
                    masterKeyArgs.set(masterKeyArgsSize + 1, childKey);
                    builder.addStatement(masterKeyFormat + ", $T.$L, event -> instance.$L(event), $S)", masterKeyArgs.toArray());
                }
            } else {
                masterKeyArgs.add("");
                masterKeyArgs.add(methodName);
                masterKeyArgs.add(customData);
                for (String childKey : childKeys) {
                    masterKeyArgs.set(masterKeyArgsSize, childKey);
                    builder.addStatement(masterKeyFormat + ", $S, event -> instance.$L(event), $S)", masterKeyArgs.toArray());
                }
            }
        } else {
            // 未声明子事件，只注册方法参数本身
            masterKeyArgs.add(methodName);
            masterKeyArgs.add(customData);
            builder.addStatement(masterKeyFormat + ", null, event -> instance.$L(event), $S)", masterKeyArgs.toArray());
        }
    }

    /** 获取所有的字符串子键 */
    private List<String> getChildKeys(AnnotationMirror annotationMirror) {
        List<? extends AnnotationValue> childKeyList = AptUtils.getAnnotationValueValue(annotationMirror, PNAME_CHILD_KEYS);
        if (childKeyList == null) {
            return new ArrayList<>();
        }
        ArrayList<String> result = new ArrayList<>(childKeyList.size());
        for (AnnotationValue annotationValue : childKeyList) {
            String childKey = (String) annotationValue.getValue();
            if (AptUtils.isEmpty(childKey)) {
                continue;
            }
            result.add(childKey);
        }
        return result;
    }

    /** 获取所有的子事件类型 */
    @Nonnull
    private List<TypeMirror> getChildEventTypeMirrors(ExecutableElement method, AnnotationMirror annotationMirror) {
        List<? extends AnnotationValue> childEventsList = AptUtils.getAnnotationValueValue(annotationMirror, PNAME_CHILD_EVENTS);
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
     * 判断泛型参数是否是通配符或Object
     */
    private boolean isTypeParameterWildCardOrObject(final VariableElement genericEventVariableElement) {
        return genericEventVariableElement.asType().accept(new SimpleTypeVisitor8<Boolean, Void>() {

            @Override
            protected Boolean defaultAction(TypeMirror e, Void aVoid) {
                return false;
            }

            @Override
            public Boolean visitDeclared(DeclaredType t, Void aVoid) {
                final List<? extends TypeMirror> typeArguments = t.getTypeArguments();
                if (typeArguments.isEmpty()) {
                    return true; // 泛型擦除，未声明泛型参数
                }

                final TypeMirror typeMirror = typeArguments.get(0);
                if (typeMirror.getKind() == TypeKind.WILDCARD) {
                    return true; // 通配符
                }
                return AptUtils.isSameTypeIgnoreTypeParameter(typeUtils, typeMirror, objectTypeMirror);
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