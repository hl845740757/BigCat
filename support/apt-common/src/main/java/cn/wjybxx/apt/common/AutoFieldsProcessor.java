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
import cn.wjybxx.apt.BeanUtils;
import cn.wjybxx.apt.MyAbstractProcessor;
import com.google.auto.service.AutoService;
import com.squareup.javapoet.*;

import javax.annotation.processing.Processor;
import javax.annotation.processing.RoundEnvironment;
import javax.lang.model.element.*;
import javax.lang.model.util.Elements;
import javax.tools.Diagnostic;
import java.util.Collections;
import java.util.List;
import java.util.Set;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date 2023/4/6
 */
@AutoService(Processor.class)
public class AutoFieldsProcessor extends MyAbstractProcessor {

    public static final String CNAME_AUTO = "cn.wjybxx.common.annotation.AutoFields";
    private static final String CNAME_ALIAS = "cn.wjybxx.common.annotation.Alias";

    private static final String PROPERTY_SKIP_STATIC = "skipStatic";
    private static final String PROPERTY_SKIP_INSTANCE = "skipInstance";

    private static final String KEY_SET_FIELD_NAME = "_KEY_SET";
    public static final String KEY_SET_METHOD_NAME = "keySet";

    private TypeElement anno_autoTypeElement;
    private TypeElement anno_aliasTypeElement;

    private TypeName stringTypeName;
    private TypeName stringSetTypeName;

    @Override
    public Set<String> getSupportedAnnotationTypes() {
        return Collections.singleton(CNAME_AUTO);
    }

    @Override
    protected void ensureInited() {
        if (anno_autoTypeElement != null) {
            return;
        }

        anno_autoTypeElement = elementUtils.getTypeElement(CNAME_AUTO);
        anno_aliasTypeElement = elementUtils.getTypeElement(CNAME_ALIAS);

        stringTypeName = ClassName.get(String.class);
        stringSetTypeName = ParameterizedTypeName.get(Set.class, String.class);
    }

    @Override
    protected boolean doProcess(Set<? extends TypeElement> annotations, RoundEnvironment roundEnv) {
        final Set<? extends Element> annotatedClassSet = roundEnv.getElementsAnnotatedWith(anno_autoTypeElement);
        for (Element element : annotatedClassSet) {
            try {
                genFieldsClass((TypeElement) element);
            } catch (Throwable e) {
                messager.printMessage(Diagnostic.Kind.ERROR, AptUtils.getStackTrace(e), element);
            }
        }
        return true;
    }

    private void genFieldsClass(TypeElement typeElement) {
        final List<FieldSpec> constantFields = genConstantFields(typeElement);
        final FieldSpec keySetField = genKeySetField(constantFields);
        final MethodSpec keySetMethod = genKeySetMethod();

        final TypeSpec.Builder typeBuilder = TypeSpec.classBuilder(getProxyClassName(typeElement))
                .addModifiers(Modifier.PUBLIC, Modifier.FINAL)
                .addAnnotation(AptUtils.SUPPRESS_UNCHECKED_RAWTYPES)
                .addAnnotation(processorInfoAnnotation)
                .addAnnotation(AptUtils.newSourceFileRefAnnotation(ClassName.get(typeElement)))
                .addFields(constantFields)
                .addField(keySetField)
                .addMethod(keySetMethod);

        // 写入文件
        AptUtils.writeToFile(typeElement, typeBuilder, elementUtils, messager, filer);
    }

    private String getProxyClassName(TypeElement typeElement) {
        return getProxyClassName(typeElement, elementUtils);
    }

    public static String getProxyClassName(TypeElement typeElement, Elements elementUtils) {
        return AptUtils.getProxyClassName(elementUtils, typeElement, "Fields");
    }

    // region 常量字段

    /**
     * 为每一个字段生成对应的常量
     */
    private List<FieldSpec> genConstantFields(TypeElement typeElement) {
        if (typeElement.getKind() == ElementKind.ENUM) {
            return typeElement.getEnclosedElements().stream()
                    .filter(e -> e.getKind() == ElementKind.ENUM_CONSTANT)
                    .map(this::genFieldOfEnumConst)
                    .collect(Collectors.toList());
        } else {
            final AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, typeElement, anno_autoTypeElement.asType())
                    .orElseThrow();

            final boolean skipStatic = AptUtils.getAnnotationValueValue(annotationMirror, PROPERTY_SKIP_STATIC, true);
            final boolean skipInstance = AptUtils.getAnnotationValueValue(annotationMirror, PROPERTY_SKIP_INSTANCE, false);
            return BeanUtils.getAllFieldsWithInherit(typeElement).stream()
                    .filter(e -> e.getModifiers().contains(Modifier.STATIC) ? !skipStatic : !skipInstance)
                    .map(e -> genFieldOfClassField((VariableElement) e))
                    .collect(Collectors.toList());
        }
    }

    private FieldSpec genFieldOfEnumConst(Element element) {
        final String enumName = element.getSimpleName().toString();
        return FieldSpec.builder(stringTypeName, enumName, AptUtils.PUBLIC_STATIC_FINAL)
                .initializer("$S", enumName)
                .build();
    }

    private FieldSpec genFieldOfClassField(VariableElement element) {
        final AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, element, anno_aliasTypeElement.asType())
                .orElse(null);

        final String constantValue;
        if (annotationMirror == null) {
            constantValue = element.getSimpleName().toString();
        } else {
            constantValue = AptUtils.getAnnotationValueValue(annotationMirror, "value");
        }
        return FieldSpec.builder(stringTypeName, element.getSimpleName().toString(), AptUtils.PUBLIC_STATIC_FINAL)
                .initializer("$S", constantValue)
                .build();
    }

    // endregion

    // region keySet

    /** 生成常量字段集合 */
    private FieldSpec genKeySetField(List<FieldSpec> constantFields) {
        final StringBuilder stringBuilder = new StringBuilder(64);
        for (int i = 0, size = constantFields.size(); i < size; i++) {
            FieldSpec fieldSpec = constantFields.get(i);
            stringBuilder.append(fieldSpec.name);
            if (i < size - 1) {
                if (i % 5 == 0) {
                    stringBuilder.append(",\n            ");
                } else {
                    stringBuilder.append(", ");
                }
            }
        }
        return FieldSpec.builder(stringSetTypeName, KEY_SET_FIELD_NAME, AptUtils.PRIVATE_STATIC_FINAL)
                .initializer("Set.of($L)", stringBuilder.toString())
                .build();
    }

    private MethodSpec genKeySetMethod() {
        return MethodSpec.methodBuilder(KEY_SET_METHOD_NAME)
                .addModifiers(AptUtils.PUBLIC_STATIC)
                .returns(stringSetTypeName)
                .addStatement("return $L", KEY_SET_FIELD_NAME)
                .build();
    }
    // endregion

}