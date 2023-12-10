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
import com.squareup.javapoet.ClassName;
import com.squareup.javapoet.MethodSpec;
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
public class PojoCodecGenerator extends AbstractGenerator<CodecProcessor> {

    public static final String MNAME_READ_STRING = "readString";
    public static final String MNAME_READ_BYTES = "readBytes";
    public static final String MNAME_READ_OBJECT = "readObject";

    public static final String MNAME_WRITE_STRING = "writeString";
    public static final String MNAME_WRITE_BYTES = "writeBytes";
    public static final String MNAME_WRITE_OBJECT = "writeObject";

    private static final Map<TypeKind, String> primitiveReadMethodNameMap = new EnumMap<>(TypeKind.class);
    private static final Map<TypeKind, String> primitiveWriteMethodNameMap = new EnumMap<>(TypeKind.class);

    private final Context context;
    private final TypeSpec.Builder typeBuilder;
    private final TypeMirror readerTypeMirror;
    private final TypeMirror writerTypeMirror;
    private final List<? extends Element> allFieldsAndMethodWithInherit;

    protected ClassName rawTypeName;
    protected boolean containsReaderConstructor;
    protected boolean containsReadObjectMethod;
    protected boolean containsWriteObjectMethod;

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

    public PojoCodecGenerator(CodecProcessor processor, Context context) {
        super(processor, context.typeElement);
        this.context = context;
        this.typeBuilder = context.typeBuilder;
        this.readerTypeMirror = context.readerTypeMirror;
        this.writerTypeMirror = context.writerTypeMirror;
        this.allFieldsAndMethodWithInherit = context.allFieldsAndMethodWithInherit;
    }

    // region codec

    @Override
    public void execute() {
        init();

        gen();
    }

    /** 子类需要初始化 fieldsClassName */
    protected void init() {
        rawTypeName = ClassName.get(typeElement);
        containsReaderConstructor = processor.containsReaderConstructor(typeElement, readerTypeMirror);
        containsReadObjectMethod = processor.containsReadObjectMethod(allFieldsAndMethodWithInherit, readerTypeMirror);
        containsWriteObjectMethod = processor.containsWriteObjectMethod(allFieldsAndMethodWithInherit, writerTypeMirror);

        // 需要先初始化superDeclaredType
        superDeclaredType = context.superDeclaredType;
        newInstanceMethodBuilder = processor.newNewInstanceMethodBuilder(superDeclaredType, readerTypeMirror);
        readFieldsMethodBuilder = processor.newReadFieldsMethodBuilder(superDeclaredType, readerTypeMirror);
        afterDecodeMethodBuilder = processor.newAfterDecodeMethodBuilder(superDeclaredType, readerTypeMirror);
        writeObjectMethodBuilder = processor.newWriteObjectMethodBuilder(superDeclaredType, writerTypeMirror);
    }

    protected void gen() {
        // 当前生成的是不可序列化的类
        if (context.serialAnnoMirror == null) {
            newInstanceMethodBuilder.addStatement("throw new $T()", UnsupportedOperationException.class);
            readFieldsMethodBuilder.addStatement("throw new $T()", UnsupportedOperationException.class);
            writeObjectMethodBuilder.addStatement("throw new $T()", UnsupportedOperationException.class);
            typeBuilder.addAnnotation(context.scanIgnoreAnnoSpec)
                    .addMethod(newInstanceMethodBuilder.build())
                    .addMethod(readFieldsMethodBuilder.build())
                    .addMethod(writeObjectMethodBuilder.build());
            return;
        }

        AptClassImpl aptClassImpl = context.aptClassImpl;
        genNewInstanceMethod(aptClassImpl);
        if (!aptClassImpl.isSingleton) {
            if (containsReadObjectMethod) {
                readFieldsMethodBuilder.addStatement("instance.$L(reader)", CodecProcessor.MNAME_READ_OBJECT);
            }
            if (containsWriteObjectMethod) {
                writeObjectMethodBuilder.addStatement("instance.$L(writer)", CodecProcessor.MNAME_WRITE_OBJECT);
            }
            for (VariableElement variableElement : context.serialFields) {
                final AptFieldImpl aptFieldImpl = context.fieldImplMap.get(variableElement);
                if (CodecProcessor.isAutoWriteField(variableElement, aptClassImpl, aptFieldImpl)) {
                    addWriteStatement(variableElement, aptFieldImpl);
                }
                if (CodecProcessor.isAutoReadField(variableElement, aptClassImpl, aptFieldImpl)) {
                    addReadStatement(variableElement, aptFieldImpl);
                }
            }
        }
        if (context.additionalAnnotations != null) {
            typeBuilder.addAnnotations(context.additionalAnnotations);
        }
        // getEncoder
        if (!containsGetEncoderClass(typeBuilder)) {
            typeBuilder.addMethod(processor.newGetEncoderClassMethod(superDeclaredType, rawTypeName));
        }
        typeBuilder.addMethod(newInstanceMethodBuilder.build())
                .addMethod(readFieldsMethodBuilder.build());
        // afterDecode回调
        if (!aptClassImpl.isSingleton && !containsAfterDecode(typeBuilder)
                && processor.findAfterDecodeMethod(allFieldsAndMethodWithInherit) != null) {
            afterDecodeMethodBuilder.addStatement("instance.$L()", CodecProcessor.MNAME_AFTER_DECODE);
            typeBuilder.addMethod(afterDecodeMethodBuilder.build());
        }
        // writeObject
        typeBuilder.addMethod(writeObjectMethodBuilder.build());
    }

    private static boolean containsGetEncoderClass(TypeSpec.Builder typeBuilder) {
        return typeBuilder.methodSpecs.stream()
                .anyMatch(e -> e.name.equals(CodecProcessor.MNAME_GET_ENCODER_CLASS));
    }

    private static boolean containsAfterDecode(TypeSpec.Builder typeBuilder) {
        return typeBuilder.methodSpecs.stream()
                .anyMatch(e -> e.name.equals(CodecProcessor.MNAME_AFTER_DECODE));
    }

    private void genNewInstanceMethod(AptClassImpl aptClassImpl) {
        if (aptClassImpl.isSingleton) {
            newInstanceMethodBuilder.addStatement("return $T.$L()", rawTypeName, aptClassImpl.singleton);
            return;
        }
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
            builder.addStatement("instance.$L(reader, $L)", properties.readProxy, serialName(fieldName));
            return;
        }
        final String readMethodName = getReadMethodName(variableElement);
        final ExecutableElement setterMethod = processor.findNotPrivateSetter(variableElement, allFieldsAndMethodWithInherit);
        // 优先用setter，否则直接赋值 -- 不好简化
        if (!AptUtils.isBlank(properties.setter) || setterMethod != null) {
            final String setterName = AptUtils.isBlank(properties.setter) ? setterMethod.getSimpleName().toString() : properties.setter;
            if (readMethodName.equals(MNAME_READ_OBJECT)) { // 读对象时要传入类型信息
                // instance.setName(reader.readObject(XXFields.name, XXTypeArgs.name))
                builder.addStatement("instance.$L(reader.$L($L, $L))",
                        setterName, readMethodName,
                        serialName(fieldName), serialTypeArg(fieldName));
            } else {
                // instance.setName(reader.readString(XXFields.name))
                builder.addStatement("instance.$L(reader.$L($L))",
                        setterName, readMethodName,
                        serialName(fieldName));
            }
        } else {
            if (readMethodName.equals(MNAME_READ_OBJECT)) { // 读对象时要传入类型信息
                // instance.name = reader.readObject(XXFields.name, XXTypeArgs.name)
                builder.addStatement("instance.$L = reader.$L($L, $L)",
                        fieldName, readMethodName,
                        serialName(fieldName), serialTypeArg(fieldName));
            } else {
                // instance.name = reader.readString(XXFields.name)
                builder.addStatement("instance.$L = reader.$L($L)",
                        fieldName, readMethodName,
                        serialName(fieldName));
            }
        }
    }

    private void addWriteStatement(VariableElement variableElement, AptFieldImpl properties) {
        final String fieldName = variableElement.getSimpleName().toString();
        MethodSpec.Builder builder = this.writeObjectMethodBuilder;
        if (properties.hasWriteProxy()) { // 自定义写
            builder.addStatement("instance.$L(writer, $L)", properties.writeProxy, serialName(fieldName));
            return;
        }
        // 优先用getter，否则直接访问
        String fieldAccess;
        ExecutableElement getterMethod = processor.findNotPrivateGetter(variableElement, allFieldsAndMethodWithInherit);
        if (!AptUtils.isBlank(properties.getter)) {
            fieldAccess = properties.getter + "()";
        } else if (getterMethod != null) {
            fieldAccess = getterMethod.getSimpleName() + "()";
        } else {
            fieldAccess = fieldName;
        }

        // 先处理有子类型的类型
        if (properties.dsonType != null) {
            switch (properties.dsonType) {
                case AptFieldImpl.TYPE_BINARY -> {
                    // writer.writeBytes(Fields.FieldName, subType, instance.field)
                    builder.addStatement("writer.writeBytes($L, $L, instance.$L)",
                            serialName(fieldName), properties.dsonSubType, fieldAccess);
                }
                case AptFieldImpl.TYPE_EXT_INT32 -> {
                    // writer.writeExtInt32(Fields.FieldName, subType, instance.field, WireType.VARINT, NumberStyle.SIMPLE)
                    builder.addStatement("writer.writeExtInt32($L, $L, instance.$L, $T.$L, $T.$L)",
                            serialName(fieldName),
                            properties.dsonSubType, fieldAccess,
                            processor.typeNameWireType, properties.wireType,
                            processor.typeNameNumberStyle, properties.numberStyle);
                }
                case AptFieldImpl.TYPE_EXT_INT64 -> {
                    // writer.writeExtInt64(Fields.FieldName, subType, instance.field, WireType.VARINT, NumberStyle.SIMPLE)
                    builder.addStatement("writer.writeExtInt64($L, $L, instance.$L, $T.$L, $T.$L)",
                            serialName(fieldName),
                            properties.dsonSubType, fieldAccess,
                            processor.typeNameWireType, properties.wireType,
                            processor.typeNameNumberStyle, properties.numberStyle);
                }
                case AptFieldImpl.TYPE_EXT_DOUBLE -> {
                    // writer.writeExtDouble(Fields.FieldName, subType, instance.field, NumberStyle.SIMPLE)
                    builder.addStatement("writer.writeExtDouble($L, $L, instance.$L, $T.$L)",
                            serialName(fieldName),
                            properties.dsonSubType, fieldAccess,
                            processor.typeNameNumberStyle, properties.numberStyle);
                }
                case AptFieldImpl.TYPE_EXT_STRING -> {
                    // writer.writeExtInt64(Fields.FieldName, subType, instance.field, StringStyle.AUTO)
                    builder.addStatement("writer.writeExtString($L, $L, instance.$L, $T.$L)",
                            serialName(fieldName),
                            properties.dsonSubType, fieldAccess,
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
                builder.addStatement("writer.$L($L, instance.$L, $T.$L, $T.$L)",
                        writeMethodName, serialName(fieldName), fieldAccess,
                        processor.typeNameWireType, properties.wireType,
                        processor.typeNameNumberStyle, properties.numberStyle);
                return;
            }
            case FLOAT, DOUBLE -> {
                // writer.writeInt(Fields.FieldName, instance.field, NumberStyle.SIMPLE)
                builder.addStatement("writer.$L($L, instance.$L, $T.$L)",
                        writeMethodName, serialName(fieldName), fieldAccess,
                        processor.typeNameNumberStyle, properties.numberStyle);
                return;
            }
        }
        // 处理字符串
        if (writeMethodName.equals(MNAME_WRITE_STRING)) {
            // writer.writeString(XXFields.name, instance.getName(), StringStyle.AUTO)
            builder.addStatement("writer.$L($L, instance.$L, $T.$L)",
                    writeMethodName, serialName(fieldName), fieldAccess,
                    processor.typeNameStringStyle, properties.stringStyle);
            return;
        }

        if (writeMethodName.equals(MNAME_WRITE_OBJECT)) {
            // 写对象时传入类型信息和Style
            // writer.writeObject(XXFields.name, instance.getName(), XXTypeArgs.name, ObjectStyle.INDENT)
            if (properties.objectStyle != null) {
                builder.addStatement("writer.$L($L, instance.$L, $L, $T.$L)",
                        writeMethodName, serialName(fieldName), fieldAccess, serialTypeArg(fieldName),
                        processor.typeNameObjectStyle, properties.objectStyle);
            } else {
                builder.addStatement("writer.$L($L, instance.$L, $L, null)",
                        writeMethodName, serialName(fieldName), fieldAccess, serialTypeArg(fieldName));
            }
        } else {
            // writer.writeBoolean(XXFields.name, instance.getName())
            builder.addStatement("writer.$L($L, instance.$L)",
                    writeMethodName, serialName(fieldName), fieldAccess);
        }
    }

    // endregion

    // region

    // 虽然多了临时字符串拼接，但可以大幅降低字符串模板的复杂度
    private String serialName(String fieldName) {
        return context.serialNameAccess + fieldName;
    }

    private String serialTypeArg(String fieldName) {
        return "types_" + fieldName;
    }

    /** 获取writer写字段的方法名 */
    private String getWriteMethodName(VariableElement variableElement) {
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
    private String getReadMethodName(VariableElement variableElement) {
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
}
