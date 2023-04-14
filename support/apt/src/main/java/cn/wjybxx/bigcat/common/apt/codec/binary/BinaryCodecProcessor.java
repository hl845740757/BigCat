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

package cn.wjybxx.bigcat.common.apt.codec.binary;

import cn.wjybxx.bigcat.common.apt.AptUtils;
import cn.wjybxx.bigcat.common.apt.BeanUtils;
import cn.wjybxx.bigcat.common.apt.codec.AutoTypeArgsProcessor;
import cn.wjybxx.bigcat.common.apt.codec.ClassImplProperties;
import cn.wjybxx.bigcat.common.apt.codec.CodecProcessor;
import cn.wjybxx.bigcat.common.apt.codec.FieldImplProperties;
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
public class BinaryCodecProcessor extends CodecProcessor {

    private static final String SERIALIZABLE_CLASS_CANONICAL_NAME = "cn.wjybxx.bigcat.common.codec.binary.BinarySerializable";
    private static final String SERIALIZE_IGNORE_CANONICAL_NAME = "cn.wjybxx.bigcat.common.codec.binary.BinaryIgnore";
    private static final String OBJECT_READER_CANONICAL_NAME = "cn.wjybxx.bigcat.common.codec.binary.BinaryReader";
    private static final String OBJECT_WRITER_CANONICAL_NAME = "cn.wjybxx.bigcat.common.codec.binary.BinaryWriter";

    private static final String CODEC_CANONICAL_NAME = "cn.wjybxx.bigcat.common.codec.binary.BinaryPojoCodecImpl";
    private static final String ABSTRACT_CODEC_CANONICAL_NAME = "cn.wjybxx.bigcat.common.codec.binary.AbstractBinaryPojoCodecImpl";

    private TypeElement anno_serializableTypeElement;
    private TypeElement anno_ignoreTypeElement;

    TypeElement readerTypeElement;
    TypeElement writerTypeElement;
    TypeElement codecTypeElement;

    // 要覆盖的方法缓存
    private ExecutableElement getEncoderClassMethod;
    private ExecutableElement writeObjectMethod;
    private ExecutableElement readObjectMethod;

    TypeElement abstractCodecTypeElement;
    ExecutableElement newInstanceMethod;
    ExecutableElement readFieldsMethod;
    ExecutableElement afterDecodeMethod;

    private TypeElement autoArgsTypeElement;

    @Override
    public Set<String> getSupportedAnnotationTypes() {
        return Set.of(SERIALIZABLE_CLASS_CANONICAL_NAME);
    }

    @Override
    protected void ensureInited() {
        super.ensureInited();
        if (anno_serializableTypeElement != null) {
            return;
        }
        anno_serializableTypeElement = elementUtils.getTypeElement(SERIALIZABLE_CLASS_CANONICAL_NAME);
        anno_ignoreTypeElement = elementUtils.getTypeElement(SERIALIZE_IGNORE_CANONICAL_NAME);
        readerTypeElement = elementUtils.getTypeElement(OBJECT_READER_CANONICAL_NAME);
        writerTypeElement = elementUtils.getTypeElement(OBJECT_WRITER_CANONICAL_NAME);

        codecTypeElement = elementUtils.getTypeElement(CODEC_CANONICAL_NAME);
        getEncoderClassMethod = AptUtils.findMethodByName(codecTypeElement, MNAME_GET_ENCODER_CLASS);
        writeObjectMethod = AptUtils.findMethodByName(codecTypeElement, MNAME_WRITE_OBJECT);
        readObjectMethod = AptUtils.findMethodByName(codecTypeElement, MNAME_READ_OBJECT);

        abstractCodecTypeElement = elementUtils.getTypeElement(ABSTRACT_CODEC_CANONICAL_NAME);
        newInstanceMethod = AptUtils.findMethodByName(abstractCodecTypeElement, MNAME_NEW_INSTANCE);
        readFieldsMethod = AptUtils.findMethodByName(abstractCodecTypeElement, MNAME_READ_FIELDS);
        afterDecodeMethod = AptUtils.findMethodByName(abstractCodecTypeElement, MNAME_AFTER_DECODE);

        autoArgsTypeElement = elementUtils.getTypeElement(AutoTypeArgsProcessor.AUTO_CANONICAL_NAME);
    }

    @Override
    protected boolean doProcess(Set<? extends TypeElement> annotations, RoundEnvironment roundEnv) {
        @SuppressWarnings("unchecked") final Set<TypeElement> allTypeElements = (Set<TypeElement>) roundEnv.getElementsAnnotatedWith(anno_serializableTypeElement);
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
        // 枚举
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

    void checkAutoArgs(TypeElement typeElement) {
        final AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, typeElement, autoArgsTypeElement.asType())
                .orElse(null);
        if (annotationMirror == null) {
            messager.printMessage(Diagnostic.Kind.ERROR, "BinarySerializable must contains AutoTypeArgs annotation", typeElement);
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

    boolean isSerializableField(Set<String> skipFields, VariableElement variableElement) {
        return super.isSerializableField(skipFields, variableElement, anno_ignoreTypeElement);
    }

    // ----------------------------------------------- 辅助类生成 -------------------------------------------

    private void generateSerializer(TypeElement typeElement) {
        if (isIndexableEnum(typeElement.asType())) {
            new EnumBinCodecGenerator(this, typeElement).execute();
        } else {
            new PojoBinCodecGenerator(this, typeElement).execute();
        }
    }

    /** @param superDeclaredType 用于重写方法 */
    MethodSpec.Builder newWriteObjectMethodBuilder(DeclaredType superDeclaredType) {
        return MethodSpec.overriding(writeObjectMethod, superDeclaredType, typeUtils);
    }

    MethodSpec.Builder newReadObjectMethodBuilder(DeclaredType superDeclaredType) {
        return MethodSpec.overriding(readObjectMethod, superDeclaredType, typeUtils);
    }

    MethodSpec newGetEncoderClassMethod(DeclaredType superDeclaredType, TypeName rawTypeName) {
        return MethodSpec.overriding(getEncoderClassMethod, superDeclaredType, typeUtils)
                .addStatement("return $T.class", rawTypeName)
                .addAnnotation(AptUtils.NONNULL_ANNOTATION)
                .build();
    }

    Set<String> getSkipFields(TypeElement typeElement) {
        return super.getSkipFields(typeElement, anno_serializableTypeElement);
    }

    List<AnnotationSpec> getAdditionalAnnotations(TypeElement typeElement) {
        return super.getAdditionalAnnotations(typeElement, anno_serializableTypeElement);
    }

    static String getCodecClassName(TypeElement typeElement, Elements elementUtils) {
        return AptUtils.getProxyClassName(elementUtils, typeElement, "BinCodec");
    }

}