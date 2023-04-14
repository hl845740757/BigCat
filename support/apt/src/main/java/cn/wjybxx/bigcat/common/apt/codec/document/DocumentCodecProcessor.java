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

package cn.wjybxx.bigcat.common.apt.codec.document;

import cn.wjybxx.bigcat.common.apt.AptUtils;
import cn.wjybxx.bigcat.common.apt.BeanUtils;
import cn.wjybxx.bigcat.common.apt.codec.*;
import com.google.auto.service.AutoService;
import com.squareup.javapoet.AnnotationSpec;
import com.squareup.javapoet.MethodSpec;
import com.squareup.javapoet.TypeName;

import javax.annotation.processing.Processor;
import javax.annotation.processing.RoundEnvironment;
import javax.lang.model.element.*;
import javax.lang.model.type.DeclaredType;
import javax.lang.model.util.Elements;
import javax.tools.Diagnostic;
import java.util.List;
import java.util.Set;

/**
 * @author wjybxx
 * date 2023/4/13
 */
@AutoService(Processor.class)
public class DocumentCodecProcessor extends CodecProcessor {

    // 使用这种方式可以脱离对utils，net包的依赖
    private static final String PERSISTENT_ENTITY_CANONICAL_NAME = "cn.wjybxx.bigcat.common.codec.document.DocumentSerializable";
    private static final String PERSISTENT_IGNORE_CANONICAL_NAME = "cn.wjybxx.bigcat.common.codec.document.DocumentIgnore";
    private static final String PNAME_TYPE_ALIAS = "typeAlias";

    private static final String READER_CANONICAL_NAME = "cn.wjybxx.bigcat.common.codec.document.DocumentReader";
    private static final String WRITER_CANONICAL_NAME = "cn.wjybxx.bigcat.common.codec.document.DocumentWriter";
    private static final String CODEC_CANONICAL_NAME = "cn.wjybxx.bigcat.common.codec.document.DocumentPojoCodecImpl";
    private static final String ABSTRACT_CODEC_CANONICAL_NAME = "cn.wjybxx.bigcat.common.codec.document.AbstractDocumentPojoCodecImpl";
    private static final String MNAME_GET_TYPE_NAME = "getTypeName";

    private TypeElement anno_documentTypeName;
    private TypeElement anno_ignoreTypeName;

    TypeElement readerTypeElement;
    TypeElement writerTypeElement;
    TypeElement codecTypeElement;

    // 要覆盖的方法缓存，减少大量查询
    private ExecutableElement getTypeNameMethod;
    private ExecutableElement getEncoderClassMethod;
    private ExecutableElement writeObjectMethod;
    private ExecutableElement readObjectMethod;

    TypeElement abstractCodecTypeElement;
    ExecutableElement newInstanceMethod;
    ExecutableElement readFieldsMethod;
    ExecutableElement afterDecodeMethod;

    private TypeElement autoFieldsTypeElement;
    private TypeElement autoArgsTypeElement;

    @Override
    protected void ensureInited() {
        super.ensureInited();
        if (anno_documentTypeName != null) {
            return;
        }
        anno_documentTypeName = elementUtils.getTypeElement(PERSISTENT_ENTITY_CANONICAL_NAME);
        anno_ignoreTypeName = elementUtils.getTypeElement(PERSISTENT_IGNORE_CANONICAL_NAME);
        readerTypeElement = elementUtils.getTypeElement(READER_CANONICAL_NAME);
        writerTypeElement = elementUtils.getTypeElement(WRITER_CANONICAL_NAME);

        codecTypeElement = elementUtils.getTypeElement(CODEC_CANONICAL_NAME);
        getTypeNameMethod = AptUtils.findMethodByName(codecTypeElement, MNAME_GET_TYPE_NAME);
        getEncoderClassMethod = AptUtils.findMethodByName(codecTypeElement, MNAME_GET_ENCODER_CLASS);
        writeObjectMethod = AptUtils.findMethodByName(codecTypeElement, MNAME_WRITE_OBJECT);
        readObjectMethod = AptUtils.findMethodByName(codecTypeElement, MNAME_READ_OBJECT);

        abstractCodecTypeElement = elementUtils.getTypeElement(ABSTRACT_CODEC_CANONICAL_NAME);
        newInstanceMethod = AptUtils.findMethodByName(abstractCodecTypeElement, MNAME_NEW_INSTANCE);
        readFieldsMethod = AptUtils.findMethodByName(abstractCodecTypeElement, MNAME_READ_FIELDS);
        afterDecodeMethod = AptUtils.findMethodByName(abstractCodecTypeElement, MNAME_AFTER_DECODE);

        autoFieldsTypeElement = elementUtils.getTypeElement(AutoFieldsProcessor.AUTO_CANONICAL_NAME);
        autoArgsTypeElement = elementUtils.getTypeElement(AutoTypeArgsProcessor.AUTO_CANONICAL_NAME);
    }

    @Override
    public Set<String> getSupportedAnnotationTypes() {
        return Set.of(PERSISTENT_ENTITY_CANONICAL_NAME);
    }

    /**
     * 如果保留策略修改为runtime，则需要调用进行过滤。
     * {@link AptUtils#selectSourceFile(Set, Elements)}
     */
    @Override
    protected boolean doProcess(Set<? extends TypeElement> annotations, RoundEnvironment roundEnv) {
        // 该注解只有类可以使用
        @SuppressWarnings("unchecked") final Set<TypeElement> allTypeElements = (Set<TypeElement>) roundEnv.getElementsAnnotatedWith(anno_documentTypeName);
        for (TypeElement typeElement : allTypeElements) {
            try {
                checkBase(typeElement);
                generateSerializer(typeElement);
            } catch (Throwable e) {
                messager.printMessage(Diagnostic.Kind.ERROR, AptUtils.getStackTrace(e), typeElement);
            }
        }
        return true;
    }

    /**
     * 基础信息检查
     */
    private void checkBase(TypeElement typeElement) {
        if (!isClassOrEnum(typeElement)) {
            messager.printMessage(Diagnostic.Kind.ERROR, "unsupported type", typeElement);
            return;
        }
        if (typeElement.getKind() == ElementKind.ENUM) {
            checkEnum(typeElement);
            return;
        }
        // 检查普通类 -- 普通类也可以实现IndexableEnum
        if (isIndexableEnum(typeElement.asType())) {
            checkIndexableEnum(typeElement);
        } else {
            checkNormalClass(typeElement);
        }
    }

    protected void checkNormalClass(TypeElement typeElement) {
        checkAutoFields(typeElement);
        checkAutoArgs(typeElement);
        checkConstructor(typeElement);

        final List<? extends Element> allFieldsAndMethodWithInherit = BeanUtils.getAllFieldsAndMethodsWithInherit(typeElement);
        final Set<String> skipFields = getSkipFields(typeElement);
        final boolean containsReaderConstructor = containsReaderConstructor(typeElement);
        final boolean containsReadObjectMethod = containsReadObjectMethod(allFieldsAndMethodWithInherit);
        final boolean containsWriteObjectMethod = containsWriteObjectMethod(allFieldsAndMethodWithInherit);
        final ClassImplProperties classImplProperties = parseClassImpl(typeElement);

        for (Element element : allFieldsAndMethodWithInherit) {
            if (element.getKind() != ElementKind.FIELD) {
                continue;
            }
            final VariableElement variableElement = (VariableElement) element;
            if (!isSerializableField(skipFields, variableElement)) {
                continue;
            }

            FieldImplProperties fieldImplProperties = parseFiledImpl(variableElement);
            if (isAutoWriteField(variableElement, containsWriteObjectMethod, classImplProperties, fieldImplProperties)) {
                if (hasWriteProxy(variableElement, fieldImplProperties)) {
                    continue;
                }
                // 工具写：需要提供可直接取值或包含非private的getter方法
                if (!canGetDirectly(variableElement, typeElement) && !containsNotPrivateGetter(variableElement, allFieldsAndMethodWithInherit)) {
                    messager.printMessage(Diagnostic.Kind.ERROR,
                            String.format("serializable field (%s) must contains a not private getter or canGetDirectly", variableElement.getSimpleName().toString()),
                            variableElement);
                    continue;
                }
            }
            if (isAutoReadField(variableElement, containsReaderConstructor, containsReadObjectMethod, classImplProperties, fieldImplProperties)) {
                if (hasReadProxy(variableElement, fieldImplProperties)) {
                    continue;
                }
                // 工具读：需要提供可直接赋值或非private的setter方法
                if (!canSetDirectly(variableElement, typeElement) && !containsNotPrivateSetter(variableElement, allFieldsAndMethodWithInherit)) {
                    messager.printMessage(Diagnostic.Kind.ERROR,
                            String.format("serializable field (%s) must contains a not private setter or canSetDirectly", variableElement.getSimpleName().toString()),
                            variableElement);
                    continue;
                }
            }
        }
    }

    void checkConstructor(TypeElement typeElement) {
        super.checkConstructor(typeElement, readerTypeElement);
    }

    boolean containsReaderConstructor(TypeElement typeElement) {
        return super.containsReaderConstructor(typeElement, readerTypeElement);
    }

    boolean containsReadObjectMethod(List<? extends Element> allFieldsAndMethodWithInherit) {
        return super.containsReadObjectMethod(allFieldsAndMethodWithInherit, readerTypeElement);
    }

    boolean containsWriteObjectMethod(List<? extends Element> allFieldsAndMethodWithInherit) {
        return super.containsWriteObjectMethod(allFieldsAndMethodWithInherit, writerTypeElement);
    }

    private void checkAutoFields(TypeElement typeElement) {
        final AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, typeElement, autoFieldsTypeElement.asType())
                .orElse(null);
        if (annotationMirror == null) {
            messager.printMessage(Diagnostic.Kind.ERROR, "DocumentSerializable must contains AutoFields annotation", typeElement);
        }
    }

    private void checkAutoArgs(TypeElement typeElement) {
        final AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, typeElement, autoArgsTypeElement.asType())
                .orElse(null);
        if (annotationMirror == null) {
            messager.printMessage(Diagnostic.Kind.ERROR, "DocumentSerializable must contains AutoTypeArgs annotation", typeElement);
        }
    }

    boolean isSerializableField(Set<String> skipFields, VariableElement variableElement) {
        return super.isSerializableField(skipFields, variableElement, anno_ignoreTypeName);
    }

    private String getTypeAlias(TypeElement typeElement) {
        final AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, typeElement, anno_documentTypeName.asType())
                .orElseThrow();
        final String value = AptUtils.getAnnotationValueValue(annotationMirror, PNAME_TYPE_ALIAS);
        if (null != value && !value.isBlank()) {
            return value.trim();
        } else {
            return AptUtils.getProxyClassName(elementUtils, typeElement, "");
        }
    }

    // ----------------------------------------------- 辅助类生成 -------------------------------------------

    private void generateSerializer(TypeElement typeElement) {
        if (isIndexableEnum(typeElement.asType())) {
            new EnumDocumentCodecGenerator(this, typeElement).execute();
        } else {
            new PojoDocumentCodecGenerator(this, typeElement).execute();
        }
    }

    /**
     * 创建getTypeAlias方法
     */
    MethodSpec newGetTypeNameMethod(DeclaredType superDeclaredType, TypeElement typeElement) {
        return MethodSpec.overriding(getTypeNameMethod, superDeclaredType, typeUtils)
                .addStatement("return $S", getTypeAlias(typeElement))
                .addAnnotation(AptUtils.NONNULL_ANNOTATION)
                .build();
    }

    /**
     * 创建返回负责被序列化的类对象的方法
     */
    MethodSpec newGetEncoderClassMethod(DeclaredType superDeclaredType) {
        return MethodSpec.overriding(getEncoderClassMethod, superDeclaredType, typeUtils)
                .addStatement("return $T.class", TypeName.get(superDeclaredType.getTypeArguments().get(0)))
                .addAnnotation(AptUtils.NONNULL_ANNOTATION)
                .build();
    }

    /**
     * 创建writeObject方法
     */
    MethodSpec.Builder newWriteObjectMethodBuilder(DeclaredType superDeclaredType) {
        return MethodSpec.overriding(writeObjectMethod, superDeclaredType, typeUtils);
    }

    /**
     * 创建readObject方法
     */
    MethodSpec.Builder newReadObjectMethodBuilder(DeclaredType superDeclaredType) {
        return MethodSpec.overriding(readObjectMethod, superDeclaredType, typeUtils);
    }

    Set<String> getSkipFields(TypeElement typeElement) {
        return super.getSkipFields(typeElement, anno_documentTypeName);
    }

    List<AnnotationSpec> getAdditionalAnnotations(TypeElement typeElement) {
        return super.getAdditionalAnnotations(typeElement, anno_documentTypeName);
    }

    /**
     * 获取class对应的序列化工具类的类名
     */
    static String getCodecClassName(TypeElement typeElement, Elements elementUtils) {
        if (typeElement.getEnclosingElement().getKind() == ElementKind.PACKAGE) {
            return typeElement.getSimpleName().toString() + "DocCodec";
        } else {
            // 内部类，避免与其它的内部类冲突，不能使用简单名
            final String packageName = elementUtils.getPackageOf(typeElement).getQualifiedName().toString();
            final String fullName = typeElement.getQualifiedName().toString();
            final String uniqueName = fullName.substring(packageName.length() + 1).replace(".", "_");
            return uniqueName + "DocumentCodec";
        }
    }
}