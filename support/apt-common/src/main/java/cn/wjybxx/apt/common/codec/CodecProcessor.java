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

package cn.wjybxx.apt.common.codec;

import cn.wjybxx.apt.AptUtils;
import cn.wjybxx.apt.BeanUtils;
import cn.wjybxx.apt.MyAbstractProcessor;
import com.squareup.javapoet.AnnotationSpec;
import com.squareup.javapoet.ClassName;
import com.squareup.javapoet.MethodSpec;
import com.squareup.javapoet.TypeName;

import javax.lang.model.element.*;
import javax.lang.model.type.DeclaredType;
import javax.lang.model.type.TypeKind;
import javax.lang.model.type.TypeMirror;
import javax.lang.model.util.Elements;
import javax.lang.model.util.Types;
import javax.tools.Diagnostic;
import java.util.ArrayList;
import java.util.List;

/**
 * @author wjybxx
 * date 2023/4/13
 */
public abstract class CodecProcessor extends MyAbstractProcessor {

    public static final String CNAME_FIELD_IMPL = "cn.wjybxx.common.codec.FieldImpl";
    public static final String CNAME_CLASS_IMPL = "cn.wjybxx.common.codec.ClassImpl";

    public static final String CNAME_WireType = "cn.wjybxx.dson.WireType";
    public static final String CNAME_NumberStyle = "cn.wjybxx.dson.text.NumberStyle";
    public static final String CNAME_StringStyle = "cn.wjybxx.dson.text.StringStyle";
    public static final String CNAME_ObjectStyle = "cn.wjybxx.dson.text.ObjectStyle";

    // CodecImpl
    public static final String MNAME_GET_ENCODER_CLASS = "getEncoderClass";
    public static final String MNAME_WRITE_OBJECT = "writeObject";
    public static final String MNAME_READ_OBJECT = "readObject";

    // AbstractCodecImpl
    public static final String MNAME_NEW_INSTANCE = "newInstance";
    public static final String MNAME_READ_FIELDS = "readFields";
    public static final String MNAME_AFTER_DECODE = "afterDecode";

    public static final String CNAME_ENUM_LITE = "cn.wjybxx.common.EnumLite";
    public static final String MNAME_FOR_NUMBER = "forNumber";
    public static final String MNAME_GET_NUMBER = "getNumber";

    public TypeElement anno_serializableTypeElement;
    public TypeElement anno_ignoreTypeElement;

    public TypeElement readerTypeElement;
    public TypeElement writerTypeElement;
    public TypeElement enumCodecTypeElement;

    // 要覆盖的方法缓存，减少大量查询
    public TypeElement codecTypeElement;
    public ExecutableElement getEncoderClassMethod;
    public ExecutableElement writeObjectMethod;
    public ExecutableElement readObjectMethod;

    public TypeElement abstractCodecTypeElement;
    public ExecutableElement newInstanceMethod;
    public ExecutableElement readFieldsMethod;
    public ExecutableElement afterDecodeMethod;

    public TypeMirror anno_fieldImplTypeMirror;
    public TypeMirror anno_classImplTypeMirror;
    public TypeElement autoSchemaTypeElement;

    public TypeName typeNameWireType;
    public TypeName typeNameNumberStyle;
    public TypeName typeNameStringStyle;
    public TypeName typeNameObjectStyle;

    public TypeMirror enumLiteTypeMirror;
    public TypeMirror stringTypeMirror;
    public TypeMirror enumTypeMirror;

    public CodecProcessor() {
    }

    protected void ensureInited(String serializableCanonicalName, String ignoreCanonicalName,
                                String readerCanonicalName, String writerCanonicalName,
                                String codecCanonicalName, String abstractCodecCanonicalName,
                                String enumCodecCanonicalName) {
        if (anno_serializableTypeElement != null) {
            return;
        }
        anno_serializableTypeElement = elementUtils.getTypeElement(serializableCanonicalName);
        anno_ignoreTypeElement = elementUtils.getTypeElement(ignoreCanonicalName);
        readerTypeElement = elementUtils.getTypeElement(readerCanonicalName);
        writerTypeElement = elementUtils.getTypeElement(writerCanonicalName);
        enumCodecTypeElement = elementUtils.getTypeElement(enumCodecCanonicalName);

        codecTypeElement = elementUtils.getTypeElement(codecCanonicalName);
        getEncoderClassMethod = AptUtils.findMethodByName(codecTypeElement, MNAME_GET_ENCODER_CLASS);
        writeObjectMethod = AptUtils.findMethodByName(codecTypeElement, MNAME_WRITE_OBJECT);
        readObjectMethod = AptUtils.findMethodByName(codecTypeElement, MNAME_READ_OBJECT);

        abstractCodecTypeElement = elementUtils.getTypeElement(abstractCodecCanonicalName);
        newInstanceMethod = AptUtils.findMethodByName(abstractCodecTypeElement, MNAME_NEW_INSTANCE);
        readFieldsMethod = AptUtils.findMethodByName(abstractCodecTypeElement, MNAME_READ_FIELDS);
        afterDecodeMethod = AptUtils.findMethodByName(abstractCodecTypeElement, MNAME_AFTER_DECODE);

        anno_fieldImplTypeMirror = elementUtils.getTypeElement(CNAME_FIELD_IMPL).asType();
        anno_classImplTypeMirror = elementUtils.getTypeElement(CNAME_CLASS_IMPL).asType();
        autoSchemaTypeElement = elementUtils.getTypeElement(AutoSchemaProcessor.CNAME_AUTO);

        typeNameWireType = AptUtils.classNameOfCanonicalName(CNAME_WireType);
        typeNameNumberStyle = AptUtils.classNameOfCanonicalName(CNAME_NumberStyle);
        typeNameStringStyle = AptUtils.classNameOfCanonicalName(CNAME_StringStyle);
        typeNameObjectStyle = AptUtils.classNameOfCanonicalName(CNAME_ObjectStyle);

        stringTypeMirror = elementUtils.getTypeElement(String.class.getCanonicalName()).asType();
        enumTypeMirror = elementUtils.getTypeElement(Enum.class.getCanonicalName()).asType();
        enumLiteTypeMirror = elementUtils.getTypeElement(CNAME_ENUM_LITE).asType();
    }

    public boolean isClassOrEnum(TypeElement typeElement) {
        return typeElement.getKind() == ElementKind.CLASS
                || typeElement.getKind() == ElementKind.ENUM;
    }

    public boolean isString(TypeMirror typeMirror) {
        return typeUtils.isSameType(typeMirror, stringTypeMirror);
    }

    public boolean isByteArray(TypeMirror typeMirror) {
        return AptUtils.isByteArray(typeMirror);
    }

    protected boolean isEnum(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, enumTypeMirror);
    }

    protected boolean isEnumLite(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, enumLiteTypeMirror);
    }

    // region 枚举

    /**
     * 检查枚举 - 要序列化的枚举，必须实现 EnumLite 接口，否则无法序列化，或自己手写serializer。
     */
    public void checkEnum(TypeElement typeElement) {
        if (!isEnumLite(typeElement.asType())) {
            messager.printMessage(Diagnostic.Kind.ERROR,
                    "serializable enum must implement EnumLite",
                    typeElement);
            return;
        }
        checkForNumberMethod(typeElement);
    }

    protected void checkForNumberMethod(TypeElement typeElement) {
        if (!containNotPrivateStaticForNumberMethod(typeElement)) {
            messager.printMessage(Diagnostic.Kind.ERROR,
                    "serializable enum contains a not private 'static T forNumber(int)' method!",
                    typeElement);
        }
    }

    /**
     * 是否包含静态的非private的forNumber方法
     */
    private boolean containNotPrivateStaticForNumberMethod(TypeElement typeElement) {
        return typeElement.getEnclosedElements().stream()
                .filter(e -> e.getKind() == ElementKind.METHOD)
                .map(e -> (ExecutableElement) e)
                .filter(method -> !method.getModifiers().contains(Modifier.PRIVATE))
                .filter(method -> method.getModifiers().contains(Modifier.STATIC))
                .filter(method -> method.getParameters().size() == 1)
                .filter(method -> method.getSimpleName().toString().equals(MNAME_FOR_NUMBER))
                .anyMatch(method -> method.getParameters().get(0).asType().getKind() == TypeKind.INT);
    }
    // endregion

    // region 普通类

    protected void checkNormalClass(TypeElement typeElement) {
        checkAutoArgs(typeElement);
        checkConstructor(typeElement);

        final List<? extends Element> allFieldsAndMethodWithInherit = BeanUtils.getAllFieldsAndMethodsWithInherit(typeElement);
        final AptClassImpl aptClassImpl = parseClassImpl(typeElement);
        for (Element element : allFieldsAndMethodWithInherit) {
            if (element.getKind() != ElementKind.FIELD) {
                continue;
            }
            final VariableElement variableElement = (VariableElement) element;
            if (!isSerializableField(variableElement)) {
                continue;
            }

            AptFieldImpl aptFieldImpl = parseFiledImpl(variableElement);
            if (isAutoWriteField(variableElement, aptClassImpl, aptFieldImpl)) {
                if (aptFieldImpl.hasWriteProxy()) {
                    continue;
                }
                // 工具写：需要提供可直接取值或包含非private的getter方法
                if (AptUtils.isBlank(aptFieldImpl.getter)
                        && !canGetDirectly(variableElement, typeElement)
                        && !containsNotPrivateGetter(variableElement, allFieldsAndMethodWithInherit)) {
                    messager.printMessage(Diagnostic.Kind.ERROR,
                            String.format("serializable field (%s) must contains a not private getter or canGetDirectly", variableElement.getSimpleName()),
                            typeElement); // 可能无法定位到超类字段，因此打印到Type
                    continue;
                }
            }
            if (isAutoReadField(variableElement, aptClassImpl, aptFieldImpl)) {
                if (aptFieldImpl.hasReadProxy()) {
                    continue;
                }
                // 工具读：需要提供可直接赋值或非private的setter方法
                if (AptUtils.isBlank(aptFieldImpl.setter) &&
                        !canSetDirectly(variableElement, typeElement)
                        && !containsNotPrivateSetter(variableElement, allFieldsAndMethodWithInherit)) {
                    messager.printMessage(Diagnostic.Kind.ERROR,
                            String.format("serializable field (%s) must contains a not private setter or canSetDirectly", variableElement.getSimpleName()),
                            typeElement);
                    continue;
                }
            }
        }
    }

    /** 检查是否包含了AutoSchema注解 */
    public void checkAutoArgs(TypeElement typeElement) {
        final AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, typeElement, autoSchemaTypeElement.asType())
                .orElse(null);
        if (annotationMirror == null) {
            messager.printMessage(Diagnostic.Kind.ERROR, "SerializableClass must contains AutoSchema annotation", typeElement);
        }
    }

    /** 检查是否包含无参构造方法或解析构造方法 */
    protected void checkConstructor(TypeElement typeElement) {
        if (typeElement.getModifiers().contains(Modifier.ABSTRACT)) {
            return;
        }
        if (BeanUtils.containsNoArgsConstructor(typeElement)
                || containsReaderConstructor(typeElement)) {
            return;
        }
        String typeName = typeElement.getSimpleName().toString();
        String readerName = readerTypeElement.getSimpleName().toString();
        messager.printMessage(Diagnostic.Kind.ERROR,
                String.format("SerializableClass %s must contains no-args constructor or %s-args constructor!", typeName, readerName),
                typeElement);
    }

    /** 是否包含 T(Reader reader) 构造方法 */
    public boolean containsReaderConstructor(TypeElement typeElement) {
        return BeanUtils.containsOneArgsConstructor(typeUtils, typeElement, readerTypeElement.asType());
    }

    /** 是否包含 readerObject 实例方法 */
    public boolean containsReadObjectMethod(List<? extends Element> allFieldsAndMethodWithInherit) {
        return allFieldsAndMethodWithInherit.stream()
                .filter(e -> e.getKind() == ElementKind.METHOD)
                .map(e -> (ExecutableElement) e)
                .filter(e -> !e.getModifiers().contains(Modifier.PRIVATE) && !e.getModifiers().contains(Modifier.STATIC))
                .filter(e -> e.getParameters().size() == 1)
                .filter(e -> e.getSimpleName().toString().equals(MNAME_READ_OBJECT))
                .anyMatch(e -> AptUtils.isSameTypeIgnoreTypeParameter(typeUtils, e.getParameters().get(0).asType(), readerTypeElement.asType()));
    }

    /** 是否包含 writeObject 实例方法 */
    public boolean containsWriteObjectMethod(List<? extends Element> allFieldsAndMethodWithInherit) {
        return allFieldsAndMethodWithInherit.stream()
                .filter(e -> e.getKind() == ElementKind.METHOD)
                .map(e -> (ExecutableElement) e)
                .filter(e -> !e.getModifiers().contains(Modifier.PRIVATE) && !e.getModifiers().contains(Modifier.STATIC))
                .filter(e -> e.getParameters().size() == 1)
                .filter(e -> e.getSimpleName().toString().equals(MNAME_WRITE_OBJECT))
                .anyMatch(e -> AptUtils.isSameTypeIgnoreTypeParameter(typeUtils, e.getParameters().get(0).asType(), writerTypeElement.asType()));
    }

    /** 查找反序列化钩子方法 */
    public ExecutableElement findAfterDecodeMethod(List<? extends Element> allFieldsAndMethodWithInherit) {
        return allFieldsAndMethodWithInherit.stream()
                .filter(e -> e.getKind() == ElementKind.METHOD)
                .filter(e -> !e.getModifiers().contains(Modifier.PRIVATE) && !e.getModifiers().contains(Modifier.STATIC))
                .map(e -> (ExecutableElement) e)
                .filter(e -> e.getParameters().size() == 0)
                .filter(e -> e.getSimpleName().toString().equals(MNAME_AFTER_DECODE))
                .findFirst()
                .orElse(null);
    }

    public AptClassImpl parseClassImpl(TypeElement typeElement) {
        return AptClassImpl.parse(typeUtils, typeElement, anno_classImplTypeMirror);
    }

    // 字段处理

    public AptFieldImpl parseFiledImpl(VariableElement variableElement) {
        return AptFieldImpl.parse(typeUtils, variableElement, anno_fieldImplTypeMirror);
    }

    /**
     * 测试{@link TypeElement}是否可以直接读取字段。
     * （这里需要考虑继承问题）
     *
     * @param variableElement 类字段，可能是继承的字段
     * @return 如果可直接取值，则返回true
     */
    public boolean canGetDirectly(final VariableElement variableElement, TypeElement typeElement) {
        if (variableElement.getModifiers().contains(Modifier.PUBLIC)) {
            return true;
        }
        if (variableElement.getModifiers().contains(Modifier.PRIVATE)) {
            return false;
        }
        return isMemberOrPackageMember(variableElement, typeElement);
    }

    /**
     * 测试{@link TypeElement}是否可以直接写字段。
     * （这里需要考虑继承问题）
     *
     * @param variableElement 类字段，可能是继承的字段
     * @return 如果可直接赋值，则返回true
     */
    public boolean canSetDirectly(final VariableElement variableElement, TypeElement typeElement) {
        if (variableElement.getModifiers().contains(Modifier.FINAL) || variableElement.getModifiers().contains(Modifier.PRIVATE)) {
            return false;
        }
        if (variableElement.getModifiers().contains(Modifier.PUBLIC)) {
            return true;
        }
        return isMemberOrPackageMember(variableElement, typeElement);
    }

    private boolean isMemberOrPackageMember(VariableElement variableElement, TypeElement typeElement) {
        final TypeElement enclosingElement = (TypeElement) variableElement.getEnclosingElement();
        if (enclosingElement.equals(typeElement)) {
            return true;
        }
        return elementUtils.getPackageOf(enclosingElement).equals(elementUtils.getPackageOf(typeElement));
    }

    /**
     * 是否包含非private的getter方法
     *
     * @param allFieldsAndMethodWithInherit 所有的字段和方法，可能在父类中
     */
    public boolean containsNotPrivateGetter(final VariableElement variableElement, final List<? extends Element> allFieldsAndMethodWithInherit) {
        return BeanUtils.containsNotPrivateGetter(typeUtils, variableElement, allFieldsAndMethodWithInherit);
    }

    /**
     * 是否包含非private的setter方法
     *
     * @param allFieldsAndMethodWithInherit 所有的字段和方法，可能在父类中
     */
    public boolean containsNotPrivateSetter(final VariableElement variableElement, List<? extends Element> allFieldsAndMethodWithInherit) {
        return BeanUtils.containsNotPrivateSetter(typeUtils, variableElement, allFieldsAndMethodWithInherit);
    }

    /**
     * 是否是可序列化的字段
     */
    public boolean isSerializableField(VariableElement variableElement) {
        return isSerializableField(typeUtils, elementUtils, variableElement, anno_ignoreTypeElement.asType());
    }

    public static boolean isSerializableField(Types typeUtils, Elements elementUtils, VariableElement variableElement, TypeMirror anno_ignoreMirror) {
        if (variableElement.getModifiers().contains(Modifier.STATIC)) {
            return false;
        }
        // 有注解的情况下，取决于注解的值
        AnnotationMirror ignoreMirror = AptUtils.findAnnotation(typeUtils, variableElement, anno_ignoreMirror)
                .orElse(null);
        if (ignoreMirror != null) {
            Boolean ignore = AptUtils.getAnnotationValueValueWithDefaults(elementUtils, ignoreMirror, "value");
            return ignore != Boolean.TRUE;
        }
        // 无注解的情况下，默认忽略 transient 字段
        return !variableElement.getModifiers().contains(Modifier.TRANSIENT);
    }

    /** 是否是托管写的字段 */
    public boolean isAutoWriteField(VariableElement variableElement, AptClassImpl aptClassImpl, AptFieldImpl aptFieldImpl) {
        // 优先判断skip属性
        if (aptClassImpl.skipFields.contains(variableElement.getSimpleName().toString())) {
            return false;
        }
        // 写代理 -- 自行写，或指向空表示不自动写
        if (aptFieldImpl.isDeclaredWriteProxy()) {
            return aptFieldImpl.hasWriteProxy();
        }
        return true;
    }

    /** 是否是托管写的字段 */
    public boolean isAutoReadField(VariableElement variableElement, AptClassImpl aptClassImpl, AptFieldImpl aptFieldImpl) {
        // final必定或构造方法读
        if (variableElement.getModifiers().contains(Modifier.FINAL)) {
            return false;
        }
        // 优先判断skip属性
        if (aptClassImpl.skipFields.contains(variableElement.getSimpleName().toString())) {
            return false;
        }
        // 读代理 -- 自行读，或指向空表示不自动读
        if (aptFieldImpl.isDeclaredReadProxy()) {
            return aptFieldImpl.hasReadProxy();
        }
        return true;
    }

    // endregion

    // region

    public MethodSpec newGetEncoderClassMethod(DeclaredType superDeclaredType, TypeName rawTypeName) {
        return MethodSpec.overriding(getEncoderClassMethod, superDeclaredType, typeUtils)
                .addStatement("return $T.class", rawTypeName)
                .addAnnotation(AptUtils.NONNULL_ANNOTATION)
                .build();
    }

    public MethodSpec.Builder newWriteObjectMethodBuilder(DeclaredType superDeclaredType) {
        return MethodSpec.overriding(writeObjectMethod, superDeclaredType, typeUtils);
    }

    public MethodSpec.Builder newReadObjectMethodBuilder(DeclaredType superDeclaredType) {
        return MethodSpec.overriding(readObjectMethod, superDeclaredType, typeUtils);
    }

    public MethodSpec.Builder newNewInstanceMethodBuilder(DeclaredType superDeclaredType) {
        return MethodSpec.overriding(newInstanceMethod, superDeclaredType, typeUtils);
    }

    public MethodSpec.Builder newReadFieldsMethodBuilder(DeclaredType superDeclaredType) {
        return MethodSpec.overriding(readFieldsMethod, superDeclaredType, typeUtils);
    }

    public MethodSpec.Builder newAfterDecodeMethodBuilder(DeclaredType superDeclaredType) {
        return MethodSpec.overriding(afterDecodeMethod, superDeclaredType, typeUtils);
    }

    public List<AnnotationSpec> getAdditionalAnnotations(TypeElement typeElement) {
        AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, typeElement, anno_serializableTypeElement.asType())
                .orElseThrow();

        final List<? extends AnnotationValue> annotationsList = AptUtils.getAnnotationValueValue(annotationMirror, "annotations");
        if (annotationsList == null || annotationsList.isEmpty()) {
            return List.of();
        }

        List<AnnotationSpec> result = new ArrayList<>(annotationsList.size());
        for (final AnnotationValue annotationValue : annotationsList) {
            final TypeMirror typeMirror = AptUtils.getAnnotationValueTypeMirror(annotationValue);
            result.add(AnnotationSpec.builder((ClassName) ClassName.get(typeMirror))
                    .build());
        }
        return result;
    }

    // endregion
}