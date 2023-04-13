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
import cn.wjybxx.bigcat.common.apt.codec.AbstractCodecGenerator;
import cn.wjybxx.bigcat.common.apt.codec.AutoTypeArgsProcessor;
import cn.wjybxx.bigcat.common.apt.codec.ClassImplProperties;
import cn.wjybxx.bigcat.common.apt.codec.FieldImplProperties;
import com.squareup.javapoet.MethodSpec;
import com.squareup.javapoet.TypeName;
import com.squareup.javapoet.TypeSpec;

import javax.lang.model.element.*;
import javax.lang.model.type.DeclaredType;
import java.util.List;

import static cn.wjybxx.bigcat.common.apt.codec.CodecProcessor.MNAME_AFTER_DECODE;

/**
 * @author wjybxx
 * date 2023/4/13
 */
class PojoBinCodecGenerator extends AbstractCodecGenerator<BinaryCodecProcessor> {

    private TypeName rawTypeName;
    private List<? extends Element> allFieldsAndMethodWithInherit;
    private boolean containsReaderConstructor;
    private boolean containsWriterMethod;
    private String typeArgClassName;

    private DeclaredType superDeclaredType;
    private MethodSpec.Builder newInstanceMethodBuilder;
    private MethodSpec.Builder readFieldsMethodBuilder;
    private MethodSpec.Builder afterDecodeMethodBuilder;
    private MethodSpec getEncoderClassMethod;
    private MethodSpec.Builder writeObjectMethodBuilder;

    PojoBinCodecGenerator(BinaryCodecProcessor processor, TypeElement typeElement) {
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
        containsWriterMethod = processor.containsWriterMethod(allFieldsAndMethodWithInherit);
        typeArgClassName = AutoTypeArgsProcessor.getProxyClassName(typeElement, elementUtils);

        // 需要先初始化superDeclaredType
        superDeclaredType = typeUtils.getDeclaredType(processor.abstractCodecTypeElement, typeUtils.erasure(typeElement.asType()));
        newInstanceMethodBuilder = MethodSpec.overriding(processor.newInstanceMethod, superDeclaredType, typeUtils);
        readFieldsMethodBuilder = MethodSpec.overriding(processor.readFieldsMethod, superDeclaredType, typeUtils);
        afterDecodeMethodBuilder = MethodSpec.overriding(processor.afterDecodeMethod, superDeclaredType, typeUtils);
        getEncoderClassMethod = processor.newGetEncoderClassMethod(superDeclaredType, rawTypeName);
        writeObjectMethodBuilder = processor.newWriteObjectMethodBuilder(superDeclaredType);
    }

    private void gen() {
        genNewInstanceMethod(); // 先挂载自定义读
        if (containsWriterMethod) {  // 先挂载自定义写
            writeObjectMethodBuilder.addStatement("instance.$L(writer)", BinaryCodecProcessor.MNAME_WRITE_OBJECT);
        }

        ClassImplProperties classImplProperties = processor.parseClassImpl(typeElement);
        for (Element element : allFieldsAndMethodWithInherit) {
            if (element.getKind() != ElementKind.FIELD) {
                continue;
            }
            final VariableElement variableElement = (VariableElement) element;
            if (!processor.isSerializableField(variableElement)) {
                continue;
            }
            final FieldImplProperties fieldImplProperties = processor.parseFiledImpl(variableElement);
            if (processor.isAutoWriteField(variableElement, containsWriterMethod, classImplProperties, fieldImplProperties)) {
                addWriteStatement(variableElement, fieldImplProperties);
            }
            if (processor.isAutoReadField(variableElement, containsReaderConstructor, classImplProperties, fieldImplProperties)) {
                addReadStatement(variableElement, fieldImplProperties);
            }
        }

        TypeSpec.Builder typeBuilder = TypeSpec.classBuilder(BinaryCodecProcessor.getCodecClassName(typeElement, elementUtils))
                .addModifiers(Modifier.PUBLIC, Modifier.FINAL)
                .addAnnotation(AptUtils.SUPPRESS_UNCHECKED_ANNOTATION)
                .addAnnotation(processorInfoAnnotation)
                .addAnnotations(processor.getAdditionalAnnotations(typeElement))
                .superclass(TypeName.get(superDeclaredType))
                .addMethod(getEncoderClassMethod)
                .addMethod(newInstanceMethodBuilder.build())
                .addMethod(readFieldsMethodBuilder.build())
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
            // writer.writeObject(instance.getName(), XXTypeArgs.name)
            writeObjectMethodBuilder.addStatement("writer.$L(instance.$L, $L.$L)", writeMethodName, access, typeArgClassName, fieldName);
        } else {
            // writer.writeString(instance.getName())
            writeObjectMethodBuilder.addStatement("writer.$L(instance.$L)", writeMethodName, access);
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
                // instance.setName(reader.readObject(XXTypeArgs.name))
                readFieldsMethodBuilder.addStatement("instance.$L(reader.$L($L.$L))", setterName, readMethodName, typeArgClassName, fieldName);
            } else {
                // instance.setName(reader.readString(XXTypeArgs.name))
                readFieldsMethodBuilder.addStatement("instance.$L(reader.$L())", setterName, readMethodName);
            }
        } else {
            if (readMethodName.equals(READ_OBJECT_METHOD_NAME)) { // 读对象时传入类型信息
                // instance.name = reader.readObject(XXTypeArgs.name)
                readFieldsMethodBuilder.addStatement("instance.$L = reader.$L($L.$L)", fieldName, readMethodName, typeArgClassName, fieldName);
            } else {
                // instance.name = reader.readString()
                readFieldsMethodBuilder.addStatement("instance.$L = reader.$L()", fieldName, readMethodName);
            }
        }
    }

}