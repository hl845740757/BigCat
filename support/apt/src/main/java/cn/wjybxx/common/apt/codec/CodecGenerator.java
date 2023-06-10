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

package cn.wjybxx.common.apt.codec;

import cn.wjybxx.common.apt.AbstractGenerator;
import cn.wjybxx.common.apt.AptUtils;
import cn.wjybxx.common.apt.BeanUtils;
import cn.wjybxx.common.apt.codec.binary.BinaryCodecProcessor;
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
    public static final String MNAME_WRITE_BYTES = "writeBytes";
    public static final String MNAME_WRITE_OBJECT = "writeObject";

    public static final String MNAME_WRITE_BINARY = "writeBytes";
    public static final String MNAME_WRITE_EXTSTRING = "writeExtString";
    public static final String MNAME_WRITE_EXTINt32 = "writeExtInt32";
    public static final String MNAME_WRITE_EXTINT64 = "writeExtInt64";

    private static final Map<TypeKind, String> primitiveReadMethodName = new EnumMap<>(TypeKind.class);
    private static final Map<TypeKind, String> primitiveWriteMethodName = new EnumMap<>(TypeKind.class);

    protected TypeName rawTypeName;
    protected List<? extends Element> allFieldsAndMethodWithInherit;
    protected boolean containsReaderConstructor;
    protected boolean containsReadObjectMethod;
    protected boolean containsWriteObjectMethod;
    protected String typeArgClassName;
    protected String fieldsClassName;

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
            primitiveReadMethodName.put(typeKind, "read" + name);
            primitiveWriteMethodName.put(typeKind, "write" + name);
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
            return primitiveWriteMethodName.get(typeMirror.getKind());
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
            return primitiveReadMethodName.get(typeMirror.getKind());
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
        typeArgClassName = AutoTypeArgsProcessor.getProxyClassName(typeElement, elementUtils);
        fieldsClassName = null;

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
            if (!processor.isSerializableField(aptClassImpl.skipFields, variableElement)) {
                continue;
            }
            final AptFieldImpl aptFieldImpl = processor.parseFiledImpl(variableElement);
            if (processor.isAutoWriteField(variableElement, containsWriteObjectMethod, aptClassImpl, aptFieldImpl)) {
                addWriteStatement(variableElement, aptFieldImpl);
            }
            if (processor.isAutoReadField(variableElement, containsReaderConstructor, containsReadObjectMethod, aptClassImpl, aptFieldImpl)) {
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

                .addMethod(processor.newGetEncoderClassMethod(superDeclaredType, rawTypeName))
                .addMethod(processor.newAutoStartMethodBuilder(superDeclaredType, aptClassImpl.autoStartEnd));

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
    private void addWriteStatement(VariableElement variableElement, AptFieldImpl properties) {
        MethodSpec.Builder builder = this.writeObjectMethodBuilder;
        if (properties.hasWriteProxy()) { // 自定义写
            builder.addStatement("instance.$L(writer)", properties.writeProxy);
            return;
        }
        final String fieldName = variableElement.getSimpleName().toString();
        final String wireType = properties.wireType;

        // 优先用getter，否则直接访问
        String access;
        if (!AptUtils.isBlank(properties.getter)) {
            access = properties.getter + "()";
        } else if (processor.containsNotPrivateGetter(variableElement, allFieldsAndMethodWithInherit)) {
            access = getGetterName(variableElement) + "()";
        } else {
            access = fieldName;
        }

        // 先处理特殊的long和string
        if (properties.dsonType != null) {
            switch (properties.dsonType) {
                case AptFieldImpl.TYPE_EXT_INT32 -> {
                    builder.addStatement("writer.$L($L.$L, $L, instance.$L, $T.$L)",
                            MNAME_WRITE_EXTINt32, fieldsClassName, fieldName,
                            properties.dsonSubType, access,
                            processor.typeNameWireType, wireType);
                }
                case AptFieldImpl.TYPE_EXT_INT64 -> {
                    builder.addStatement("writer.$L($L.$L, $L, instance.$L, $T.$L)",
                            MNAME_WRITE_EXTINT64, fieldsClassName, fieldName,
                            properties.dsonSubType, access,
                            processor.typeNameWireType, wireType);
                }
                case AptFieldImpl.TYPE_EXT_STRING -> {
                    builder.addStatement("writer.$L($L.$L, $L, instance.$L)",
                            MNAME_WRITE_EXTSTRING, fieldsClassName, fieldName,
                            properties.dsonSubType, access);
                }
                case AptFieldImpl.TYPE_BINARY -> {
                    builder.addStatement("writer.$L($L.$L, $L, instance.$L)",
                            MNAME_WRITE_BINARY, fieldsClassName, fieldName,
                            properties.dsonSubType, access);
                }
                default -> {
                    messager.printMessage(Diagnostic.Kind.ERROR, "bad dsonType ", variableElement);
                }
            }
            return;
        }

        final String writeMethodName = getWriteMethodName(variableElement);
        // 先处理支持 WireType 的数值
        switch (variableElement.asType().getKind()) {
            case INT, LONG, SHORT, BYTE -> {
                builder.addStatement("writer.$L($L.$L, instance.$L, $T.$L)",
                        writeMethodName, fieldsClassName, fieldName,
                        access,
                        processor.typeNameWireType, wireType);
                return;
            }
        }

        if (writeMethodName.equals(MNAME_WRITE_OBJECT)) { // 写对象时传入类型信息
            // writer.writeObject(XXFields.name, instance.getName(), XXTypeArgs.name)
            builder.addStatement("writer.$L($L.$L, instance.$L, $L.$L)",
                    writeMethodName, fieldsClassName, fieldName,
                    access,
                    typeArgClassName, fieldName);
        } else {
            // writer.writeString(XXFields.name, instance.getName())
            builder.addStatement("writer.$L($L.$L, instance.$L)",
                    writeMethodName, fieldsClassName, fieldName,
                    access);
        }
    }

    private void addReadStatement(VariableElement variableElement, AptFieldImpl properties) {
        MethodSpec.Builder builder = readFieldsMethodBuilder;
        if (properties.hasReadProxy()) { // 自定义读
            builder.addStatement("instance.$L(reader)", properties.readProxy);
            return;
        }
        final String fieldName = variableElement.getSimpleName().toString();
        final String readMethodName = getReadMethodName(variableElement);
        // 优先用setter，否则直接赋值 -- 不好简化

        if (!AptUtils.isBlank(properties.setter)
                || processor.containsNotPrivateSetter(variableElement, allFieldsAndMethodWithInherit)) {
            final String setterName = AptUtils.isBlank(properties.setter) ? getSetterName(variableElement) : properties.setter;
            if (readMethodName.equals(MNAME_READ_OBJECT)) { // 读对象时传入类型信息
                // instance.setName(reader.readObject(XXFields.name, XXTypeArgs.name))
                builder.addStatement("instance.$L(reader.$L($L.$L, $L.$L))",
                        setterName, readMethodName,
                        fieldsClassName, fieldName,
                        typeArgClassName, fieldName);
            } else {
                // instance.setName(reader.readString(XXFields.name))
                builder.addStatement("instance.$L(reader.$L($L.$L))",
                        setterName, readMethodName,
                        fieldsClassName, fieldName);
            }
        } else {
            if (readMethodName.equals(MNAME_READ_OBJECT)) { // 读对象时传入类型信息
                // instance.name = reader.readObject(XXFields.name, XXTypeArgs.name)
                builder.addStatement("instance.$L = reader.$L($L.$L, $L.$L)",
                        fieldName, readMethodName,
                        fieldsClassName, fieldName,
                        typeArgClassName, fieldName);
            } else {
                // instance.name = reader.readString(XXFields.name)
                builder.addStatement("instance.$L = reader.$L($L.$L)",
                        fieldName, readMethodName,
                        fieldsClassName, fieldName);
            }
        }
    }

    // endregion
}
