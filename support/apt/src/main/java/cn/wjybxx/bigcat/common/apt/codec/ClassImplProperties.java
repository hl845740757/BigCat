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

package cn.wjybxx.bigcat.common.apt.codec;

import cn.wjybxx.bigcat.common.apt.AptUtils;

import javax.annotation.Nonnull;
import javax.lang.model.element.AnnotationMirror;
import javax.lang.model.element.TypeElement;
import javax.lang.model.type.TypeMirror;
import javax.lang.model.util.Types;

/**
 * @author wjybxx
 * date 2023/4/13
 */
public class ClassImplProperties {

    public boolean isAnnotationPresent = false;
    // 默认值要保持和注解一致
    public boolean autoReadNonFinalFields = true;
    public boolean autoWriteNonFinalFields = true;
    public boolean autoWriteFinalFields = false;

    @Nonnull
    public static ClassImplProperties parse(Types typeUtils, TypeElement typeElement, TypeMirror implMirror) {
        final ClassImplProperties properties = new ClassImplProperties();
        final AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, typeElement, implMirror)
                .orElse(null);
        if (annotationMirror != null) {
            properties.autoReadNonFinalFields = AptUtils.getAnnotationValueValue(annotationMirror, "autoReadNonFinalFields",
                    properties.autoReadNonFinalFields);

            properties.autoWriteNonFinalFields = AptUtils.getAnnotationValueValue(annotationMirror, "autoWriteNonFinalFields",
                    properties.autoWriteNonFinalFields);

            properties.autoWriteFinalFields = AptUtils.getAnnotationValueValue(annotationMirror, "autoWriteFinalFields",
                    properties.autoWriteFinalFields);

            properties.isAnnotationPresent = true;
        }
        return properties;
    }
}