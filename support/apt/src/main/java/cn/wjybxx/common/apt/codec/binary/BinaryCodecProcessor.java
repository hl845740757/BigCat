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

import cn.wjybxx.common.apt.AptUtils;
import cn.wjybxx.common.apt.codec.CodecProcessor;
import com.google.auto.service.AutoService;

import javax.annotation.processing.Processor;
import javax.annotation.processing.RoundEnvironment;
import javax.lang.model.element.ElementKind;
import javax.lang.model.element.TypeElement;
import javax.lang.model.util.Elements;
import javax.tools.Diagnostic;
import java.util.Set;

/**
 * @author wjybxx
 * date 2023/4/13
 */
@AutoService(Processor.class)
public class BinaryCodecProcessor extends CodecProcessor {

    private static final String CNAME_SERIALIZABLE = "cn.wjybxx.dson.codec.binary.BinarySerializable";
    private static final String CNAME_IGNORE = "cn.wjybxx.dson.codec.binary.BinaryIgnore";

    private static final String CNAME_READER = "cn.wjybxx.dson.codec.binary.BinaryObjectReader";
    private static final String CNAME_WRITER = "cn.wjybxx.dson.codec.binary.BinaryObjectWriter";
    private static final String CNAME_CODEC = "cn.wjybxx.dson.codec.binary.BinaryPojoCodecImpl";

    private static final String CNAME_ABSTRACT_CODEC = "cn.wjybxx.dson.codec.binary.AbstractBinaryPojoCodecImpl";
    private static final String CNAME_ENUM_CODEC = "cn.wjybxx.dson.codec.binary.codecs.DsonEnumCodec";

    public BinaryCodecProcessor() {
        super();
    }

    @Override
    public Set<String> getSupportedAnnotationTypes() {
        return Set.of(CNAME_SERIALIZABLE);
    }

    @Override
    protected void ensureInited() {
        if (anno_serializableTypeElement != null) {
            return;
        }
        super.ensureInited(CNAME_SERIALIZABLE, CNAME_IGNORE,
                CNAME_READER, CNAME_WRITER,
                CNAME_CODEC, CNAME_ABSTRACT_CODEC,
                CNAME_ENUM_CODEC);
    }

    @Override
    protected boolean doProcess(Set<? extends TypeElement> annotations, RoundEnvironment roundEnv) {
        final Set<TypeElement> allTypeElements = (Set<TypeElement>) roundEnv.getElementsAnnotatedWith(anno_serializableTypeElement);
        for (TypeElement typeElement : allTypeElements) {
            try {
                checkBase(typeElement);
                generateSerializer(typeElement);
            } catch (Throwable e) {
                messager.printMessage(Diagnostic.Kind.ERROR, AptUtils.getStackTrace(e), typeElement);
            }
        }
        return true;
    }

    /**
     * 基础信息检查
     */
    private void checkBase(TypeElement typeElement) {
        if (!isClassOrEnum(typeElement)) {
            messager.printMessage(Diagnostic.Kind.ERROR, "unsupported type", typeElement);
            return;
        }
        // 枚举
        if (typeElement.getKind() == ElementKind.ENUM) {
            checkEnum(typeElement);
            return;
        }
        // 检查普通类
        if (isDsonEnum(typeElement.asType())) {
            checkForNumberMethod(typeElement);
        } else {
            checkNormalClass(typeElement);
        }
    }

    // ----------------------------------------------- 辅助类生成 -------------------------------------------

    private void generateSerializer(TypeElement typeElement) {
        if (isDsonEnum(typeElement.asType())) {
            new EnumBinCodecGenerator(this, typeElement).execute();
        } else {
            new PojoBinCodecGenerator(this, typeElement).execute();
        }
    }

    static String getCodecClassName(TypeElement typeElement, Elements elementUtils) {
        return AptUtils.getProxyClassName(elementUtils, typeElement, "BinCodec");
    }

}