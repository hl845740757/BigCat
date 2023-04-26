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

package cn.wjybxx.common.apt.codec.binary;

import cn.wjybxx.common.apt.AptUtils;
import cn.wjybxx.common.apt.codec.CodecGenerator;
import com.squareup.javapoet.TypeSpec;

import javax.lang.model.element.TypeElement;

/**
 * @author wjybxx
 * date 2023/4/13
 */
class PojoBinCodecGenerator extends CodecGenerator<BinaryCodecProcessor> {

    PojoBinCodecGenerator(BinaryCodecProcessor processor, TypeElement typeElement) {
        super(processor, typeElement);
    }

    protected void init() {
        super.init();
        fieldsClassName = typeArgClassName + ".Numbers"; // 一个静态内部类
    }

    @Override
    protected void gen() {
        TypeSpec.Builder typeBuilder = genCommons(BinaryCodecProcessor.getCodecClassName(typeElement, elementUtils));

        // 写入文件
        AptUtils.writeToFile(typeElement, typeBuilder, elementUtils, messager, filer);
    }
}