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
import com.google.auto.service.AutoService;
import com.squareup.javapoet.*;

import javax.annotation.processing.Processor;
import javax.annotation.processing.RoundEnvironment;
import javax.lang.model.element.*;
import javax.lang.model.type.DeclaredType;
import javax.lang.model.type.TypeKind;
import javax.lang.model.type.TypeMirror;
import javax.tools.Diagnostic;
import java.util.*;

/**
 * @author wjybxx
 * date 2023/4/13
 */
@AutoService(Processor.class)
public class CodecProcessor extends MyAbstractProcessor {

    // region 常量
    public static final String CNAME_FIELD_IMPL = "cn.wjybxx.common.codec.FieldImpl";
    public static final String CNAME_CLASS_IMPL = "cn.wjybxx.common.codec.ClassImpl";
    public static final String CNAME_TYPEARG = "cn.wjybxx.common.codec.TypeArgInfo";
    public static final String CNAME_WireType = "cn.wjybxx.dson.WireType";
    public static final String CNAME_NumberStyle = "cn.wjybxx.dson.text.NumberStyle";
    public static final String CNAME_StringStyle = "cn.wjybxx.dson.text.StringStyle";
    public static final String CNAME_ObjectStyle = "cn.wjybxx.dson.text.ObjectStyle";

    // Document
    public static final String CNAME_DOC_SERIALIZABLE = "cn.wjybxx.common.codec.document.DocumentSerializable";
    public static final String CNAME_DOC_IGNORE = "cn.wjybxx.common.codec.document.DocumentIgnore";
    private static final String CNAME_DOC_READER = "cn.wjybxx.common.codec.document.DocumentObjectReader";
    private static final String CNAME_DOC_WRITER = "cn.wjybxx.common.codec.document.DocumentObjectWriter";
    private static final String CNAME_DOC_SCAN_IGNORE = "cn.wjybxx.common.codec.document.DocumentPojoCodecScanIgnore";
    // Binary
    public static final String CNAME_BIN_SERIALIZABLE = "cn.wjybxx.common.codec.binary.BinarySerializable";
    public static final String CNAME_BIN_IGNORE = "cn.wjybxx.common.codec.binary.BinaryIgnore";
    private static final String CNAME_BIN_READER = "cn.wjybxx.common.codec.binary.BinaryObjectReader";
    private static final String CNAME_BIN_WRITER = "cn.wjybxx.common.codec.binary.BinaryObjectWriter";
    private static final String CNAME_BIN_SCAN_IGNORE = "cn.wjybxx.common.codec.binary.BinaryPojoCodecScanIgnore";
    // Linker
    private static final String CNAME_CODEC_LINKER_GROUP = "cn.wjybxx.common.codec.CodecLinkerGroup";
    private static final String CNAME_CODEC_LINKER = "cn.wjybxx.common.codec.CodecLinker";
    private static final String MNAME_OUTPUT = "outputPackage";
    private static final String MNAME_CLASSIMPL = "classImpl";

    // PojoCodecImpl
    public static final String CNAME_POJO_CODEC = "cn.wjybxx.common.codec.PojoCodecImpl";
    public static final String MNAME_READ_OBJECT = "readObject";
    public static final String MNAME_WRITE_OBJECT = "writeObject";
    // AbstractPojoCodecImpl
    private static final String CNAME_ABSTRACT_CODEC = "cn.wjybxx.common.codec.AbstractPojoCodecImpl";
    public static final String MNAME_GET_ENCODER_CLASS = "getEncoderClass";
    public static final String MNAME_NEW_INSTANCE = "newInstance";
    public static final String MNAME_READ_FIELDS = "readFields";
    public static final String MNAME_AFTER_DECODE = "afterDecode";
    // EnumLiteCode
    private static final String CNAME_ENUM_CODEC = "cn.wjybxx.common.codec.codecs.EnumLiteCodec";
    public static final String CNAME_ENUM_LITE = "cn.wjybxx.common.EnumLite";
    public static final String MNAME_FOR_NUMBER = "forNumber";
    public static final String MNAME_GET_NUMBER = "getNumber";

    //endregion

    // region 字段
    public TypeMirror anno_fieldImplTypeMirror;
    public TypeMirror anno_classImplTypeMirror;
    public ClassName typeNameTypeArgInfo;
    public ClassName typeNameWireType;
    public ClassName typeNameNumberStyle;
    public ClassName typeNameStringStyle;
    public ClassName typeNameObjectStyle;

    // document
    public TypeElement anno_docSerializable;
    public TypeMirror anno_docIgnore;
    public TypeMirror docReaderTypeMirror;
    public TypeMirror docWriterTypeMirror;
    public AnnotationSpec docScanIgnoreAnnoSpec;
    // binary
    public TypeElement anno_binSerializable;
    public TypeMirror anno_binIgnore;
    public TypeMirror binReaderTypeMirror;
    public TypeMirror binWriterTypeMirror;
    public AnnotationSpec binScanIgnoreAnnoSpec;
    // linker
    public TypeElement anno_codecLinkerGroup;
    public TypeElement anno_codecLinker;

    // abstractCodec
    public TypeElement abstractCodecTypeElement;
    public ExecutableElement getEncoderClassMethod;
    public ExecutableElement doc_newInstanceMethod;
    public ExecutableElement doc_readFieldsMethod;
    public ExecutableElement doc_afterDecodeMethod;
    public ExecutableElement doc_writeObjectMethod;
    public ExecutableElement bin_newInstanceMethod;
    public ExecutableElement bin_readFieldsMethod;
    public ExecutableElement bin_afterDecodeMethod;
    public ExecutableElement bin_writeObjectMethod;
    // enumLiteCodec
    public TypeElement enumCodecTypeElement;

    // 特殊类型依赖
    // 基础类型
    public TypeMirror stringTypeMirror;
    public TypeMirror enumLiteTypeMirror;
    // 集合类型
    public TypeMirror mapTypeMirror;
    public TypeMirror collectionTypeMirror;
    public TypeMirror setTypeMirror;
    public TypeMirror enumSetRawTypeMirror;
    public TypeMirror enumMapRawTypeMirror;
    public TypeMirror linkedHashMapTypeMirror;
    public TypeMirror linkedHashSetTypeMirror;
    public TypeMirror arrayListTypeMirror;

    // endregion

    public CodecProcessor() {
    }

    @Override
    public Set<String> getSupportedAnnotationTypes() {
        return Set.of(CNAME_BIN_SERIALIZABLE, CNAME_DOC_SERIALIZABLE);
    }

    @Override
    protected void ensureInited() {
        if (typeNameWireType != null) return;
        // common
        anno_fieldImplTypeMirror = elementUtils.getTypeElement(CNAME_FIELD_IMPL).asType();
        anno_classImplTypeMirror = elementUtils.getTypeElement(CNAME_CLASS_IMPL).asType();
        typeNameTypeArgInfo = ClassName.get(elementUtils.getTypeElement(CNAME_TYPEARG));
        typeNameWireType = AptUtils.classNameOfCanonicalName(CNAME_WireType);
        typeNameNumberStyle = AptUtils.classNameOfCanonicalName(CNAME_NumberStyle);
        typeNameStringStyle = AptUtils.classNameOfCanonicalName(CNAME_StringStyle);
        typeNameObjectStyle = AptUtils.classNameOfCanonicalName(CNAME_ObjectStyle);

        // document
        anno_docSerializable = elementUtils.getTypeElement(CNAME_DOC_SERIALIZABLE);
        anno_docIgnore = elementUtils.getTypeElement(CNAME_DOC_IGNORE).asType();
        docReaderTypeMirror = elementUtils.getTypeElement(CNAME_DOC_READER).asType();
        docWriterTypeMirror = elementUtils.getTypeElement(CNAME_DOC_WRITER).asType();
        docScanIgnoreAnnoSpec = AnnotationSpec.builder(ClassName.get(elementUtils.getTypeElement(CNAME_DOC_SCAN_IGNORE)))
                .build();
        // binary
        anno_binSerializable = elementUtils.getTypeElement(CNAME_BIN_SERIALIZABLE);
        anno_binIgnore = elementUtils.getTypeElement(CNAME_BIN_IGNORE).asType();
        binReaderTypeMirror = elementUtils.getTypeElement(CNAME_BIN_READER).asType();
        binWriterTypeMirror = elementUtils.getTypeElement(CNAME_BIN_WRITER).asType();
        binScanIgnoreAnnoSpec = AnnotationSpec.builder(ClassName.get(elementUtils.getTypeElement(CNAME_BIN_SCAN_IGNORE)))
                .build();
        // linker
        anno_codecLinkerGroup = elementUtils.getTypeElement(CNAME_CODEC_LINKER_GROUP);
        anno_codecLinker = elementUtils.getTypeElement(CNAME_CODEC_LINKER);

        // PojoCodec
        TypeElement pojoCodecTypeElement = elementUtils.getTypeElement(CNAME_POJO_CODEC);
        getEncoderClassMethod = AptUtils.findMethodByName(pojoCodecTypeElement, MNAME_GET_ENCODER_CLASS);
        // abstractCodec
        abstractCodecTypeElement = elementUtils.getTypeElement(CNAME_ABSTRACT_CODEC);
        {
            List<ExecutableElement> allMethodsWithInherit = BeanUtils.getAllMethodsWithInherit(abstractCodecTypeElement);
            // doc
            doc_newInstanceMethod = findCodecMethod(allMethodsWithInherit, MNAME_NEW_INSTANCE, docReaderTypeMirror);
            doc_readFieldsMethod = findCodecMethod(allMethodsWithInherit, MNAME_READ_FIELDS, docReaderTypeMirror);
            doc_afterDecodeMethod = findCodecMethod(allMethodsWithInherit, MNAME_AFTER_DECODE, docReaderTypeMirror);
            doc_writeObjectMethod = findCodecMethod(allMethodsWithInherit, MNAME_WRITE_OBJECT, docWriterTypeMirror);
            // bin
            bin_newInstanceMethod = findCodecMethod(allMethodsWithInherit, MNAME_NEW_INSTANCE, binReaderTypeMirror);
            bin_readFieldsMethod = findCodecMethod(allMethodsWithInherit, MNAME_READ_FIELDS, binReaderTypeMirror);
            bin_afterDecodeMethod = findCodecMethod(allMethodsWithInherit, MNAME_AFTER_DECODE, binReaderTypeMirror);
            bin_writeObjectMethod = findCodecMethod(allMethodsWithInherit, MNAME_WRITE_OBJECT, binWriterTypeMirror);
        }
        // enumLiteCodec
        enumCodecTypeElement = elementUtils.getTypeElement(CNAME_ENUM_CODEC);

        // 特殊类型依赖
        // 基础类型
        stringTypeMirror = elementUtils.getTypeElement(String.class.getCanonicalName()).asType();
        enumLiteTypeMirror = elementUtils.getTypeElement(CNAME_ENUM_LITE).asType();
        // 集合
        mapTypeMirror = elementUtils.getTypeElement(Map.class.getCanonicalName()).asType();
        collectionTypeMirror = elementUtils.getTypeElement(Collection.class.getCanonicalName()).asType();
        setTypeMirror = elementUtils.getTypeElement(Set.class.getCanonicalName()).asType();
        enumSetRawTypeMirror = typeUtils.erasure(AptUtils.getTypeMirrorOfClass(elementUtils, EnumSet.class));
        enumMapRawTypeMirror = typeUtils.erasure(AptUtils.getTypeMirrorOfClass(elementUtils, EnumMap.class));
        linkedHashMapTypeMirror = typeUtils.erasure(AptUtils.getTypeMirrorOfClass(elementUtils, LinkedHashMap.class));
        linkedHashSetTypeMirror = typeUtils.erasure(AptUtils.getTypeMirrorOfClass(elementUtils, LinkedHashSet.class));
        arrayListTypeMirror = typeUtils.erasure(AptUtils.getTypeMirrorOfClass(elementUtils, ArrayList.class));
    }

    private ExecutableElement findCodecMethod(List<ExecutableElement> allMethodsWithInherit,
                                              String methodName, TypeMirror firstArg) {
        return allMethodsWithInherit.stream()
                .filter(e -> e.getKind() == ElementKind.METHOD && e.getSimpleName().toString().equals(methodName))
                .filter(e -> e.getParameters().size() > 0
                        && AptUtils.isSameTypeIgnoreTypeParameter(typeUtils, e.getParameters().get(0).asType(), firstArg)
                )
                .findFirst()
                .orElseThrow(() -> new RuntimeException("method is absent, methodName: " + methodName));
    }

    @Override
    protected boolean doProcess(Set<? extends TypeElement> annotations, RoundEnvironment roundEnv) {
        final Set<TypeElement> allTypeElements = AptUtils.selectSourceFileAny(roundEnv, elementUtils,
                anno_binSerializable, anno_docSerializable, anno_codecLinkerGroup);
        for (TypeElement typeElement : allTypeElements) {
            try {
                // 各种缓存，避免频繁解析类型信息
                Context context = new Context(typeElement);
                context.binSerialAnnoMirror = AptUtils.findAnnotation(typeUtils, typeElement, anno_binSerializable.asType())
                        .orElse(null);
                context.docSerialAnnoMirror = AptUtils.findAnnotation(typeUtils, typeElement, anno_docSerializable.asType())
                        .orElse(null);
                context.linkerGroupAnnoMirror = AptUtils.findAnnotation(typeUtils, typeElement, anno_codecLinkerGroup.asType())
                        .orElse(null);

                context.aptClassImpl = AptClassImpl.parse(typeUtils, typeElement, anno_classImplTypeMirror);
                context.allFieldsAndMethodWithInherit = BeanUtils.getAllFieldsAndMethodsWithInherit(typeElement);
                cacheAptFieldImpl(context);

                if (context.linkerGroupAnnoMirror != null) {
                    // 不是为自己生成，而是为字段生成
                    processLinkerGroup(context);
                } else {
                    processDirectType(context);
                }
            } catch (Throwable e) {
                messager.printMessage(Diagnostic.Kind.ERROR, AptUtils.getStackTrace(e), typeElement);
            }
        }
        return true;
    }

    private void processLinkerGroup(Context groupContext) {
        final String outPackage = AptUtils.getAnnotationValueValue(groupContext.linkerGroupAnnoMirror, MNAME_OUTPUT);
        Objects.requireNonNull(outPackage, "outPackage");

        for (VariableElement variableElement : groupContext.allFields) {
            AnnotationMirror linkerAnnoMirror = AptUtils.findAnnotation(typeUtils, variableElement, anno_codecLinker.asType())
                    .orElse(null);
            if (linkerAnnoMirror == null) { // 不需要链接的字段
                continue;
            }
            DeclaredType declaredType = AptUtils.findDeclaredType(variableElement.asType());
            if (declaredType == null) {
                messager.printMessage(Diagnostic.Kind.ERROR, "Bad Linker Target", variableElement);
                continue;
            }

            AnnotationValue classImplAnnoValue = AptUtils.getAnnotationValue(linkerAnnoMirror, MNAME_CLASSIMPL);
            Objects.requireNonNull(classImplAnnoValue, "classImp props is absent");
            AptClassImpl aptClassImpl = AptClassImpl.parse((AnnotationMirror) classImplAnnoValue.getValue());

            // 创建模拟数据
            TypeElement typeElement = (TypeElement) declaredType.asElement();
            Context context = new Context(typeElement);
            context.linkerAddAnnotations = getAdditionalAnnotations(linkerAnnoMirror);
            context.binSerialAnnoMirror = context.docSerialAnnoMirror = linkerAnnoMirror;
            context.binAddAnnotations = context.docAddAnnotations = context.linkerAddAnnotations;

            context.aptClassImpl = aptClassImpl;
            context.allFieldsAndMethodWithInherit = BeanUtils.getAllFieldsAndMethodsWithInherit(typeElement);
            cacheAptFieldImpl(context);
            // binary
            {
                context.serialTypeElement = anno_binSerializable;
                context.ignoreTypeMirror = anno_binIgnore;
                context.readerTypeMirror = binReaderTypeMirror;
                context.writerTypeMirror = binWriterTypeMirror;

                context.serialFields = new ArrayList<>();
                checkTypeElement(context);
                context.binSerialFields.addAll(context.serialFields);
            }
            // document
            {
                context.serialTypeElement = anno_docSerializable;
                context.ignoreTypeMirror = anno_docIgnore;
                context.readerTypeMirror = docReaderTypeMirror;
                context.writerTypeMirror = docWriterTypeMirror;

                context.serialFields = new ArrayList<>();
                checkTypeElement(context);
                context.docSerialFields.addAll(context.serialFields);
            }
            generateCodec(context);
        }
    }

    private void processDirectType(Context context) {
        if (context.binSerialAnnoMirror != null) {
            context.binAddAnnotations = getAdditionalAnnotations(context.binSerialAnnoMirror);
            context.serialTypeElement = anno_binSerializable;
            context.ignoreTypeMirror = anno_binIgnore;
            context.readerTypeMirror = binReaderTypeMirror;
            context.writerTypeMirror = binWriterTypeMirror;

            context.serialFields = new ArrayList<>();
            checkTypeElement(context);
            context.binSerialFields.addAll(context.serialFields);
        }
        if (context.docSerialAnnoMirror != null) {
            context.docAddAnnotations = getAdditionalAnnotations(context.docSerialAnnoMirror);
            context.serialTypeElement = anno_docSerializable;
            context.ignoreTypeMirror = anno_docIgnore;
            context.readerTypeMirror = docReaderTypeMirror;
            context.writerTypeMirror = docWriterTypeMirror;

            context.serialFields = new ArrayList<>();
            checkTypeElement(context);
            context.docSerialFields.addAll(context.serialFields);
        }
        generateCodec(context);
    }

    private void generateCodec(Context context) {
        TypeElement typeElement = context.typeElement;
        if (isEnumLite(typeElement.asType())) {
            DeclaredType superDeclaredType = typeUtils.getDeclaredType(enumCodecTypeElement, typeUtils.erasure(typeElement.asType()));
            initTypeBuilder(context, typeElement, superDeclaredType);
            //
            new EnumCodecGenerator(this, typeElement, context).execute();
        } else {
            DeclaredType superDeclaredType = typeUtils.getDeclaredType(abstractCodecTypeElement, typeUtils.erasure(typeElement.asType()));
            initTypeBuilder(context, typeElement, superDeclaredType);
            // 先生成常量字段
            SchemaGenerator schemaGenerator = new SchemaGenerator(this, context);
            schemaGenerator.execute();
            // 不论注解是否存在，所有方法都要实现
            // Binary
            {
                context.serialAnnoMirror = context.binSerialAnnoMirror;
                context.scanIgnoreAnnoSpec = binScanIgnoreAnnoSpec;
                context.additionalAnnotations = context.binAddAnnotations;

                context.serialTypeElement = anno_binSerializable;
                context.ignoreTypeMirror = anno_binIgnore;
                context.readerTypeMirror = binReaderTypeMirror;
                context.writerTypeMirror = binWriterTypeMirror;
                context.serialFields = context.binSerialFields;
                context.serialNameAccess = "numbers_";
                new PojoCodecGenerator(this, context).execute();
            }
            // Document
            {
                context.serialAnnoMirror = context.docSerialAnnoMirror;
                context.scanIgnoreAnnoSpec = docScanIgnoreAnnoSpec;
                context.additionalAnnotations = context.docAddAnnotations;

                context.serialTypeElement = anno_docSerializable;
                context.ignoreTypeMirror = anno_docIgnore;
                context.readerTypeMirror = docReaderTypeMirror;
                context.writerTypeMirror = docWriterTypeMirror;
                context.serialFields = context.docSerialFields;
                context.serialNameAccess = "names_";
                new PojoCodecGenerator(this, context).execute();
            }
        }
        // 写入文件
        if (context.outPackage != null) {
            AptUtils.writeToFile(typeElement, context.typeBuilder, context.outPackage, messager, filer);
        } else {
            AptUtils.writeToFile(typeElement, context.typeBuilder, elementUtils, messager, filer);
        }
    }

    // region

    private void cacheAptFieldImpl(Context context) {
        context.allFields = context.allFieldsAndMethodWithInherit.stream()
                .filter(e -> e.getKind() == ElementKind.FIELD)
                .filter(e -> !e.getModifiers().contains(Modifier.STATIC))
                .map(e -> (VariableElement) e)
                .toList();

        context.allFields.forEach(variableElement -> {
            AptFieldImpl aptFieldImpl = AptFieldImpl.parse(typeUtils, variableElement, anno_fieldImplTypeMirror);
            context.fieldImplMap.put(variableElement, aptFieldImpl);
        });
    }

    private List<AnnotationSpec> getAdditionalAnnotations(AnnotationMirror annotationMirror) {
        if (annotationMirror == null) {
            return List.of();
        }
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

    private void initTypeBuilder(Context context, TypeElement typeElement, DeclaredType superDeclaredType) {
        context.superDeclaredType = superDeclaredType;
        context.typeBuilder = TypeSpec.classBuilder(getCodecName(typeElement))
                .addModifiers(Modifier.PUBLIC, Modifier.FINAL)
                .addAnnotation(AptUtils.SUPPRESS_UNCHECKED_RAWTYPES)
                .addAnnotation(processorInfoAnnotation)
                .superclass(TypeName.get(superDeclaredType));
    }

    private String getCodecName(TypeElement typeElement) {
        return AptUtils.getProxyClassName(elementUtils, typeElement, "Codec");
    }
    // endregion

    private void checkTypeElement(Context context) {
        TypeElement typeElement = context.typeElement;
        if (!isClassOrEnum(typeElement)) {
            messager.printMessage(Diagnostic.Kind.ERROR, "unsupported type", typeElement);
            return;
        }
        if (typeElement.getKind() == ElementKind.ENUM) {
            checkEnum(typeElement);
        } else {
            checkNormalClass(context);
        }
    }

    // region 枚举检查

    /**
     * 检查枚举 - 要自动序列化的枚举，必须实现EnumLite接口且提供forNumber方法。
     */
    private void checkEnum(TypeElement typeElement) {
        if (!isEnumLite(typeElement.asType())) {
            messager.printMessage(Diagnostic.Kind.ERROR,
                    "serializable enum must implement EnumLite",
                    typeElement);
            return;
        }
        if (!containNotPrivateStaticForNumberMethod(typeElement)) {
            messager.printMessage(Diagnostic.Kind.ERROR,
                    "serializable enum must contains a not private 'static T forNumber(int)' method!",
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

    // region 普通类检查

    private void checkNormalClass(Context context) {
        final AptClassImpl aptClassImpl = context.aptClassImpl;
        if (aptClassImpl.isSingleton) {
            return;
        }
        TypeElement typeElement = context.typeElement;
        checkConstructor(typeElement, context.readerTypeMirror);

        final List<? extends Element> allFieldsAndMethodWithInherit = context.allFieldsAndMethodWithInherit;
        for (Element element : allFieldsAndMethodWithInherit) {
            if (element.getKind() != ElementKind.FIELD) {
                continue;
            }
            final VariableElement variableElement = (VariableElement) element;
            if (!isSerializableField(variableElement, context.ignoreTypeMirror)) {
                continue;
            }

            context.serialFields.add(variableElement);
            AptFieldImpl aptFieldImpl = context.fieldImplMap.get(variableElement);

            if (isAutoWriteField(variableElement, aptClassImpl, aptFieldImpl)) {
                if (aptFieldImpl.hasWriteProxy()) {
                    continue;
                }
                // 工具写：需要提供可直接取值或包含非private的getter方法
                if (AptUtils.isBlank(aptFieldImpl.getter)
                        && !canGetDirectly(variableElement, typeElement)
                        && findNotPrivateGetter(variableElement, allFieldsAndMethodWithInherit) == null) {
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
                if (AptUtils.isBlank(aptFieldImpl.setter)
                        && !canSetDirectly(variableElement, typeElement)
                        && findNotPrivateSetter(variableElement, allFieldsAndMethodWithInherit) == null) {
                    messager.printMessage(Diagnostic.Kind.ERROR,
                            String.format("serializable field (%s) must contains a not private setter or canSetDirectly", variableElement.getSimpleName()),
                            typeElement); // 可能无法定位到超类字段，因此打印到Type
                    continue;
                }
            }
        }
    }

    /** 检查是否包含无参构造方法或解析构造方法 */
    private void checkConstructor(TypeElement typeElement, TypeMirror readerTypeMirror) {
        if (typeElement.getModifiers().contains(Modifier.ABSTRACT)) {
            return;
        }
        if (BeanUtils.containsNoArgsConstructor(typeElement)
                || containsReaderConstructor(typeElement, readerTypeMirror)) {
            return;
        }
        messager.printMessage(Diagnostic.Kind.ERROR,
                "SerializableClass %s must contains no-args constructor or reader-args constructor!",
                typeElement);
    }

    // region 钩子方法检查

    /** 是否包含 T(Reader reader) 构造方法 */
    public boolean containsReaderConstructor(TypeElement typeElement, TypeMirror readerTypeMirror) {
        return BeanUtils.containsOneArgsConstructor(typeUtils, typeElement, readerTypeMirror);
    }

    /** 是否包含 readerObject 实例方法 */
    public boolean containsReadObjectMethod(List<? extends Element> allFieldsAndMethodWithInherit, TypeMirror readerTypeMirror) {
        return allFieldsAndMethodWithInherit.stream()
                .filter(e -> e.getKind() == ElementKind.METHOD)
                .map(e -> (ExecutableElement) e)
                .filter(e -> !e.getModifiers().contains(Modifier.PRIVATE) && !e.getModifiers().contains(Modifier.STATIC))
                .filter(e -> e.getParameters().size() == 1)
                .filter(e -> e.getSimpleName().toString().equals(MNAME_READ_OBJECT))
                .anyMatch(e -> AptUtils.isSameTypeIgnoreTypeParameter(typeUtils, e.getParameters().get(0).asType(), readerTypeMirror));
    }

    /** 是否包含 writeObject 实例方法 */
    public boolean containsWriteObjectMethod(List<? extends Element> allFieldsAndMethodWithInherit, TypeMirror writerTypeMirror) {
        return allFieldsAndMethodWithInherit.stream()
                .filter(e -> e.getKind() == ElementKind.METHOD)
                .map(e -> (ExecutableElement) e)
                .filter(e -> !e.getModifiers().contains(Modifier.PRIVATE) && !e.getModifiers().contains(Modifier.STATIC))
                .filter(e -> e.getParameters().size() == 1)
                .filter(e -> e.getSimpleName().toString().equals(MNAME_WRITE_OBJECT))
                .anyMatch(e -> AptUtils.isSameTypeIgnoreTypeParameter(typeUtils, e.getParameters().get(0).asType(), writerTypeMirror));
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
    // endregion

    // region 字段检查

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
     * 查找非private的getter方法
     *
     * @param allFieldsAndMethodWithInherit 所有的字段和方法，可能在父类中
     */
    public ExecutableElement findNotPrivateGetter(final VariableElement variableElement,
                                                  final List<? extends Element> allFieldsAndMethodWithInherit) {
        return BeanUtils.findNotPrivateGetter(typeUtils, variableElement, allFieldsAndMethodWithInherit);
    }

    /**
     * 查找非private的setter方法
     *
     * @param allFieldsAndMethodWithInherit 所有的字段和方法，可能在父类中
     */
    public ExecutableElement findNotPrivateSetter(final VariableElement variableElement,
                                                  final List<? extends Element> allFieldsAndMethodWithInherit) {
        return BeanUtils.findNotPrivateSetter(typeUtils, variableElement, allFieldsAndMethodWithInherit);
    }

    /** 是否是可序列化的字段 */
    public boolean isSerializableField(VariableElement variableElement, TypeMirror ignoreTypeMirror) {
        if (variableElement.getModifiers().contains(Modifier.STATIC)) {
            return false;
        }
        // 有注解的情况下，取决于注解的值
        AnnotationMirror ignoreAnnoMirror = AptUtils.findAnnotation(typeUtils, variableElement, ignoreTypeMirror)
                .orElse(null);
        if (ignoreAnnoMirror != null) {
            Boolean ignore = AptUtils.getAnnotationValueValueWithDefaults(elementUtils, ignoreAnnoMirror, "value");
            return ignore != Boolean.TRUE;
        }
        // 无注解的情况下，默认忽略 transient 字段
        return !variableElement.getModifiers().contains(Modifier.TRANSIENT);
    }

    /** 是否是托管写的字段 */
    public static boolean isAutoWriteField(VariableElement variableElement, AptClassImpl aptClassImpl, AptFieldImpl aptFieldImpl) {
        if (aptClassImpl.isSingleton) {
            return false;
        }
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
    public static boolean isAutoReadField(VariableElement variableElement, AptClassImpl aptClassImpl, AptFieldImpl aptFieldImpl) {
        if (aptClassImpl.isSingleton) {
            return false;
        }
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

    // endregion

    // region 类型测试
    protected boolean isClassOrEnum(TypeElement typeElement) {
        return typeElement.getKind() == ElementKind.CLASS
                || typeElement.getKind() == ElementKind.ENUM;
    }

    protected boolean isString(TypeMirror typeMirror) {
        return typeUtils.isSameType(typeMirror, stringTypeMirror);
    }

    protected boolean isByteArray(TypeMirror typeMirror) {
        return AptUtils.isByteArray(typeMirror);
    }

    protected boolean isEnumLite(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, enumLiteTypeMirror);
    }

    protected boolean isMap(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, mapTypeMirror);
    }

    protected boolean isCollection(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, collectionTypeMirror);
    }

    protected boolean isSet(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, setTypeMirror);
    }

    protected boolean isEnumSet(TypeMirror typeMirror) {
        return typeMirror == enumSetRawTypeMirror || AptUtils.isSameTypeIgnoreTypeParameter(typeUtils, typeMirror, enumSetRawTypeMirror);
    }

    protected boolean isEnumMap(TypeMirror typeMirror) {
        return typeMirror == enumMapRawTypeMirror || AptUtils.isSameTypeIgnoreTypeParameter(typeUtils, typeMirror, enumMapRawTypeMirror);
    }
    // endregion

    // region overriding util

    public MethodSpec newGetEncoderClassMethod(DeclaredType superDeclaredType, TypeName rawTypeName) {
        return MethodSpec.overriding(getEncoderClassMethod, superDeclaredType, typeUtils)
                .addStatement("return $T.class", rawTypeName)
                .addAnnotation(AptUtils.ANNOTATION_NONNULL)
                .build();
    }

    public MethodSpec.Builder newNewInstanceMethodBuilder(DeclaredType superDeclaredType, TypeMirror readerTypeMirror) {
        if (readerTypeMirror == binReaderTypeMirror) {
            return MethodSpec.overriding(bin_newInstanceMethod, superDeclaredType, typeUtils);
        } else {
            return MethodSpec.overriding(doc_newInstanceMethod, superDeclaredType, typeUtils);
        }
    }

    public MethodSpec.Builder newReadFieldsMethodBuilder(DeclaredType superDeclaredType, TypeMirror readerTypeMirror) {
        if (readerTypeMirror == binReaderTypeMirror) {
            return MethodSpec.overriding(bin_readFieldsMethod, superDeclaredType, typeUtils);
        } else {
            return MethodSpec.overriding(doc_readFieldsMethod, superDeclaredType, typeUtils);
        }
    }

    public MethodSpec.Builder newAfterDecodeMethodBuilder(DeclaredType superDeclaredType, TypeMirror readerTypeMirror) {
        if (readerTypeMirror == binReaderTypeMirror) {
            return MethodSpec.overriding(bin_afterDecodeMethod, superDeclaredType, typeUtils);
        } else {
            return MethodSpec.overriding(doc_afterDecodeMethod, superDeclaredType, typeUtils);
        }
    }

    public MethodSpec.Builder newWriteObjectMethodBuilder(DeclaredType superDeclaredType, TypeMirror writerTypeMirror) {
        if (writerTypeMirror == binWriterTypeMirror) {
            return MethodSpec.overriding(bin_writeObjectMethod, superDeclaredType, typeUtils);
        } else {
            return MethodSpec.overriding(doc_writeObjectMethod, superDeclaredType, typeUtils);
        }
    }

    // endregion

}