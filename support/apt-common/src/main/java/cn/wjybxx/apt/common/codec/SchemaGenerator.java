package cn.wjybxx.apt.common.codec;

import cn.wjybxx.apt.AbstractGenerator;
import cn.wjybxx.apt.AptUtils;
import com.squareup.javapoet.ClassName;
import com.squareup.javapoet.FieldSpec;
import com.squareup.javapoet.ParameterizedTypeName;
import com.squareup.javapoet.TypeName;

import javax.lang.model.element.Modifier;
import javax.lang.model.element.TypeElement;
import javax.lang.model.element.VariableElement;
import javax.lang.model.type.DeclaredType;
import javax.lang.model.type.TypeMirror;
import javax.tools.Diagnostic;
import java.util.*;
import java.util.stream.Collectors;

/**
 * 方法对象
 *
 * @author wjybxx
 * date - 2023/12/10
 */
public class SchemaGenerator extends AbstractGenerator<CodecProcessor> {

    private final Context context;
    private final ClassName typeArgRawTypeName;

    public SchemaGenerator(CodecProcessor processor, Context context) {
        super(processor, context.typeElement);
        this.context = context;
        this.typeArgRawTypeName = processor.typeNameTypeArgInfo;
    }

    @Override
    public void execute() {
        final List<FieldSpec> typesFields = genTypeFields();
        final List<FieldSpec> numbersSpec = genNumbers();
        final List<FieldSpec> namesSpec = genNames();
        context.typeBuilder.addFields(typesFields)
                .addFields(numbersSpec)
                .addFields(namesSpec);
    }

    // region typeArgs

    private List<FieldSpec> genTypeFields() {
        // 需要去重
        LinkedHashSet<VariableElement> allSerialFields = new LinkedHashSet<>();
        allSerialFields.addAll(context.binSerialFields);
        allSerialFields.addAll(context.docSerialFields);
        return allSerialFields.stream()
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

        String constName = "types_" + variableElement.getSimpleName().toString();
        FieldSpec.Builder builder = FieldSpec.builder(fieldTypeName, constName, AptUtils.PUBLIC_STATIC_FINAL);

        TypeArgMirrors typeArgMirrors = parseTypeArgMirrors(variableElement);
        if (typeArgMirrors.value != null) { // map
            if (typeArgMirrors.impl == null) {
                builder.initializer("$T.of($T.class, null, $T.class, $T.class)",
                        typeArgRawTypeName,
                        TypeName.get(typeUtils.erasure(typeArgMirrors.declared)),
                        TypeName.get(typeUtils.erasure(typeArgMirrors.key)),
                        TypeName.get(typeUtils.erasure(typeArgMirrors.value)));
            } else if (processor.isEnumMap(typeArgMirrors.impl)) { // enumMap
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
            } else if (processor.isEnumSet(typeArgMirrors.impl)) { // enumSet
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
        AptFieldImpl properties = context.fieldImplMap.get(variableElement);
        TypeMirror typeMirror = variableElement.asType();
        if (processor.isMap(typeMirror)) {
            return parseMapTypeArgs(variableElement, properties);
        }
        TypeMirror typeMirror1 = variableElement.asType();
        if (processor.isCollection(typeMirror1)) {
            return parseCollectionTypeArgs(variableElement, properties);
        }
        // 普通类型字段
        return TypeArgMirrors.of(typeUtils.erasure(variableElement.asType()), properties.implMirror);
    }

    private TypeArgMirrors parseMapTypeArgs(VariableElement variableElement, AptFieldImpl properties) {
        // 查找真实的实现类型，自身或EnumMap，或Impl属性
        final TypeMirror realImplMirror = parseMapVarImpl(variableElement, properties);
        // 查找传递给Map接口的KV泛型参数
        final DeclaredType superTypeMirror = AptUtils.upwardToSuperTypeMirror(typeUtils, variableElement.asType(), processor.mapTypeMirror);
        final List<TypeMirror> kvTypeMirrors = new ArrayList<>(superTypeMirror.getTypeArguments());
        if (kvTypeMirrors.size() != 2) {
            messager.printMessage(Diagnostic.Kind.ERROR, "Can't find key or value type of map", variableElement);
        }
        return TypeArgMirrors.ofMap(variableElement.asType(), realImplMirror, kvTypeMirrors.get(0), kvTypeMirrors.get(1));
    }

    private TypeArgMirrors parseCollectionTypeArgs(VariableElement variableElement, AptFieldImpl properties) {
        TypeMirror realImplMirror = parseCollectionVarImpl(variableElement, properties);
        // 查找传递给Collection接口的E泛型参数
        final DeclaredType superTypeMirror = AptUtils.upwardToSuperTypeMirror(typeUtils, variableElement.asType(), processor.collectionTypeMirror);
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
        TypeMirror typeMirror = variableElement.asType();
        if (processor.isEnumMap(typeMirror)) {
            return processor.enumMapRawTypeMirror;
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
        checkDefaultImpl(variableElement, processor.linkedHashMapTypeMirror);
        return null;
    }

    private TypeMirror parseCollectionVarImpl(VariableElement variableElement, AptFieldImpl properties) {
        if (!AptUtils.isBlank(properties.readProxy)) {
            return null; // 有读代理，不需要解析
        }
        TypeMirror typeMirror1 = variableElement.asType();
        if (processor.isEnumSet(typeMirror1)) {
            return processor.enumSetRawTypeMirror;
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
        TypeMirror typeMirror = variableElement.asType();
        if (processor.isSet(typeMirror)) {
            checkDefaultImpl(variableElement, processor.linkedHashSetTypeMirror);
        } else {
            checkDefaultImpl(variableElement, processor.arrayListTypeMirror);
        }
        return null;
    }

    /** 检查字段是否是默认实现类型的超类 */
    private void checkDefaultImpl(VariableElement variableElement, TypeMirror defImpl) {
        if (!AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, defImpl, variableElement.asType())) {
            messager.printMessage(Diagnostic.Kind.ERROR,
                    "Unknown abstract Map or Collection must contains impl annotation " + CodecProcessor.CNAME_FIELD_IMPL,
                    variableElement);
        }
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

    private List<FieldSpec> genNames() {
        final List<VariableElement> serialFields = context.docSerialFields;
        final Set<String> dsonNameSet = new HashSet<>((int) (serialFields.size() * 1.35f));
        final List<FieldSpec> fieldSpecList = new ArrayList<>(serialFields.size());

        for (VariableElement variableElement : serialFields) {
            AptFieldImpl properties = context.fieldImplMap.get(variableElement);
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

    private List<FieldSpec> genNumbers() {
        final List<VariableElement> serialFields = context.binSerialFields;
        final Set<Integer> fullNumberSet = new HashSet<>((int) (serialFields.size() * 1.35f));
        final List<FieldSpec> fieldSpecList = new ArrayList<>(serialFields.size());

        int curIdep = -1;
        int curNumber = 0;
        TypeElement curTypeElement = null;
        for (VariableElement variableElement : serialFields) {
            // idep必须在切换类时重新计算
            if (curTypeElement == null || !curTypeElement.equals(variableElement.getEnclosingElement())) {
                curTypeElement = (TypeElement) variableElement.getEnclosingElement();
                curIdep = calIdep(curTypeElement);
            }
            AptFieldImpl properties = context.fieldImplMap.get(variableElement);
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