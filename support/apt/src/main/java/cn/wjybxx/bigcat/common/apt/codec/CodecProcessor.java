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

package cn.wjybxx.bigcat.common.apt.codec;

import cn.wjybxx.bigcat.common.apt.AptUtils;
import cn.wjybxx.bigcat.common.apt.BeanUtils;
import cn.wjybxx.bigcat.common.apt.MyAbstractProcessor;
import com.squareup.javapoet.AnnotationSpec;
import com.squareup.javapoet.ClassName;

import javax.lang.model.element.*;
import javax.lang.model.type.ArrayType;
import javax.lang.model.type.TypeKind;
import javax.lang.model.type.TypeMirror;
import javax.tools.Diagnostic;
import java.util.ArrayList;
import java.util.List;
import java.util.Set;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date 2023/4/13
 */
public abstract class CodecProcessor extends MyAbstractProcessor {

    public static final String FIELD_IMPL_CANONICAL_NAME = "cn.wjybxx.bigcat.common.codec.FieldImpl";
    public static final String CLASS_IMPL_CANONICAL_NAME = "cn.wjybxx.bigcat.common.codec.ClassImpl";

    // CodecImpl
    public static final String MNAME_GET_ENCODER_CLASS = "getEncoderClass";
    public static final String MNAME_WRITE_OBJECT = "writeObject"; // 同时也是自定义写方法
    public static final String MNAME_READ_OBJECT = "readObject";
    // AbstractCodecImpl
    public static final String MNAME_NEW_INSTANCE = "newInstance";
    public static final String MNAME_READ_FIELDS = "readFields";
    public static final String MNAME_AFTER_DECODE = "afterDecode";

    public static final String INDEXABLE_ENUM_CANONICAL_NAME = "cn.wjybxx.bigcat.common.IndexableEnum";
    public static final String MNAME_FOR_NUMBER = "forNumber";
    public static final String MNAME_GET_NUMBER = "getNumber";

    public TypeMirror stringTypeMirror;
    public TypeMirror enumTypeMirror;

    public TypeMirror anno_fieldImplTypeMirror;
    public TypeMirror anno_classImplTypeMirror;
    public TypeMirror indexableEnumTypeMirror;

    @Override
    protected void ensureInited() {
        if (stringTypeMirror != null) {
            return;
        }
        stringTypeMirror = elementUtils.getTypeElement(String.class.getCanonicalName()).asType();
        enumTypeMirror = elementUtils.getTypeElement(Enum.class.getCanonicalName()).asType();

        anno_fieldImplTypeMirror = elementUtils.getTypeElement(FIELD_IMPL_CANONICAL_NAME).asType();
        anno_classImplTypeMirror = elementUtils.getTypeElement(CLASS_IMPL_CANONICAL_NAME).asType();
        indexableEnumTypeMirror = elementUtils.getTypeElement(INDEXABLE_ENUM_CANONICAL_NAME).asType();
    }

    public boolean isClassOrEnum(TypeElement typeElement) {
        return typeElement.getKind() == ElementKind.CLASS
                || typeElement.getKind() == ElementKind.ENUM;
    }

    public boolean isString(TypeMirror typeMirror) {
        return typeUtils.isSameType(typeMirror, stringTypeMirror);
    }

    public boolean isByteArray(TypeMirror typeMirror) {
        return typeMirror.getKind() == TypeKind.ARRAY &&
                ((ArrayType) typeMirror).getComponentType().getKind() == TypeKind.BYTE;
    }

    protected boolean isEnum(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, enumTypeMirror);
    }

    protected boolean isIndexableEnum(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, indexableEnumTypeMirror);
    }

    //

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

    // 枚举公共检查
    // 我们对枚举的要求是一致的

    /**
     * 检查枚举 - 要序列化的枚举，必须实现 indexableEnum 接口，否则无法序列化，或自己手写serializer。
     */
    public void checkEnum(TypeElement typeElement) {
        if (!isIndexableEnum(typeElement.asType())) {
            messager.printMessage(Diagnostic.Kind.ERROR,
                    "serializable enum must implement IndexableEnum",
                    typeElement);
            return;
        }
        checkIndexableEnum(typeElement);
    }

    /**
     * 检查 indexableEnum 的子类是否有静态的forNumber方法
     */
    protected void checkIndexableEnum(TypeElement typeElement) {
        if (!containNotPrivateStaticForNumberMethod(typeElement)) {
            messager.printMessage(Diagnostic.Kind.ERROR,
                    "serializable enum contains a not private 'static T forNumber(int)' method!",
                    typeElement);
        }
    }

    /**
     * 是否包含静态的非private的forNumber方法
     * <pre>{@code
     * enum MyEnum implements IndexableEnum {
     *
     *      static MyEnum forNumber(int) {
     *
     *      }
     * }
     * }</pre>
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

    // 普通类检查

    /** 检查是否包含无参构造方法或解析构造方法 */
    protected void checkConstructor(TypeElement typeElement, TypeElement readerTypeElement) {
        if (typeElement.getModifiers().contains(Modifier.ABSTRACT)) {
            return;
        }
        if (BeanUtils.containsNoArgsConstructor(typeElement)
                || containsReaderConstructor(typeElement, readerTypeElement)) {
            return;
        }
        String typeName = typeElement.getSimpleName().toString();
        String readerName = readerTypeElement.getSimpleName().toString();
        messager.printMessage(Diagnostic.Kind.ERROR,
                String.format("SerializableClass %s must contains no-args constructor or %s-args constructor!", typeName, readerName),
                typeElement);
    }

    /** 是否包含 T(Reader reader) 构造方法 */
    protected boolean containsReaderConstructor(TypeElement typeElement, TypeElement readerTypeElement) {
        return BeanUtils.containsOneArgsConstructor(typeUtils, typeElement, readerTypeElement.asType());
    }

    /** 是否包含 readerObject 实例方法 */
    protected boolean containsReadObjectMethod(List<? extends Element> allFieldsAndMethodWithInherit, TypeElement readerTypeElement) {
        return allFieldsAndMethodWithInherit.stream()
                .filter(e -> e.getKind() == ElementKind.METHOD)
                .map(e -> (ExecutableElement) e)
                .filter(e -> !e.getModifiers().contains(Modifier.PRIVATE) && !e.getModifiers().contains(Modifier.STATIC))
                .filter(e -> e.getParameters().size() == 1)
                .filter(e -> e.getSimpleName().toString().equals(MNAME_READ_OBJECT))
                .anyMatch(e -> AptUtils.isSameTypeIgnoreTypeParameter(typeUtils, e.getParameters().get(0).asType(), readerTypeElement.asType()));
    }

    /** 是否包含 writeObject 实例方法 */
    protected boolean containsWriteObjectMethod(List<? extends Element> allFieldsAndMethodWithInherit, TypeElement writerTypeElement) {
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

    public ClassImplProperties parseClassImpl(TypeElement typeElement) {
        return ClassImplProperties.parse(typeUtils, typeElement, anno_classImplTypeMirror);
    }

    public FieldImplProperties parseFiledImpl(VariableElement variableElement) {
        return FieldImplProperties.parse(typeUtils, variableElement, anno_fieldImplTypeMirror);
    }

    /**
     * 是否是可序列化的字段
     *
     * @param skipFields        跳过的字段
     * @param ignoreTypeElement 用户忽略或不忽略的注解
     */
    protected boolean isSerializableField(Set<String> skipFields, VariableElement variableElement, TypeElement ignoreTypeElement) {
        if (variableElement.getModifiers().contains(Modifier.STATIC)) {
            return false;
        }
        if (skipFields.contains(variableElement.getSimpleName().toString())) {
            return false;
        }
        // 有注解的情况下，取决于注解的值
        AnnotationMirror ignoreMirror = AptUtils.findAnnotation(typeUtils, variableElement, ignoreTypeElement.asType())
                .orElse(null);
        if (ignoreMirror != null) {
            Boolean ignore = AptUtils.getAnnotationValueValueWithDefaults(elementUtils, ignoreMirror, "value");
            return ignore != Boolean.TRUE;
        }

        // 无注解的情况下，默认忽略 transient 字段
        return !variableElement.getModifiers().contains(Modifier.TRANSIENT);
    }

    /** 是否是托管写的字段 */
    public boolean isAutoWriteField(VariableElement variableElement, boolean containsWriterMethod,
                                    ClassImplProperties classImplProperties, FieldImplProperties fieldImplProperties) {
        // 写代理 -- 自行写
        if (hasWriteProxy(variableElement, fieldImplProperties)) {
            return true;
        }
        // 不包含writeObject方法，全部托管
        if (!containsWriterMethod) {
            return true;
        }
        // 处理final字段
        if (variableElement.getModifiers().contains(Modifier.FINAL)) {
            return classImplProperties.autoWriteFinalFields;
        } else {
            return classImplProperties.autoWriteNonFinalFields;
        }
    }

    /** 是否是托管写的字段 */
    public boolean isAutoReadField(VariableElement variableElement, boolean containsReaderConstructor, boolean containsReadObjectMethod,
                                   ClassImplProperties classImplProperties, FieldImplProperties fieldImplProperties) {
        // final必定或构造方法读
        if (variableElement.getModifiers().contains(Modifier.FINAL)) {
            return false;
        }
        // 读代理 -- 自行读
        if (hasReadProxy(variableElement, fieldImplProperties)) {
            return true;
        }
        // 不包含解析构造方法，全部托管
        if (!containsReaderConstructor && !containsReadObjectMethod) {
            return true;
        }
        return classImplProperties.autoReadNonFinalFields;
    }

    public boolean hasWriteProxy(VariableElement variableElement, FieldImplProperties fieldImplProperties) {
        return fieldImplProperties.isAnnotationPresent && fieldImplProperties.hasWriteProxy();
    }

    public boolean hasReadProxy(VariableElement variableElement, FieldImplProperties fieldImplProperties) {
        return fieldImplProperties.isAnnotationPresent && fieldImplProperties.hasReadProxy();
    }

    //

    public Set<String> getSkipFields(TypeElement typeElement, TypeElement annotationTypeElement) {
        AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, typeElement, annotationTypeElement.asType())
                .orElseThrow();

        final List<? extends AnnotationValue> annotationsList = AptUtils.getAnnotationValueValue(annotationMirror, "skipFields");
        if (annotationsList == null || annotationsList.isEmpty()) {
            return Set.of();
        }
        return annotationsList.stream()
                .map(e -> (String) e.getValue())
                .collect(Collectors.toSet());

    }

    protected List<AnnotationSpec> getAdditionalAnnotations(TypeElement typeElement, TypeElement annotationTypeElement) {
        AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, typeElement, annotationTypeElement.asType())
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
}