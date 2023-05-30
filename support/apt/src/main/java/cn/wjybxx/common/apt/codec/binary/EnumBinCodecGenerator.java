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

package cn.wjybxx.common.apt.codec.binary;

import cn.wjybxx.common.apt.AbstractGenerator;
import cn.wjybxx.common.apt.AptUtils;
import com.squareup.javapoet.MethodSpec;
import com.squareup.javapoet.TypeName;
import com.squareup.javapoet.TypeSpec;

import javax.lang.model.element.Modifier;
import javax.lang.model.element.TypeElement;
import javax.lang.model.type.DeclaredType;

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
        final DeclaredType superDeclaredType = typeUtils.getDeclaredType(processor.enumCodecTypeElement, typeUtils.erasure(typeElement.asType()));

        MethodSpec constructor = MethodSpec.constructorBuilder()
                .addModifiers(Modifier.PUBLIC)
                .addStatement("super($T.class, $T::forNumber)", rawTypeName, rawTypeName)
                .build();

        final TypeSpec.Builder typeBuilder = TypeSpec.classBuilder(BinaryCodecProcessor.getCodecClassName(typeElement, elementUtils));
        typeBuilder.addModifiers(Modifier.PUBLIC, Modifier.FINAL)
                .addAnnotation(AptUtils.SUPPRESS_UNCHECKED_ANNOTATION)
                .addAnnotation(processorInfoAnnotation)
                .addAnnotations(processor.getAdditionalAnnotations(typeElement))
                .superclass(TypeName.get(superDeclaredType))
                .addMethod(constructor);

        // 写入文件
        AptUtils.writeToFile(typeElement, typeBuilder, elementUtils, messager, filer);
    }

}