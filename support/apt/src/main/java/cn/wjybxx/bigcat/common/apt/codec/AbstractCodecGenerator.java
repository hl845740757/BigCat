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

import cn.wjybxx.bigcat.common.apt.AbstractGenerator;
import cn.wjybxx.bigcat.common.apt.BeanUtils;

import javax.lang.model.element.TypeElement;
import javax.lang.model.element.VariableElement;
import javax.lang.model.type.TypeKind;
import javax.lang.model.type.TypeMirror;
import java.util.EnumMap;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/4/13
 */
public abstract class AbstractCodecGenerator<T extends CodecProcessor> extends AbstractGenerator<T> {

    public static final String READ_STRING_METHOD_NAME = "readString";
    public static final String READ_BYTES_METHOD_NAME = "readBytes";
    public static final String READ_OBJECT_METHOD_NAME = "readObject";

    public static final String WRITE_STRING_METHOD_NAME = "writeString";
    public static final String WRITE_BYTES_METHOD_NAME = "writeBytes";
    public static final String WRITE_OBJECT_METHOD_NAME = "writeObject";

    private static final Map<TypeKind, String> primitiveReadMethodName = new EnumMap<>(TypeKind.class);
    private static final Map<TypeKind, String> primitiveWriteMethodName = new EnumMap<>(TypeKind.class);

    static {
        for (TypeKind typeKind : TypeKind.values()) {
            if (!typeKind.isPrimitive()) {
                continue;
            }
            final String name = BeanUtils.firstCharToUpperCase(typeKind.name().toLowerCase());
            primitiveReadMethodName.put(typeKind, "read" + name);
            primitiveWriteMethodName.put(typeKind, "write" + name);
        }
    }

    public AbstractCodecGenerator(T processor, TypeElement typeElement) {
        super(processor, typeElement);
    }

    protected String getGetterName(VariableElement variableElement) {
        return BeanUtils.getterMethodName(variableElement.getSimpleName().toString(), isPrimitiveBool(variableElement));
    }

    protected String getSetterName(VariableElement variableElement) {
        return BeanUtils.setterMethodName(variableElement.getSimpleName().toString(), isPrimitiveBool(variableElement));
    }

    private static boolean isPrimitiveBool(VariableElement variableElement) {
        return variableElement.asType().getKind() == TypeKind.BOOLEAN;
    }

    /** 获取writer写字段的方法名 */
    protected String getWriteMethodName(VariableElement variableElement) {
        TypeMirror typeMirror = variableElement.asType();
        if (isPrimitiveType(typeMirror)) {
            return primitiveWriteMethodName.get(typeMirror.getKind());
        }
        if (processor.isString(typeMirror)) {
            return WRITE_STRING_METHOD_NAME;
        }
        if (processor.isByteArray(typeMirror)) {
            return WRITE_BYTES_METHOD_NAME;
        }
        return WRITE_OBJECT_METHOD_NAME;
    }

    /** 获取reader读字段的方法名 */
    protected String getReadMethodName(VariableElement variableElement) {
        TypeMirror typeMirror = variableElement.asType();
        if (isPrimitiveType(typeMirror)) {
            return primitiveReadMethodName.get(typeMirror.getKind());
        }
        if (processor.isString(typeMirror)) {
            return READ_STRING_METHOD_NAME;
        }
        if (processor.isByteArray(typeMirror)) {
            return READ_BYTES_METHOD_NAME;
        }
        return READ_OBJECT_METHOD_NAME;
    }

    private static boolean isPrimitiveType(TypeMirror typeMirror) {
        return typeMirror.getKind().isPrimitive();
    }

}
