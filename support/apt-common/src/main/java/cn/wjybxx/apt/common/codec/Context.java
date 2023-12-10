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

import com.squareup.javapoet.AnnotationSpec;
import com.squareup.javapoet.TypeSpec;

import javax.annotation.Nullable;
import javax.lang.model.element.AnnotationMirror;
import javax.lang.model.element.Element;
import javax.lang.model.element.TypeElement;
import javax.lang.model.element.VariableElement;
import javax.lang.model.type.DeclaredType;
import javax.lang.model.type.TypeMirror;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * @author wjybxx
 * date - 2023/12/10
 */
public class Context {

    final TypeElement typeElement;
    // binary
    AnnotationMirror binSerialAnnoMirror;
    List<AnnotationSpec> binAddAnnotations;
    // document
    AnnotationMirror docSerialAnnoMirror;
    List<AnnotationSpec> docAddAnnotations;
    // linker
    AnnotationMirror linkerGroupAnnoMirror;
    List<AnnotationSpec> linkerAddAnnotations;

    AptClassImpl aptClassImpl;
    List<? extends Element> allFieldsAndMethodWithInherit;
    List<VariableElement> allFields;
    final Map<VariableElement, AptFieldImpl> fieldImplMap = new HashMap<>(); // 字段的注解缓存

    final List<VariableElement> binSerialFields = new ArrayList<>();
    final List<VariableElement> docSerialFields = new ArrayList<>();

    TypeElement serialTypeElement; // 当前处理的注解
    TypeMirror ignoreTypeMirror;
    TypeMirror readerTypeMirror;
    TypeMirror writerTypeMirror;
    List<VariableElement> serialFields; // 可序列化的字段；检测字段时将可序列化字段写入该List
    @Nullable
    AnnotationMirror serialAnnoMirror; // 为null则表示不可序列化
    AnnotationSpec scanIgnoreAnnoSpec;
    List<AnnotationSpec> additionalAnnotations; // 生成代码附加注解

    TypeSpec.Builder typeBuilder;
    DeclaredType superDeclaredType;
    String serialNameAccess;
    String outPackage; // 输出目录

    public Context(TypeElement typeElement) {
        this.typeElement = typeElement;
    }
}