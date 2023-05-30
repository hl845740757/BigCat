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

package cn.wjybxx.common.apt.codec;

import cn.wjybxx.common.apt.AptUtils;

import javax.annotation.Nonnull;
import javax.lang.model.element.AnnotationMirror;
import javax.lang.model.element.AnnotationValue;
import javax.lang.model.element.TypeElement;
import javax.lang.model.type.TypeMirror;
import javax.lang.model.util.Types;
import java.util.List;
import java.util.Set;
import java.util.stream.Collectors;

/**
 * @author wjybxx
 * date 2023/4/13
 */
public class AptClassImpl {

    public boolean isAnnotationPresent = false;
    // 默认值要保持和注解一致
    public boolean autoStartEnd = true;
    public Set<String> skipFields = Set.of();
    public boolean autoReadNonFinalFields = true;
    public boolean autoWriteNonFinalFields = true;
    public boolean autoWriteFinalFields = false;

    @Nonnull
    public static AptClassImpl parse(Types typeUtils, TypeElement typeElement, TypeMirror implMirror) {
        final AptClassImpl properties = new AptClassImpl();
        final AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, typeElement, implMirror)
                .orElse(null);
        if (annotationMirror != null) {
            properties.isAnnotationPresent = true;
            properties.autoStartEnd = AptUtils.getAnnotationValueValue(annotationMirror, "autoStartEnd",
                    properties.autoStartEnd);

            // 字符串数组属性返回值为List - List都不是直接值...
            List<AnnotationValue> skipFields = AptUtils.getAnnotationValueValue(annotationMirror, "skipFields");
            if (skipFields != null) {
                properties.skipFields = skipFields.stream()
                        .map(e -> (String) e.getValue())
                        .collect(Collectors.toSet());
            }

            properties.autoReadNonFinalFields = AptUtils.getAnnotationValueValue(annotationMirror, "autoReadNonFinalFields",
                    properties.autoReadNonFinalFields);

            properties.autoWriteNonFinalFields = AptUtils.getAnnotationValueValue(annotationMirror, "autoWriteNonFinalFields",
                    properties.autoWriteNonFinalFields);

            properties.autoWriteFinalFields = AptUtils.getAnnotationValueValue(annotationMirror, "autoWriteFinalFields",
                    properties.autoWriteFinalFields);
        }
        return properties;
    }
}