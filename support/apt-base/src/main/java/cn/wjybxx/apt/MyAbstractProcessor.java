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

package cn.wjybxx.apt;

import com.squareup.javapoet.AnnotationSpec;

import javax.annotation.processing.*;
import javax.lang.model.SourceVersion;
import javax.lang.model.element.TypeElement;
import javax.lang.model.util.Elements;
import javax.lang.model.util.Types;
import javax.tools.Diagnostic;
import java.util.Set;

/**
 * 封装模块，避免子类错误实现
 *
 * @author wjybxx
 * date 2023/4/6
 */
public abstract class MyAbstractProcessor extends AbstractProcessor {

    protected Types typeUtils;
    protected Elements elementUtils;
    protected Messager messager;
    protected Filer filer;
    protected AnnotationSpec processorInfoAnnotation;

    @Override
    public final synchronized void init(ProcessingEnvironment processingEnv) {
        super.init(processingEnv);
        messager = processingEnv.getMessager();
        elementUtils = processingEnv.getElementUtils();
        typeUtils = processingEnv.getTypeUtils();
        filer = processingEnv.getFiler();
        processorInfoAnnotation = AptUtils.newProcessorInfoAnnotation(getClass());
    }

    @Override
    public SourceVersion getSupportedSourceVersion() {
        return AptUtils.SOURCE_VERSION;
    }

    @Override
    public abstract Set<String> getSupportedAnnotationTypes();

    @Override
    public final boolean process(Set<? extends TypeElement> annotations, RoundEnvironment roundEnv) {
        try {
            ensureInited();
        } catch (Throwable e) {
            messager.printMessage(Diagnostic.Kind.ERROR, AptUtils.getStackTrace(e));
            return false;
        }
        try {
            return doProcess(annotations, roundEnv);
        } catch (Throwable e) {
            messager.printMessage(Diagnostic.Kind.ERROR, AptUtils.getStackTrace(e));
            return false;
        }
    }

    /**
     * 确保完成了初始化
     */
    protected abstract void ensureInited();

    /**
     * 如果返回true，表示注解已经被认领，并且不会要求后续处理器处理它们;
     * 如果返回false，表示注解类型无人认领，并且可能要求后续处理器处理它们。 处理器可以始终返回相同的布尔值，或者可以基于所选择的标准改变结果。
     * 建议返回true。
     */
    protected abstract boolean doProcess(Set<? extends TypeElement> annotations, RoundEnvironment roundEnv);

}