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

package cn.wjybxx.apt.common.codec;

import cn.wjybxx.apt.AptUtils;

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
    public static final String TYPE_EXT_DOUBLE = "EXT_DOUBLE";

    public static final String WIRE_TYPE_VARINT = "VARINT";
    public static final String STYLE_SIMPLE = "SIMPLE";
    public static final String STYLE_AUTO = "AUTO";
    public static final String STYLE_INDENT = "INDENT";

    /** 注解是否存在 */
    public boolean isAnnotationPresent = false;

    /** 字段序列化时的名字 */
    public String name = "";
    /** 取值方法 */
    public String getter = "";
    /** 赋值方法 */
    public String setter = "";

    /** 实现类 */
    public TypeMirror implMirror;
    /** 写代理方法名 */
    public String writeProxy;
    /** 读代理方法名 */
    public String readProxy;

    public int number = -1;
    public int idep = -1;
    public String wireType = WIRE_TYPE_VARINT;
    public String dsonType = null; // 该属性只有显式声明才有效
    public int dsonSubType = 0;

    public String numberStyle = STYLE_SIMPLE;
    public String stringStyle = STYLE_AUTO;
    public String objectStyle = null; // 该属性只有显式声明才有效

    public boolean isDeclaredWriteProxy() {
        return isAnnotationPresent && writeProxy != null;
    }

    public boolean isDeclaredReadProxy() {
        return isAnnotationPresent && readProxy != null;
    }

    public boolean hasWriteProxy() {
        return isAnnotationPresent && !AptUtils.isBlank(writeProxy);
    }

    public boolean hasReadProxy() {
        return isAnnotationPresent && !AptUtils.isBlank(readProxy);
    }

    //
    @Nonnull
    public static AptFieldImpl parse(Types typeUtils, VariableElement variableElement, TypeMirror implMirror) {
        final AptFieldImpl properties = new AptFieldImpl();
        final AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, variableElement, implMirror)
                .orElse(null);
        if (annotationMirror != null) {
            properties.isAnnotationPresent = true;

            final Map<String, AnnotationValue> valueMap = AptUtils.getAnnotationValuesMap(annotationMirror);
            properties.name = getStringValue(valueMap, "name", properties.name);
            properties.getter = getStringValue(valueMap, "getter", properties.getter);
            properties.setter = getStringValue(valueMap, "setter", properties.setter);

            final AnnotationValue impl = valueMap.get("value");
            if (impl != null) {
                properties.implMirror = AptUtils.getAnnotationValueTypeMirror(impl);
            }
            properties.writeProxy = getStringValue(valueMap, "writeProxy", properties.writeProxy);
            properties.readProxy = getStringValue(valueMap, "readProxy", properties.readProxy);

            properties.idep = getIntValue(valueMap, "idep", properties.idep);
            properties.number = getIntValue(valueMap, "number", properties.number);
            properties.wireType = getEnumConstantName(valueMap, "wireType", properties.wireType);
            properties.dsonType = getEnumConstantName(valueMap, "dsonType", null);
            properties.dsonSubType = getIntValue(valueMap, "dsonSubType", properties.dsonSubType);

            properties.numberStyle = getEnumConstantName(valueMap, "numberStyle", properties.numberStyle);
            properties.stringStyle = getEnumConstantName(valueMap, "stringStyle", properties.stringStyle);
            properties.objectStyle = getEnumConstantName(valueMap, "objectStyle", properties.objectStyle);
        }
        return properties;
    }

    private static String getStringValue(Map<String, AnnotationValue> valueMap, String pname, String def) {
        AnnotationValue value = valueMap.get(pname);
        if (value == null) return def;
        String str = (String) value.getValue();
        return str.trim();
    }

    private static Integer getIntValue(Map<String, AnnotationValue> valueMap, String pname, Integer def) {
        AnnotationValue value = valueMap.get(pname);
        if (value == null) return def;
        return (Integer) value.getValue();
    }

    private static Boolean getBoolValue(Map<String, AnnotationValue> valueMap, String pname, Boolean def) {
        AnnotationValue value = valueMap.get(pname);
        if (value == null) return def;
        return (Boolean) value.getValue();
    }

    private static String getEnumConstantName(Map<String, AnnotationValue> valueMap, String pname, String def) {
        AnnotationValue value = valueMap.get(pname);
        if (value == null) return def;

        VariableElement enumConstant = (VariableElement) value.getValue();
        return enumConstant.getSimpleName().toString();
    }
}