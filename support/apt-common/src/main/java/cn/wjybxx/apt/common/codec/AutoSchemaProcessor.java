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
import cn.wjybxx.apt.common.codec.binary.BinaryCodecProcessor;
import cn.wjybxx.apt.common.codec.document.DocumentCodecProcessor;
import com.google.auto.service.AutoService;
import com.squareup.javapoet.*;

import javax.annotation.processing.Processor;
import javax.annotation.processing.RoundEnvironment;
import javax.lang.model.element.*;
import javax.lang.model.type.DeclaredType;
import javax.lang.model.type.TypeMirror;
import javax.lang.model.util.Elements;
import javax.tools.Diagnostic;
import java.util.*;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date 2023/4/6
 */
@AutoService(Processor.class)
public class AutoSchemaProcessor extends MyAbstractProcessor {

    public static final String CNAME_FIELD_IMPL = CodecProcessor.CNAME_FIELD_IMPL;
    public static final String CNAME_CLASS_IMPL = CodecProcessor.CNAME_CLASS_IMPL;

    public static final String CNAME_AUTO = "cn.wjybxx.common.codec.AutoSchema";
    public static final String CNAME_TYPEARG = "cn.wjybxx.common.codec.TypeArgInfo";

    private TypeElement anno_autoTypeElement;
    private TypeMirror anno_fieldImpTypeMirror;
    public TypeMirror anno_classImplTypeMirror;
    public TypeMirror anno_docIgnore;
    public TypeMirror anno_binIgnore;
    private ClassName typeArgRawTypeName;

    public TypeMirror mapTypeMirror;
    public TypeMirror collectionTypeMirror;
    public TypeMirror setTypeMirror;
    public TypeMirror enumSetRawTypeMirror;
    public TypeMirror enumMapRawTypeMirror;
    public TypeMirror linkedHashMapTypeMirror;
    public TypeMirror linkedHashSetTypeMirror;
    public TypeMirror arrayListTypeMirror;

    @Override
    public Set<String> getSupportedAnnotationTypes() {
        return Set.of(CNAME_AUTO);
    }

    @Override
    protected void ensureInited() {
        if (anno_autoTypeElement != null) {
            return;
        }
        anno_autoTypeElement = elementUtils.getTypeElement(CNAME_AUTO);
        anno_fieldImpTypeMirror = elementUtils.getTypeElement(CNAME_FIELD_IMPL).asType();
        anno_classImplTypeMirror = elementUtils.getTypeElement(CNAME_CLASS_IMPL).asType();
        anno_docIgnore = elementUtils.getTypeElement(DocumentCodecProcessor.CNAME_IGNORE).asType();
        anno_binIgnore = elementUtils.getTypeElement(BinaryCodecProcessor.CNAME_IGNORE).asType();
        typeArgRawTypeName = ClassName.get(elementUtils.getTypeElement(CNAME_TYPEARG));

        mapTypeMirror = elementUtils.getTypeElement(Map.class.getCanonicalName()).asType();
        collectionTypeMirror = elementUtils.getTypeElement(Collection.class.getCanonicalName()).asType();
        setTypeMirror = elementUtils.getTypeElement(Set.class.getCanonicalName()).asType();
        enumSetRawTypeMirror = typeUtils.erasure(AptUtils.getTypeMirrorOfClass(elementUtils, EnumSet.class));
        enumMapRawTypeMirror = typeUtils.erasure(AptUtils.getTypeMirrorOfClass(elementUtils, EnumMap.class));
        linkedHashMapTypeMirror = typeUtils.erasure(AptUtils.getTypeMirrorOfClass(elementUtils, LinkedHashMap.class));
        linkedHashSetTypeMirror = typeUtils.erasure(AptUtils.getTypeMirrorOfClass(elementUtils, LinkedHashSet.class));
        arrayListTypeMirror = typeUtils.erasure(AptUtils.getTypeMirrorOfClass(elementUtils, ArrayList.class));
    }

    @Override
    protected boolean doProcess(Set<? extends TypeElement> annotations, RoundEnvironment roundEnv) {
        final Set<? extends Element> annotatedClassSet = roundEnv.getElementsAnnotatedWith(anno_autoTypeElement);
        for (Element element : annotatedClassSet) {
            if (element.getKind() != ElementKind.CLASS) {
                continue; // 忽略正常类以外的东西，比如record
            }
            try {
                genTypeArgsClass((TypeElement) element);
            } catch (Throwable e) {
                messager.printMessage(Diagnostic.Kind.ERROR, AptUtils.getStackTrace(e), element);
            }
        }
        return true;
    }

    private void genTypeArgsClass(TypeElement typeElement) {
        final List<VariableElement> allFields = collectFields(typeElement);
        final List<FieldSpec> typesFields = genTypeFields(typeElement, allFields);
        final List<FieldSpec> numbersSpec = genNumbers(typeElement, allFields);
        final List<FieldSpec> namesSpec = genNames(typeElement, allFields);

        final TypeSpec.Builder typeBuilder = TypeSpec.classBuilder(getProxyClassName(typeElement, elementUtils))
                .addModifiers(Modifier.PUBLIC, Modifier.FINAL)
                .addAnnotation(AptUtils.SUPPRESS_UNCHECKED_RAWTYPES)
                .addAnnotation(processorInfoAnnotation)
                .addAnnotation(AptUtils.newSourceFileRefAnnotation(ClassName.get(typeElement)))
                .addFields(typesFields)
                .addFields(numbersSpec)
                .addFields(namesSpec);

        // 写入文件
        AptUtils.writeToFile(typeElement, typeBuilder, elementUtils, messager, filer);
    }

    /** 只处理实例字段 */
    private List<VariableElement> collectFields(TypeElement typeElement) {
        return BeanUtils.getAllFieldsWithInherit(typeElement).stream()
                .map(e -> (VariableElement) e)
                .filter(e -> {
                    return CodecProcessor.isSerializableField(typeUtils, elementUtils, e, anno_binIgnore)
                            || CodecProcessor.isSerializableField(typeUtils, elementUtils, e, anno_docIgnore);
                })
                .collect(Collectors.toList());
    }

    // region typeArgs

    private List<FieldSpec> genTypeFields(TypeElement typeElement, List<VariableElement> allFields) {
        return allFields.stream()
                .map(this::genTypeField)
                .collect(Collectors.toList());
    }

    private FieldSpec genTypeField(VariableElement variableElement) {
        ParameterizedTypeName fieldTypeName;
        if (variableElement.asType().getKind().isPrimitive()) {
            // 基础类型不能做泛型参数...
            fieldTypeName = ParameterizedTypeName.get(typeArgRawTypeName,
                    TypeName.get(variableElement.asType()).box());
        } else {
            fieldTypeName = ParameterizedTypeName.get(typeArgRawTypeName,
                    TypeName.get(typeUtils.erasure(variableElement.asType())));
        }

        FieldSpec.Builder builder = FieldSpec.builder(fieldTypeName, variableElement.getSimpleName().toString(), AptUtils.PUBLIC_STATIC_FINAL);
        TypeArgMirrors typeArgMirrors = parseTypeArgMirrors(variableElement);
        if (typeArgMirrors.value != null) { // map
            if (typeArgMirrors.impl == null) {
                builder.initializer("$T.of($T.class, null, $T.class, $T.class)",
                        typeArgRawTypeName,
                        TypeName.get(typeUtils.erasure(typeArgMirrors.declared)),
                        TypeName.get(typeUtils.erasure(typeArgMirrors.key)),
                        TypeName.get(typeUtils.erasure(typeArgMirrors.value)));
            } else if (isEnumMap(typeArgMirrors.impl)) { // enumMap
                builder.initializer("$T.ofEnumMap($T.class, $T.class, $T.class)",
                        typeArgRawTypeName,
                        TypeName.get(typeUtils.erasure(typeArgMirrors.declared)),
                        TypeName.get(typeUtils.erasure(typeArgMirrors.key)),
                        TypeName.get(typeUtils.erasure(typeArgMirrors.value)));
            } else {
                builder.initializer("$T.of($T.class, $T::new, $T.class, $T.class)",
                        typeArgRawTypeName,
                        TypeName.get(typeUtils.erasure(typeArgMirrors.declared)),
                        TypeName.get(typeUtils.erasure(typeArgMirrors.impl)),
                        TypeName.get(typeUtils.erasure(typeArgMirrors.key)),
                        TypeName.get(typeUtils.erasure(typeArgMirrors.value)));
            }
        } else if (typeArgMirrors.key != null) { // collection字段
            if (typeArgMirrors.impl == null) {
                builder.initializer("$T.of($T.class, null, $T.class, null)",
                        typeArgRawTypeName,
                        TypeName.get(typeUtils.erasure(typeArgMirrors.declared)),
                        TypeName.get(typeUtils.erasure(typeArgMirrors.key)));
            } else if (isEnumSet(typeArgMirrors.impl)) { // enumSet
                builder.initializer("$T.ofEnumSet($T.class, $T.class)",
                        typeArgRawTypeName,
                        TypeName.get(typeUtils.erasure(typeArgMirrors.declared)),
                        TypeName.get(typeUtils.erasure(typeArgMirrors.key)));
            } else {
                builder.initializer("$T.of($T.class, $T::new, $T.class, null)",
                        typeArgRawTypeName,
                        TypeName.get(typeUtils.erasure(typeArgMirrors.declared)),
                        TypeName.get(typeUtils.erasure(typeArgMirrors.impl)),
                        TypeName.get(typeUtils.erasure(typeArgMirrors.key)));
            }
        } else { // 其它类型字段
            if (typeArgMirrors.impl == null) {
                builder.initializer("$T.of($T.class)",
                        typeArgRawTypeName,
                        TypeName.get(typeUtils.erasure(typeArgMirrors.declared)));
            } else {
                builder.initializer("$T.of($T.class, $T::new)",
                        typeArgRawTypeName,
                        TypeName.get(typeUtils.erasure(typeArgMirrors.declared)),
                        TypeName.get(typeUtils.erasure(typeArgMirrors.impl)));
            }
        }
        return builder.build();
    }

    private TypeArgMirrors parseTypeArgMirrors(VariableElement variableElement) {
        AptFieldImpl properties = AptFieldImpl.parse(typeUtils, variableElement, anno_fieldImpTypeMirror);
        if (isMap(variableElement.asType())) {
            return parseMapTypeArgs(variableElement, properties);
        }
        if (isCollection(variableElement.asType())) {
            return parseCollectionTypeArgs(variableElement, properties);
        }
        // 普通类型字段
        return TypeArgMirrors.of(typeUtils.erasure(variableElement.asType()), properties.implMirror);
    }

    private TypeArgMirrors parseMapTypeArgs(VariableElement variableElement, AptFieldImpl properties) {
        // 查找真实的实现类型，自身或EnumMap，或Impl属性
        final TypeMirror realImplMirror = parseMapVarImpl(variableElement, properties);
        // 查找传递给Map接口的KV泛型参数
        final DeclaredType superTypeMirror = AptUtils.upwardToSuperTypeMirror(typeUtils, variableElement.asType(), mapTypeMirror);
        final List<TypeMirror> kvTypeMirrors = new ArrayList<>(superTypeMirror.getTypeArguments());
        if (kvTypeMirrors.size() != 2) {
            messager.printMessage(Diagnostic.Kind.ERROR, "Can't find key or value type of map", variableElement);
        }
        return TypeArgMirrors.ofMap(variableElement.asType(), realImplMirror, kvTypeMirrors.get(0), kvTypeMirrors.get(1));
    }

    private TypeArgMirrors parseCollectionTypeArgs(VariableElement variableElement, AptFieldImpl properties) {
        TypeMirror realImplMirror = parseCollectionVarImpl(variableElement, properties);
        // 查找传递给Collection接口的E泛型参数
        final DeclaredType superTypeMirror = AptUtils.upwardToSuperTypeMirror(typeUtils, variableElement.asType(), collectionTypeMirror);
        final List<? extends TypeMirror> typeArguments = superTypeMirror.getTypeArguments();
        if (typeArguments.size() != 1) {
            messager.printMessage(Diagnostic.Kind.ERROR, "Can't find element type of collection", variableElement);
        }
        return TypeArgMirrors.ofCollection(variableElement.asType(), realImplMirror, typeArguments.get(0));
    }

    private TypeMirror parseMapVarImpl(VariableElement variableElement, AptFieldImpl properties) {
        if (!AptUtils.isBlank(properties.readProxy)) {
            return null; // 有读代理，不需要解析
        }
        if (isEnumMap(variableElement.asType())) {
            return enumMapRawTypeMirror;
        }
        final DeclaredType declaredType = AptUtils.findDeclaredType(variableElement.asType());
        assert declaredType != null;

        // 具体类和抽象类都可以指定实现类
        if (properties.implMirror != null
                && AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, properties.implMirror, variableElement.asType())) {
            return properties.implMirror;
        }
        // 是具体类型
        if (!declaredType.asElement().getModifiers().contains(Modifier.ABSTRACT)) {
            return declaredType;
        }
        // 如果是抽象的，并且不是LinkedHashMap的超类，则抛出异常
        checkDefaultImpl(variableElement, linkedHashMapTypeMirror);
        return null;
    }

    private TypeMirror parseCollectionVarImpl(VariableElement variableElement, AptFieldImpl properties) {
        if (!AptUtils.isBlank(properties.readProxy)) {
            return null; // 有读代理，不需要解析
        }
        if (isEnumSet(variableElement.asType())) {
            return enumSetRawTypeMirror;
        }
        final DeclaredType declaredType = AptUtils.findDeclaredType(variableElement.asType());
        assert declaredType != null;

        // 具体类和抽象类都可以指定实现类
        if (properties.implMirror != null
                && AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, properties.implMirror, variableElement.asType())) {
            return properties.implMirror;
        }
        // 是具体类型
        if (!declaredType.asElement().getModifiers().contains(Modifier.ABSTRACT)) {
            return declaredType;
        }
        // 如果是抽象的，并且不是ArrayList/LinkedHashSet的超类，则抛出异常
        if (isSet(variableElement.asType())) {
            checkDefaultImpl(variableElement, linkedHashSetTypeMirror);
        } else {
            checkDefaultImpl(variableElement, arrayListTypeMirror);
        }
        return null;
    }

    /** 检查字段是否是默认实现类型的超类 */
    private void checkDefaultImpl(VariableElement variableElement, TypeMirror defImpl) {
        if (!AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, defImpl, variableElement.asType())) {
            messager.printMessage(Diagnostic.Kind.ERROR,
                    "Unknown abstract Map or Collection must contains impl annotation " + CNAME_FIELD_IMPL,
                    variableElement);
        }
    }

    private boolean isMap(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, mapTypeMirror);
    }

    private boolean isCollection(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, collectionTypeMirror);
    }

    private boolean isSet(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, setTypeMirror);
    }

    private boolean isEnumSet(TypeMirror typeMirror) {
        return typeMirror == enumSetRawTypeMirror || AptUtils.isSameTypeIgnoreTypeParameter(typeUtils, typeMirror, enumSetRawTypeMirror);
    }

    private boolean isEnumMap(TypeMirror typeMirror) {
        return typeMirror == enumMapRawTypeMirror || AptUtils.isSameTypeIgnoreTypeParameter(typeUtils, typeMirror, enumMapRawTypeMirror);
    }

    public static String getProxyClassName(TypeElement typeElement, Elements elementUtils) {
        return AptUtils.getProxyClassName(elementUtils, typeElement, "Schema");
    }

    private static class TypeArgMirrors {

        TypeMirror declared;
        /** fieldImpl注解指定的实现类 */
        TypeMirror impl;
        /** 集合的元素和map的key */
        TypeMirror key;
        /** map的value */
        TypeMirror value;

        public TypeArgMirrors(TypeMirror declared) {
            this.declared = declared;
        }

        private TypeArgMirrors(TypeMirror declared, TypeMirror impl, TypeMirror key, TypeMirror value) {
            this.declared = declared;
            this.impl = impl;
            this.key = key;
            this.value = value;
        }

        public static TypeArgMirrors of(TypeMirror declared, TypeMirror impl) {
            return new TypeArgMirrors(declared, impl, null, null);
        }

        public static TypeArgMirrors ofCollection(TypeMirror declared, TypeMirror impl, TypeMirror element) {
            return new TypeArgMirrors(declared, impl, element, null);
        }

        public static TypeArgMirrors ofMap(TypeMirror declared, TypeMirror impl, TypeMirror key, TypeMirror value) {
            return new TypeArgMirrors(declared, impl, key, value);
        }
    }
    // endregion

    // region names

    private List<FieldSpec> genNames(TypeElement typeElement, List<VariableElement> allFields) {
        final Set<String> dsonNameSet = new HashSet<>((int) (allFields.size() * 1.35f));
        final List<FieldSpec> fieldSpecList = new ArrayList<>(allFields.size());

        for (VariableElement variableElement : allFields) {
            // name其实可以都生成
            if (!CodecProcessor.isSerializableField(typeUtils, elementUtils, variableElement, anno_docIgnore)) {
                continue;
            }
            AptFieldImpl properties = AptFieldImpl.parse(typeUtils, variableElement, anno_fieldImpTypeMirror);
            String fieldName = variableElement.getSimpleName().toString();
            String dsonName;
            if (!AptUtils.isBlank(properties.name)) {
                dsonName = properties.name.trim();
            } else {
                dsonName = fieldName;
            }
            if (!dsonNameSet.add(dsonName)) {
                messager.printMessage(Diagnostic.Kind.ERROR,
                        String.format("dsonName is duplicate, dsonName %s", dsonName),
                        variableElement);
                continue;
            }
            fieldSpecList.add(FieldSpec.builder(AptUtils.CLSNAME_STRING, "names_" + fieldName, AptUtils.PUBLIC_STATIC_FINAL)
                    .initializer("$S", dsonName)
                    .build()
            );
        }
        return fieldSpecList;
    }

    // endregion

    // region numbers

    private List<FieldSpec> genNumbers(TypeElement typeElement, List<VariableElement> allFields) {
        final Set<Integer> fullNumberSet = new HashSet<>((int) (allFields.size() * 1.35f));
        final List<FieldSpec> fieldSpecList = new ArrayList<>(allFields.size());

        int curIdep = -1;
        int curNumber = 0;
        TypeElement curTypeElement = null;
        for (VariableElement variableElement : allFields) {
            // number一定要进行过滤
            if (!CodecProcessor.isSerializableField(typeUtils, elementUtils, variableElement, anno_binIgnore)) {
                continue;
            }
            // idep必须在切换类时重新计算
            if (curTypeElement == null || !curTypeElement.equals(variableElement.getEnclosingElement())) {
                curTypeElement = (TypeElement) variableElement.getEnclosingElement();
                curIdep = calIdep(curTypeElement);
            }

            AptFieldImpl properties = AptFieldImpl.parse(typeUtils, variableElement, anno_fieldImpTypeMirror);
            if (properties.idep >= 0) {
                curIdep = properties.idep;
            }
            if (properties.number >= 0) {
                curNumber = properties.number;
            }
            int fullNumber = makeFullNumber(curIdep, curNumber);
            if (!fullNumberSet.add(fullNumber)) {
                messager.printMessage(Diagnostic.Kind.ERROR,
                        String.format("fullNumber is duplicate, idep %d, number %d", curIdep, curNumber),
                        variableElement);
                continue;
            }

            String fieldName = variableElement.getSimpleName().toString();
            fieldSpecList.add(FieldSpec.builder(TypeName.INT, "numbers_" + fieldName, AptUtils.PUBLIC_STATIC_FINAL)
                    .initializer("$L", fullNumber)
                    .build()
            );
            curNumber++;
        }
        return fieldSpecList;
    }

    private static int calIdep(TypeElement typeElement) {
        List<TypeElement> typeElementList = AptUtils.flatInherit(typeElement);
        if (typeElementList.size() == 1) { // Object
            return 0;
        }
        // 需要去掉自身和Object
        return typeElementList.size() - 2;
    }

    private static int makeFullNumber(int idep, int lnumber) {
        return (lnumber << 3) | idep;
    }

    // endregion

}