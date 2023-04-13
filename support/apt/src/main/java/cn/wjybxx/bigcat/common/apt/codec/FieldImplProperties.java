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
import javax.lang.model.element.AnnotationValue;
import javax.lang.model.element.VariableElement;
import javax.lang.model.type.TypeMirror;
import javax.lang.model.util.Types;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/4/6
 */
public class FieldImplProperties {

    /** 注解是否存在 */
    public boolean isAnnotationPresent = false;
    /** 实现类 */
    public TypeMirror implMirror;
    /** 写代理方法名 */
    public String writeProxy = "";
    /** 读代理方法名 */
    public String readProxy = "";

    @Nonnull
    public static FieldImplProperties parse(Types typeUtils, VariableElement variableElement, TypeMirror implMirror) {
        final FieldImplProperties properties = new FieldImplProperties();
        final AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, variableElement, implMirror)
                .orElse(null);
        if (annotationMirror != null) {
            final Map<String, AnnotationValue> valueMap = AptUtils.getAnnotationValuesMap(annotationMirror);
            final AnnotationValue value = valueMap.get("value");
            final AnnotationValue writeProxy = valueMap.get("writeProxy");
            final AnnotationValue readProxy = valueMap.get("readProxy");
            if (value != null) {
                properties.implMirror = AptUtils.getAnnotationValueTypeMirror(value);
            }
            if (writeProxy != null) {
                properties.writeProxy = ((String) writeProxy.getValue()).trim();
            }
            if (readProxy != null) {
                properties.readProxy = ((String) readProxy.getValue()).trim();
            }
            properties.isAnnotationPresent = true;
        }
        return properties;
    }

    public boolean hasWriteProxy() {
        return !AptUtils.isBlank(writeProxy);
    }

    public boolean hasReadProxy() {
        return !AptUtils.isBlank(readProxy);
    }

}