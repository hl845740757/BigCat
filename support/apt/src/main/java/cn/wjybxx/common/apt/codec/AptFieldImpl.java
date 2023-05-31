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
import javax.lang.model.element.VariableElement;
import javax.lang.model.type.TypeMirror;
import javax.lang.model.util.Types;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/4/6
 */
public class AptFieldImpl {

    public static final String TYPE_END_OF_OBJECT = "END_OF_OBJECT";
    public static final String TYPE_BINARY = "BINARY";
    public static final String TYPE_EXT_STRING = "EXT_STRING";
    public static final String TYPE_EXT_INT32 = "EXT_INT32";
    public static final String TYPE_EXT_INT64 = "EXT_INT64";

    public static final String WIRE_TYPE_VARINT = "VARINT";
    public static final String PNAME_wireType = "wireType";

    public static final String PNAME_dsonType = "dsonType";
    public static final String PNAME_dsonSubType = "dsonSubType";

    /** 注解是否存在 */
    public boolean isAnnotationPresent = false;
    /** 实现类 */
    public TypeMirror implMirror;
    /** 取值方法 */
    public String getter = "";
    /** 赋值方法 */
    public String setter = "";
    /** 写代理方法名 */
    public String writeProxy = "";
    /** 读代理方法名 */
    public String readProxy = "";

    public int number = -1;
    public int idep = -1;

    public String dsonType;
    public int dsonSubType = 0;
    public String wireType = WIRE_TYPE_VARINT;

    @Nonnull
    public static AptFieldImpl parse(Types typeUtils, VariableElement variableElement, TypeMirror implMirror) {
        final AptFieldImpl properties = new AptFieldImpl();
        final AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, variableElement, implMirror)
                .orElse(null);
        if (annotationMirror != null) {
            properties.isAnnotationPresent = true;

            final Map<String, AnnotationValue> valueMap = AptUtils.getAnnotationValuesMap(annotationMirror);

            final AnnotationValue impl = valueMap.get("value");
            if (impl != null) {
                properties.implMirror = AptUtils.getAnnotationValueTypeMirror(impl);
            }
            properties.getter = getStringValue(valueMap, "getter", properties.getter).trim();
            properties.setter = getStringValue(valueMap, "setter", properties.setter).trim();
            properties.writeProxy = getStringValue(valueMap, "writeProxy", properties.writeProxy).trim();
            properties.readProxy = getStringValue(valueMap, "readProxy", properties.readProxy).trim();

            properties.idep = getIntValue(valueMap, "idep", properties.idep);
            properties.number = getIntValue(valueMap, "number", properties.number);
            properties.dsonType = getEnumConstantName(valueMap, PNAME_dsonType, null);
            properties.dsonSubType = getIntValue(valueMap, PNAME_dsonSubType, properties.dsonSubType);
            properties.wireType = getEnumConstantName(valueMap, PNAME_wireType, properties.wireType);
        }
        return properties;
    }

    private static String getStringValue(Map<String, AnnotationValue> valueMap, String pname, String def) {
        AnnotationValue value = valueMap.get(pname);
        if (value == null) return def;
        return (String) value.getValue();
    }

    private static int getIntValue(Map<String, AnnotationValue> valueMap, String pname, int def) {
        AnnotationValue value = valueMap.get(pname);
        if (value == null) return def;
        return (Integer) value.getValue();
    }

    private static String getEnumConstantName(Map<String, AnnotationValue> valueMap, String pname, String def) {
        AnnotationValue value = valueMap.get(pname);
        if (value == null) return def;

        VariableElement enumConstant = (VariableElement) value.getValue();
        return enumConstant.getSimpleName().toString();
    }

    public boolean hasWriteProxy() {
        return !AptUtils.isBlank(writeProxy);
    }

    public boolean hasReadProxy() {
        return !AptUtils.isBlank(readProxy);
    }

}