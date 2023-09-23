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

import cn.wjybxx.apt.AbstractGenerator;
import cn.wjybxx.apt.AptUtils;
import cn.wjybxx.apt.BeanUtils;
import cn.wjybxx.apt.common.codec.binary.BinaryCodecProcessor;
import com.squareup.javapoet.MethodSpec;
import com.squareup.javapoet.TypeName;
import com.squareup.javapoet.TypeSpec;

import javax.lang.model.element.*;
import javax.lang.model.type.DeclaredType;
import javax.lang.model.type.TypeKind;
import javax.lang.model.type.TypeMirror;
import javax.tools.Diagnostic;
import java.util.EnumMap;
import java.util.List;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/4/13
 */
public abstract class CodecGenerator<T extends CodecProcessor> extends AbstractGenerator<T> {

    public static final String MNAME_READ_STRING = "readString";
    public static final String MNAME_READ_BYTES = "readBytes";
    public static final String MNAME_READ_OBJECT = "readObject";

    public static final String MNAME_WRITE_STRING = "writeString";
    public static final String MNAME_WRITE_OBJECT = "writeObject";

    public static final String MNAME_WRITE_BYTES = "writeBytes";
    public static final String MNAME_WRITE_EXTSTRING = "writeExtString";
    public static final String MNAME_WRITE_EXTINT32 = "writeExtInt32";
    public static final String MNAME_WRITE_EXTINT64 = "writeExtInt64";

    private static final Map<TypeKind, String> primitiveReadMethodNameMap = new EnumMap<>(TypeKind.class);
    private static final Map<TypeKind, String> primitiveWriteMethodNameMap = new EnumMap<>(TypeKind.class);

    protected TypeName rawTypeName;
    protected List<? extends Element> allFieldsAndMethodWithInherit;
    protected boolean containsReaderConstructor;
    protected boolean containsReadObjectMethod;
    protected boolean containsWriteObjectMethod;
    protected String schemaClassName;
    protected String nameAccess;

    protected DeclaredType superDeclaredType;
    protected MethodSpec.Builder newInstanceMethodBuilder;
    protected MethodSpec.Builder readFieldsMethodBuilder;
    protected MethodSpec.Builder afterDecodeMethodBuilder;
    protected MethodSpec.Builder writeObjectMethodBuilder;

    static {
        for (TypeKind typeKind : TypeKind.values()) {
            if (!typeKind.isPrimitive()) {
                continue;
            }
            final String name = BeanUtils.firstCharToUpperCase(typeKind.name().toLowerCase());
            primitiveReadMethodNameMap.put(typeKind, "read" + name);
            primitiveWriteMethodNameMap.put(typeKind, "write" + name);
        }
    }

    public CodecGenerator(T processor, TypeElement typeElement) {
        super(processor, typeElement);
    }

    // region
    protected String getGetterName(VariableElement variableElement) {
        return BeanUtils.getterMethodName(variableElement.getSimpleName().toString(), isPrimitiveBool(variableElement));
    }

    protected String getSetterName(VariableElement variableElement) {
        return BeanUtils.setterMethodName(variableElement.getSimpleName().toString(), isPrimitiveBool(variableElement));
    }

    private static boolean isPrimitiveBool(VariableElement variableElement) {
        return variableElement.asType().getKind() == TypeKind.BOOLEAN;
    }

    /** 获取writer写字段的方法名 */
    protected String getWriteMethodName(VariableElement variableElement) {
        TypeMirror typeMirror = variableElement.asType();
        if (isPrimitiveType(typeMirror)) {
            return primitiveWriteMethodNameMap.get(typeMirror.getKind());
        }
        if (processor.isString(typeMirror)) {
            return MNAME_WRITE_STRING;
        }
        if (processor.isByteArray(typeMirror)) {
            return MNAME_WRITE_BYTES;
        }
        return MNAME_WRITE_OBJECT;
    }

    /** 获取reader读字段的方法名 */
    protected String getReadMethodName(VariableElement variableElement) {
        TypeMirror typeMirror = variableElement.asType();
        if (isPrimitiveType(typeMirror)) {
            return primitiveReadMethodNameMap.get(typeMirror.getKind());
        }
        if (processor.isString(typeMirror)) {
            return MNAME_READ_STRING;
        }
        if (processor.isByteArray(typeMirror)) {
            return MNAME_READ_BYTES;
        }
        return MNAME_READ_OBJECT;
    }

    private static boolean isPrimitiveType(TypeMirror typeMirror) {
        return typeMirror.getKind().isPrimitive();
    }
    // endregion

    // region codec

    @Override
    public void execute() {
        init();

        gen();
    }

    /** 子类需要初始化 fieldsClassName */
    protected void init() {
        rawTypeName = TypeName.get(typeUtils.erasure(typeElement.asType()));
        allFieldsAndMethodWithInherit = BeanUtils.getAllFieldsAndMethodsWithInherit(typeElement);
        containsReaderConstructor = processor.containsReaderConstructor(typeElement);
        containsReadObjectMethod = processor.containsReadObjectMethod(allFieldsAndMethodWithInherit);
        containsWriteObjectMethod = processor.containsWriteObjectMethod(allFieldsAndMethodWithInherit);
        schemaClassName = AutoSchemaProcessor.getProxyClassName(typeElement, elementUtils);
        nameAccess = null;

        // 需要先初始化superDeclaredType
        superDeclaredType = typeUtils.getDeclaredType(processor.abstractCodecTypeElement, typeUtils.erasure(typeElement.asType()));
        newInstanceMethodBuilder = processor.newNewInstanceMethodBuilder(superDeclaredType);
        readFieldsMethodBuilder = processor.newReadFieldsMethodBuilder(superDeclaredType);
        afterDecodeMethodBuilder = processor.newAfterDecodeMethodBuilder(superDeclaredType);
        writeObjectMethodBuilder = processor.newWriteObjectMethodBuilder(superDeclaredType);
    }

    protected abstract void gen();

    protected TypeSpec.Builder genCommons(String codecClassName) {
        genNewInstanceMethod();
        if (containsReadObjectMethod) {
            readFieldsMethodBuilder.addStatement("instance.$L(reader)", BinaryCodecProcessor.MNAME_READ_OBJECT);
        }
        if (containsWriteObjectMethod) {
            writeObjectMethodBuilder.addStatement("instance.$L(writer)", BinaryCodecProcessor.MNAME_WRITE_OBJECT);
        }

        AptClassImpl aptClassImpl = processor.parseClassImpl(typeElement);
        for (Element element : allFieldsAndMethodWithInherit) {
            if (element.getKind() != ElementKind.FIELD) {
                continue;
            }
            final VariableElement variableElement = (VariableElement) element;
            if (!processor.isSerializableField(variableElement)) {
                continue;
            }
            final AptFieldImpl aptFieldImpl = processor.parseFiledImpl(variableElement);
            if (processor.isAutoWriteField(variableElement, aptClassImpl, aptFieldImpl)) {
                addWriteStatement(variableElement, aptFieldImpl);
            }
            if (processor.isAutoReadField(variableElement, aptClassImpl, aptFieldImpl)) {
                addReadStatement(variableElement, aptFieldImpl);
            }
        }

        TypeSpec.Builder typeBuilder = TypeSpec.classBuilder(codecClassName)
                .addModifiers(Modifier.PUBLIC, Modifier.FINAL)
                .addAnnotation(AptUtils.SUPPRESS_UNCHECKED_ANNOTATION)
                .addAnnotation(processorInfoAnnotation)
                .addAnnotations(processor.getAdditionalAnnotations(typeElement))

                .superclass(TypeName.get(superDeclaredType))
                .addMethod(newInstanceMethodBuilder.build())
                .addMethod(readFieldsMethodBuilder.build())
                .addMethod(writeObjectMethodBuilder.build())

                .addMethod(processor.newGetEncoderClassMethod(superDeclaredType, rawTypeName));

        // afterDecode回调
        if (processor.findAfterDecodeMethod(allFieldsAndMethodWithInherit) != null) {
            afterDecodeMethodBuilder.addStatement("instance.$L()", CodecProcessor.MNAME_AFTER_DECODE);
            typeBuilder.addMethod(afterDecodeMethodBuilder.build());
        }

        return typeBuilder;
    }

    private void genNewInstanceMethod() {
        if (typeElement.getModifiers().contains(Modifier.ABSTRACT)) {// 抽象类或接口
            newInstanceMethodBuilder.addStatement("throw new $T()", UnsupportedOperationException.class);
            return;
        }
        if (containsReaderConstructor) { // 解析构造方法
            newInstanceMethodBuilder.addStatement("return new $T(reader)", rawTypeName);
            return;
        }
        newInstanceMethodBuilder.addStatement("return new $T()", rawTypeName);
    }

    //
    private void addReadStatement(VariableElement variableElement, AptFieldImpl properties) {
        final String fieldName = variableElement.getSimpleName().toString();
        MethodSpec.Builder builder = readFieldsMethodBuilder;
        if (properties.hasReadProxy()) { // 自定义读
            builder.addStatement("instance.$L(reader, $L$L)", properties.readProxy, nameAccess, fieldName);
            return;
        }
        final String readMethodName = getReadMethodName(variableElement);
        final ExecutableElement setterMethod = processor.findNotPrivateSetter(variableElement, allFieldsAndMethodWithInherit);
        // 优先用setter，否则直接赋值 -- 不好简化
        if (!AptUtils.isBlank(properties.setter) || setterMethod != null) {
            final String setterName = AptUtils.isBlank(properties.setter) ? setterMethod.getSimpleName().toString() : properties.setter;
            if (readMethodName.equals(MNAME_READ_OBJECT)) { // 读对象时要传入类型信息
                // instance.setName(reader.readObject(XXFields.name, XXTypeArgs.name))
                builder.addStatement("instance.$L(reader.$L($L$L, $L.$L))",
                        setterName, readMethodName,
                        nameAccess, fieldName,
                        schemaClassName, fieldName);
            } else {
                // instance.setName(reader.readString(XXFields.name))
                builder.addStatement("instance.$L(reader.$L($L$L))",
                        setterName, readMethodName,
                        nameAccess, fieldName);
            }
        } else {
            if (readMethodName.equals(MNAME_READ_OBJECT)) { // 读对象时要传入类型信息
                // instance.name = reader.readObject(XXFields.name, XXTypeArgs.name)
                builder.addStatement("instance.$L = reader.$L($L$L, $L.$L)",
                        fieldName, readMethodName,
                        nameAccess, fieldName,
                        schemaClassName, fieldName);
            } else {
                // instance.name = reader.readString(XXFields.name)
                builder.addStatement("instance.$L = reader.$L($L$L)",
                        fieldName, readMethodName,
                        nameAccess, fieldName);
            }
        }
    }

    private void addWriteStatement(VariableElement variableElement, AptFieldImpl properties) {
        final String fieldName = variableElement.getSimpleName().toString();
        MethodSpec.Builder builder = this.writeObjectMethodBuilder;
        if (properties.hasWriteProxy()) { // 自定义写
            builder.addStatement("instance.$L(writer, $L$L)", properties.writeProxy, nameAccess, fieldName);
            return;
        }
        // 优先用getter，否则直接访问
        String access;
        ExecutableElement getterMethod = processor.findNotPrivateGetter(variableElement, allFieldsAndMethodWithInherit);
        if (!AptUtils.isBlank(properties.getter)) {
            access = properties.getter + "()";
        } else if (getterMethod != null) {
            access = getterMethod.getSimpleName() + "()";
        } else {
            access = fieldName;
        }

        // 先处理有子类型的类型
        if (properties.dsonType != null) {
            switch (properties.dsonType) {
                case AptFieldImpl.TYPE_BINARY -> {
                    // writer.writeBytes(Fields.FieldName, subType, instance.field)
                    builder.addStatement("writer.$L($L$L, $L, instance.$L)",
                            MNAME_WRITE_BYTES, nameAccess, fieldName,
                            properties.dsonSubType, access);
                }
                case AptFieldImpl.TYPE_EXT_INT32 -> {
                    // writer.writeExtInt32(Fields.FieldName, subType, instance.field, WireType.VARINT, NumberStyle.SIMPLE)
                    builder.addStatement("writer.$L($L$L, $L, instance.$L, $T.$L, $T.$L)",
                            MNAME_WRITE_EXTINT32, nameAccess, fieldName,
                            properties.dsonSubType, access,
                            processor.typeNameWireType, properties.wireType,
                            processor.typeNameNumberStyle, properties.numberStyle);
                }
                case AptFieldImpl.TYPE_EXT_INT64 -> {
                    // writer.writeExtInt64(Fields.FieldName, subType, instance.field, WireType.VARINT, NumberStyle.SIMPLE)
                    builder.addStatement("writer.$L($L$L, $L, instance.$L, $T.$L, $T.$L)",
                            MNAME_WRITE_EXTINT64, nameAccess, fieldName,
                            properties.dsonSubType, access,
                            processor.typeNameWireType, properties.wireType,
                            processor.typeNameNumberStyle, properties.numberStyle);
                }
                case AptFieldImpl.TYPE_EXT_STRING -> {
                    // writer.writeExtInt64(Fields.FieldName, subType, instance.field, StringStyle.AUTO)
                    builder.addStatement("writer.$L($L$L, $L, instance.$L, $T.$L)",
                            MNAME_WRITE_EXTSTRING, nameAccess, fieldName,
                            properties.dsonSubType, access,
                            processor.typeNameStringStyle, properties.stringStyle);
                }
                default -> {
                    messager.printMessage(Diagnostic.Kind.ERROR, "bad dsonType ", variableElement);
                }
            }
            return;
        }

        // 先处理数字
        final String writeMethodName = getWriteMethodName(variableElement);
        switch (variableElement.asType().getKind()) {
            case INT, LONG, SHORT, BYTE, CHAR -> {
                // writer.writeInt(Fields.FieldName, instance.field, WireType.VARINT, NumberStyle.SIMPLE)
                builder.addStatement("writer.$L($L$L, instance.$L, $T.$L, $T.$L)",
                        writeMethodName, nameAccess, fieldName, access,
                        processor.typeNameWireType, properties.wireType,
                        processor.typeNameNumberStyle, properties.numberStyle);
                return;
            }
            case FLOAT, DOUBLE -> {
                // writer.writeInt(Fields.FieldName, instance.field, NumberStyle.SIMPLE)
                builder.addStatement("writer.$L($L$L, instance.$L, $T.$L)",
                        writeMethodName, nameAccess, fieldName, access,
                        processor.typeNameNumberStyle, properties.numberStyle);
                return;
            }
        }
        if (writeMethodName.equals(MNAME_WRITE_STRING)) {
            // writer.writeString(XXFields.name, instance.getName(), StringStyle.AUTO)
            builder.addStatement("writer.$L($L$L, instance.$L, $T.$L)",
                    writeMethodName, nameAccess, fieldName, access,
                    processor.typeNameStringStyle, properties.stringStyle);
        } else if (writeMethodName.equals(MNAME_WRITE_OBJECT)) {
            // 写对象时传入类型信息和Style
            // writer.writeObject(XXFields.name, instance.getName(), XXTypeArgs.name, ObjectStyle.INDENT)
            if (properties.objectStyle != null) {
                builder.addStatement("writer.$L($L$L, instance.$L, $L.$L, $T.$L)",
                        writeMethodName, nameAccess, fieldName, access,
                        schemaClassName, fieldName,
                        processor.typeNameObjectStyle, properties.objectStyle);
            } else {
                builder.addStatement("writer.$L($L$L, instance.$L, $L.$L, null)",
                        writeMethodName, nameAccess, fieldName, access,
                        schemaClassName, fieldName);
            }
        } else {
            // writer.writeBoolean(XXFields.name, instance.getName())
            builder.addStatement("writer.$L($L$L, instance.$L)",
                    writeMethodName, nameAccess, fieldName, access);
        }
    }

    // endregion
}
