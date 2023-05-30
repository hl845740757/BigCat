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

package cn.wjybxx.common.apt;

import com.squareup.javapoet.AnnotationSpec;

import javax.annotation.processing.Filer;
import javax.annotation.processing.Messager;
import javax.lang.model.element.TypeElement;
import javax.lang.model.util.Elements;
import javax.lang.model.util.Types;

/**
 * @author wjybxx
 * date 2023/4/12
 */
public abstract class AbstractGenerator<T extends MyAbstractProcessor> {

    protected final T processor;
    protected final Types typeUtils;
    protected final Elements elementUtils;
    protected final Messager messager;
    protected final Filer filer;
    protected final AnnotationSpec processorInfoAnnotation;
    /** 要处理的类文件 */
    protected final TypeElement typeElement;

    public AbstractGenerator(T processor, TypeElement typeElement) {
        this.processor = processor;
        this.typeUtils = processor.typeUtils;
        this.elementUtils = processor.elementUtils;
        this.messager = processor.messager;
        this.filer = processor.filer;
        this.processorInfoAnnotation = processor.processorInfoAnnotation;
        this.typeElement = typeElement;
    }

    public abstract void execute();

}