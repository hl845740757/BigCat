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

import cn.wjybxx.bigcat.common.apt.AbstractGenerator;
import cn.wjybxx.bigcat.common.apt.AptUtils;
import com.squareup.javapoet.MethodSpec;
import com.squareup.javapoet.TypeName;
import com.squareup.javapoet.TypeSpec;

import javax.lang.model.element.Modifier;
import javax.lang.model.element.TypeElement;
import javax.lang.model.type.DeclaredType;

import static cn.wjybxx.bigcat.common.apt.codec.CodecProcessor.MNAME_FOR_NUMBER;
import static cn.wjybxx.bigcat.common.apt.codec.CodecProcessor.MNAME_GET_NUMBER;

/**
 * @author wjybxx
 * date 2023/4/13
 */
public class EnumBinCodecGenerator extends AbstractGenerator<BinaryCodecProcessor> {

    EnumBinCodecGenerator(BinaryCodecProcessor processor, TypeElement typeElement) {
        super(processor, typeElement);
    }

    @Override
    public void execute() {
        final TypeName rawTypeName = TypeName.get(typeUtils.erasure(typeElement.asType()));
        final DeclaredType superDeclaredType = typeUtils.getDeclaredType(processor.codecTypeElement, typeUtils.erasure(typeElement.asType()));

        // getEncoderClass
        final MethodSpec getEncoderClassMethod = processor.newGetEncoderClassMethod(superDeclaredType, rawTypeName);

        // 写入number即可 write.writeInt(instance.getNumber())
        final MethodSpec.Builder writeMethodBuilder = processor.newWriteObjectMethodBuilder(superDeclaredType);
        writeMethodBuilder.addStatement("writer.writeInt(instance.$L())", MNAME_GET_NUMBER);

        // 读取number即可 return A.forNumber(reader.readInt())
        final MethodSpec.Builder readMethodBuilder = processor.newReadObjectMethodBuilder(superDeclaredType);
        readMethodBuilder.addStatement("return $T.$L(reader.readInt())", rawTypeName, MNAME_FOR_NUMBER);

        final TypeSpec.Builder typeBuilder = TypeSpec.classBuilder(BinaryCodecProcessor.getCodecClassName(typeElement, elementUtils));
        typeBuilder.addModifiers(Modifier.PUBLIC, Modifier.FINAL)
                .addAnnotation(AptUtils.SUPPRESS_UNCHECKED_ANNOTATION)
                .addAnnotation(processorInfoAnnotation)
                .addAnnotations(processor.getAdditionalAnnotations(typeElement))
                .addSuperinterface(TypeName.get(superDeclaredType))
                .addMethod(getEncoderClassMethod)
                .addMethod(writeMethodBuilder.build())
                .addMethod(readMethodBuilder.build());

        // 写入文件
        AptUtils.writeToFile(typeElement, typeBuilder, elementUtils, messager, filer);
    }

}