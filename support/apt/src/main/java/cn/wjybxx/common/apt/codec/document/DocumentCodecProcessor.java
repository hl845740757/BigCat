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

package cn.wjybxx.common.apt.codec.document;

import cn.wjybxx.common.apt.AptUtils;
import cn.wjybxx.common.apt.codec.CodecProcessor;
import com.google.auto.service.AutoService;
import com.squareup.javapoet.MethodSpec;

import javax.annotation.processing.Processor;
import javax.annotation.processing.RoundEnvironment;
import javax.lang.model.element.AnnotationMirror;
import javax.lang.model.element.ElementKind;
import javax.lang.model.element.ExecutableElement;
import javax.lang.model.element.TypeElement;
import javax.lang.model.type.DeclaredType;
import javax.lang.model.util.Elements;
import javax.tools.Diagnostic;
import java.util.Set;

/**
 * @author wjybxx
 * date 2023/4/13
 */
@AutoService(Processor.class)
public class DocumentCodecProcessor extends CodecProcessor {

    private static final String CNAME_SERIALIZABLE = "cn.wjybxx.common.dson.document.DocumentSerializable";
    private static final String CNAME_IGNORE = "cn.wjybxx.common.dson.document.DocumentIgnore";
    private static final String PNAME_TYPE_ALIAS = "typeAlias";

    private static final String CNAME_READER = "cn.wjybxx.common.dson.document.DocumentObjectReader";
    private static final String CNAME_WRITER = "cn.wjybxx.common.dson.document.DocumentObjectWriter";
    private static final String CNAME_CODEC = "cn.wjybxx.common.dson.document.DocumentPojoCodecImpl";

    private static final String CNAME_ABSTRACT_CODEC = "cn.wjybxx.common.dson.document.AbstractDocumentPojoCodecImpl";
    private static final String MNAME_GET_TYPE_NAME = "getTypeName";
    private static final String CNAME_ENUM_CODEC = "cn.wjybxx.common.dson.document.codecs.DsonEnumCodec";

    // 要覆盖的方法缓存，减少大量查询
    private ExecutableElement getTypeNameMethod;

    public DocumentCodecProcessor() {
        super();
    }

    @Override
    protected void ensureInited() {
        if (anno_serializableTypeElement != null) {
            return;
        }
        super.ensureInited(CNAME_SERIALIZABLE, CNAME_IGNORE,
                CNAME_READER, CNAME_WRITER,
                CNAME_CODEC, CNAME_ABSTRACT_CODEC,
                CNAME_ENUM_CODEC);

        getTypeNameMethod = AptUtils.findMethodByName(codecTypeElement, MNAME_GET_TYPE_NAME);
    }

    @Override
    public Set<String> getSupportedAnnotationTypes() {
        return Set.of(CNAME_SERIALIZABLE);
    }

    @Override
    protected boolean doProcess(Set<? extends TypeElement> annotations, RoundEnvironment roundEnv) {
        final Set<TypeElement> allTypeElements = (Set<TypeElement>) roundEnv.getElementsAnnotatedWith(anno_serializableTypeElement);
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
        // 检查普通类
        if (isDsonEnum(typeElement.asType())) {
            checkForNumberMethod(typeElement);
        } else {
            checkNormalClass(typeElement);
        }
    }

    @Override
    protected void checkNormalClass(TypeElement typeElement) {
        checkAutoFields(typeElement);
        super.checkNormalClass(typeElement);
    }

    private void checkAutoFields(TypeElement typeElement) {
        final AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, typeElement, autoFieldsTypeElement.asType())
                .orElse(null);
        if (annotationMirror == null) {
            messager.printMessage(Diagnostic.Kind.ERROR, "DocumentSerializable must contains AutoFields annotation", typeElement);
        }
    }

    protected String getTypeAlias(TypeElement typeElement) {
        final AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, typeElement, anno_serializableTypeElement.asType())
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
        if (isDsonEnum(typeElement.asType())) {
            new EnumDocCodecGenerator(this, typeElement).execute();
        } else {
            new PojoDocCodecGenerator(this, typeElement).execute();
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

    static String getCodecClassName(TypeElement typeElement, Elements elementUtils) {
        return AptUtils.getProxyClassName(elementUtils, typeElement, "DocCodec");
    }
}