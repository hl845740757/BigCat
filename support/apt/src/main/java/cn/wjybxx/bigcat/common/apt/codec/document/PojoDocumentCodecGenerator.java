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
import cn.wjybxx.bigcat.common.apt.codec.binary.BinaryCodecProcessor;
import com.squareup.javapoet.MethodSpec;
import com.squareup.javapoet.TypeName;
import com.squareup.javapoet.TypeSpec;

import javax.lang.model.element.*;
import javax.lang.model.type.DeclaredType;
import java.util.List;
import java.util.Set;

import static cn.wjybxx.bigcat.common.apt.codec.CodecProcessor.MNAME_AFTER_DECODE;

/**
 * @author wjybxx
 * date 2023/4/13
 */
public class PojoDocumentCodecGenerator extends AbstractCodecGenerator<DocumentCodecProcessor> {

    private TypeName rawTypeName;
    private List<? extends Element> allFieldsAndMethodWithInherit;
    private boolean containsReaderConstructor;
    private boolean containsReadObjectMethod;
    private boolean containsWriteObjectMethod;
    private String typeArgClassName;
    private String fieldsClassName;

    private DeclaredType superDeclaredType;
    private MethodSpec.Builder newInstanceMethodBuilder;
    private MethodSpec.Builder readFieldsMethodBuilder;
    private MethodSpec.Builder afterDecodeMethodBuilder;
    private MethodSpec getTypeNameMethod;
    private MethodSpec getEncoderClassMethod;
    private MethodSpec.Builder writeObjectMethodBuilder;

    public PojoDocumentCodecGenerator(DocumentCodecProcessor processor, TypeElement typeElement) {
        super(processor, typeElement);
    }

    @Override
    public void execute() {
        init();

        gen();
    }

    private void init() {
        rawTypeName = TypeName.get(typeUtils.erasure(typeElement.asType()));
        allFieldsAndMethodWithInherit = BeanUtils.getAllFieldsAndMethodsWithInherit(typeElement);
        containsReaderConstructor = processor.containsReaderConstructor(typeElement);
        containsReadObjectMethod = processor.containsReadObjectMethod(allFieldsAndMethodWithInherit);
        containsWriteObjectMethod = processor.containsWriteObjectMethod(allFieldsAndMethodWithInherit);
        typeArgClassName = AutoTypeArgsProcessor.getProxyClassName(typeElement, elementUtils);
        fieldsClassName = AutoFieldsProcessor.getProxyClassName(typeElement, elementUtils);

        // 需要先初始化superDeclaredType
        superDeclaredType = typeUtils.getDeclaredType(processor.abstractCodecTypeElement, typeUtils.erasure(typeElement.asType()));
        newInstanceMethodBuilder = MethodSpec.overriding(processor.newInstanceMethod, superDeclaredType, typeUtils);
        readFieldsMethodBuilder = MethodSpec.overriding(processor.readFieldsMethod, superDeclaredType, typeUtils);
        afterDecodeMethodBuilder = MethodSpec.overriding(processor.afterDecodeMethod, superDeclaredType, typeUtils);
        getTypeNameMethod = processor.newGetTypeNameMethod(superDeclaredType, typeElement);
        getEncoderClassMethod = processor.newGetEncoderClassMethod(superDeclaredType);
        writeObjectMethodBuilder = processor.newWriteObjectMethodBuilder(superDeclaredType);
    }

    private void gen() {
        genNewInstanceMethod();
        if (containsReadObjectMethod) {
            readFieldsMethodBuilder.addStatement("instance.$L(reader)", BinaryCodecProcessor.MNAME_READ_OBJECT);
        }
        if (containsWriteObjectMethod) {
            writeObjectMethodBuilder.addStatement("instance.$L(writer)", BinaryCodecProcessor.MNAME_WRITE_OBJECT);
        }

        Set<String> skipFields = processor.getSkipFields(typeElement);
        ClassImplProperties classImplProperties = processor.parseClassImpl(typeElement);

        for (Element element : allFieldsAndMethodWithInherit) {
            if (element.getKind() != ElementKind.FIELD) {
                continue;
            }
            final VariableElement variableElement = (VariableElement) element;
            if (!processor.isSerializableField(skipFields, variableElement)) {
                continue;
            }
            final FieldImplProperties fieldImplProperties = processor.parseFiledImpl(variableElement);
            if (processor.isAutoWriteField(variableElement, containsWriteObjectMethod, classImplProperties, fieldImplProperties)) {
                addWriteStatement(variableElement, fieldImplProperties);
            }
            if (processor.isAutoReadField(variableElement, containsReaderConstructor, containsReadObjectMethod, classImplProperties, fieldImplProperties)) {
                addReadStatement(variableElement, fieldImplProperties);
            }
        }

        TypeSpec.Builder typeBuilder = TypeSpec.classBuilder(DocumentCodecProcessor.getCodecClassName(typeElement, elementUtils))
                .addModifiers(Modifier.PUBLIC, Modifier.FINAL)
                .addAnnotation(AptUtils.SUPPRESS_UNCHECKED_ANNOTATION)
                .addAnnotation(processorInfoAnnotation)
                .addAnnotations(processor.getAdditionalAnnotations(typeElement))

                .superclass(TypeName.get(superDeclaredType))
                .addMethod(newInstanceMethodBuilder.build())
                .addMethod(readFieldsMethodBuilder.build())
                .addMethod(getTypeNameMethod)
                .addMethod(getEncoderClassMethod)
                .addMethod(writeObjectMethodBuilder.build());

        // afterDecode回调
        if (processor.findAfterDecodeMethod(allFieldsAndMethodWithInherit) != null) {
            afterDecodeMethodBuilder.addStatement("instance.$L()", MNAME_AFTER_DECODE);
            typeBuilder.addMethod(afterDecodeMethodBuilder.build());
        }

        // 写入文件
        AptUtils.writeToFile(typeElement, typeBuilder, elementUtils, messager, filer);
    }

    private void genNewInstanceMethod() {
        if (typeElement.getModifiers().contains(Modifier.ABSTRACT)) {
            // 抽象类或接口
            newInstanceMethodBuilder.addStatement("throw new $T()", UnsupportedOperationException.class);
            return;
        }
        if (containsReaderConstructor) {
            // 解析构造方法
            newInstanceMethodBuilder.addStatement("return new $T(reader)", rawTypeName);
            return;
        }
        newInstanceMethodBuilder.addStatement("return new $T()", rawTypeName);
    }

    //
    private void addWriteStatement(VariableElement variableElement, FieldImplProperties filedAttribute) {
        if (filedAttribute.hasWriteProxy()) { // 自定义写
            writeObjectMethodBuilder.addStatement("instance.$L(writer)", filedAttribute.writeProxy);
            return;
        }
        final String fieldName = variableElement.getSimpleName().toString();
        final String writeMethodName = getWriteMethodName(variableElement);
        // 优先用getter，否则直接访问
        String access;
        if (processor.containsNotPrivateGetter(variableElement, allFieldsAndMethodWithInherit)) {
            access = getGetterName(variableElement) + "()";
        } else {
            access = fieldName;
        }
        if (writeMethodName.equals(WRITE_OBJECT_METHOD_NAME)) { // 写对象时传入类型信息
            // writer.writeObject(XXFields.name, instance.getName(), XXTypeArgs.name)
            writeObjectMethodBuilder.addStatement("writer.$L($L.$L, instance.$L, $L.$L)",
                    writeMethodName, fieldsClassName, fieldName, access,
                    typeArgClassName, fieldName);
        } else {
            // writer.writeString(XXFields.name, instance.getName())
            writeObjectMethodBuilder.addStatement("writer.$L($L.$L, instance.$L)",
                    writeMethodName, fieldsClassName, fieldName, access);
        }
    }

    private void addReadStatement(VariableElement variableElement, FieldImplProperties filedAttribute) {
        if (filedAttribute.hasReadProxy()) { // 自定义读
            readFieldsMethodBuilder.addStatement("instance.$L(reader)", filedAttribute.readProxy);
            return;
        }
        final String fieldName = variableElement.getSimpleName().toString();
        final String readMethodName = getReadMethodName(variableElement);
        // 优先用setter，否则直接赋值 -- 不好简化
        if (processor.containsNotPrivateSetter(variableElement, allFieldsAndMethodWithInherit)) {
            final String setterName = getSetterName(variableElement);
            if (readMethodName.equals(READ_OBJECT_METHOD_NAME)) { // 读对象时传入类型信息
                // instance.setName(reader.readObject(XXFields.name, XXTypeArgs.name))
                readFieldsMethodBuilder.addStatement("instance.$L(reader.$L($L.$L, $L.$L))",
                        setterName, readMethodName,
                        fieldsClassName, fieldName,
                        typeArgClassName, fieldName);
            } else {
                // instance.setName(reader.readString(XXFields.name))
                readFieldsMethodBuilder.addStatement("instance.$L(reader.$L($L.$L))",
                        setterName, readMethodName,
                        fieldsClassName, fieldName);
            }
        } else {
            if (readMethodName.equals(READ_OBJECT_METHOD_NAME)) { // 读对象时传入类型信息
                // instance.name = reader.readObject(XXFields.name, XXTypeArgs.name)
                readFieldsMethodBuilder.addStatement("instance.$L = reader.$L($L.$L, $L.$L)",
                        fieldName, readMethodName,
                        fieldsClassName, fieldName,
                        typeArgClassName, fieldName);
            } else {
                // instance.name = reader.readString(XXFields.name)
                readFieldsMethodBuilder.addStatement("instance.$L = reader.$L($L.$L)",
                        fieldName, readMethodName,
                        fieldsClassName, fieldName);
            }
        }
    }

}