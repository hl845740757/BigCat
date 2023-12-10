/*
 *  Copyright 2023 wjybxx
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to iBn writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

package cn.wjybxx.apt.common.codec;

import cn.wjybxx.apt.AbstractGenerator;
import com.squareup.javapoet.ClassName;
import com.squareup.javapoet.MethodSpec;

import javax.lang.model.element.Modifier;
import javax.lang.model.element.TypeElement;

/**
 * @author wjybxx
 * date 2023/4/13
 */
public class EnumCodecGenerator extends AbstractGenerator<CodecProcessor> {

    private final Context context;

    public EnumCodecGenerator(CodecProcessor processor, TypeElement typeElement,
                              Context context) {
        super(processor, typeElement);
        this.context = context;
    }

    @Override
    public void execute() {
        final ClassName rawTypeName = ClassName.get(typeElement);
        final MethodSpec constructor = MethodSpec.constructorBuilder()
                .addModifiers(Modifier.PUBLIC)
                .addStatement("super($T.class, $T::forNumber)", rawTypeName, rawTypeName)
                .build();
        context.typeBuilder.addMethod(constructor);
    }

}