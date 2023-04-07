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

package cn.wjybxx.bigcat.common.codec;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.lang.reflect.Array;
import java.util.*;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public class EntityConverterUtils {

    /** 默认递归限制 */
    public static final int RECURSION_LIMIT = 32;

    private static final Map<Class<?>, Class<?>> wrapperToPrimitiveTypeMap = new IdentityHashMap<>(9);
    private static final Map<Class<?>, Class<?>> primitiveTypeToWrapperMap = new IdentityHashMap<>(9);
    private static final Map<Class<?>, Object> primitiveTypeDefaultValueMap = new IdentityHashMap<>(9);

    static {
        wrapperToPrimitiveTypeMap.put(Boolean.class, boolean.class);
        wrapperToPrimitiveTypeMap.put(Byte.class, byte.class);
        wrapperToPrimitiveTypeMap.put(Character.class, char.class);
        wrapperToPrimitiveTypeMap.put(Double.class, double.class);
        wrapperToPrimitiveTypeMap.put(Float.class, float.class);
        wrapperToPrimitiveTypeMap.put(Integer.class, int.class);
        wrapperToPrimitiveTypeMap.put(Long.class, long.class);
        wrapperToPrimitiveTypeMap.put(Short.class, short.class);
        wrapperToPrimitiveTypeMap.put(Void.class, void.class);

        for (Map.Entry<Class<?>, Class<?>> entry : wrapperToPrimitiveTypeMap.entrySet()) {
            primitiveTypeToWrapperMap.put(entry.getValue(), entry.getKey());
        }

        primitiveTypeDefaultValueMap.put(Boolean.class, Boolean.FALSE);
        primitiveTypeDefaultValueMap.put(Byte.class, (byte) 0);
        primitiveTypeDefaultValueMap.put(Character.class, (char) 0);
        primitiveTypeDefaultValueMap.put(Double.class, 0d);
        primitiveTypeDefaultValueMap.put(Float.class, 0f);
        primitiveTypeDefaultValueMap.put(Integer.class, 0);
        primitiveTypeDefaultValueMap.put(Long.class, 0L);
        primitiveTypeDefaultValueMap.put(Short.class, (short) 0);
        primitiveTypeDefaultValueMap.put(Void.class, null);
    }

    public static Class<?> boxIfPrimitiveType(Class<?> type) {
        if (type.isPrimitive()) {
            return primitiveTypeToWrapperMap.get(type);
        } else {
            return type;
        }
    }

    public static Class<?> unboxIfWrapperType(Class<?> type) {
        final Class<?> result = wrapperToPrimitiveTypeMap.get(type);
        return result == null ? type : result;
    }

    public static Object getDefaultValue(Class<?> type) {
        if (type.isPrimitive()) {
            return primitiveTypeDefaultValueMap.get(type);
        } else {
            return null;
        }
    }

    public static boolean isBoxType(Class<?> type) {
        return wrapperToPrimitiveTypeMap.containsKey(type);
    }

    public static boolean isPrimitiveType(Class<?> type) {
        return type.isPrimitive();
    }

    /**
     * Check if the right-hand side type may be assigned to the left-hand side
     * type, assuming setting by reflection. Considers primitive wrapper
     * classes as assignable to the corresponding primitive types.
     *
     * @param lhsType the target type
     * @param rhsType the value type that should be assigned to the target type
     * @return if the target type is assignable from the value type
     */
    public static boolean isAssignable(Class<?> lhsType, Class<?> rhsType) {
        Objects.requireNonNull(lhsType, "Left-hand side type must not be null");
        Objects.requireNonNull(rhsType, "Right-hand side type must not be null");
        if (lhsType.isAssignableFrom(rhsType)) {
            return true;
        }
        if (lhsType.isPrimitive()) {
            Class<?> resolvedPrimitive = wrapperToPrimitiveTypeMap.get(rhsType);
            return (lhsType == resolvedPrimitive);
        } else {
            // rhsType.isPrimitive
            Class<?> resolvedWrapper = primitiveTypeToWrapperMap.get(rhsType);
            return (resolvedWrapper != null && lhsType.isAssignableFrom(resolvedWrapper));
        }
    }

    /**
     * Determine if the given type is assignable from the given value,
     * assuming setting by reflection. Considers primitive wrapper classes
     * as assignable to the corresponding primitive types.
     *
     * @param type  the target type
     * @param value the value that should be assigned to the type
     * @return if the type is assignable from the value
     */
    public static boolean isAssignableValue(Class<?> type, @Nullable Object value) {
        Objects.requireNonNull(type, "Type must not be null");
        return (value != null ? isAssignable(type, value.getClass()) : !type.isPrimitive());
    }
    //

    /**
     * 枚举实例可能是枚举类的子类，如果枚举实例声明了代码块{}，在编解码时需要转换为声明类
     */
    public static Class<?> getEncodeClass(Object value) {
        if (value instanceof Enum) {
            return ((Enum<?>) value).getDeclaringClass();
        } else {
            return value.getClass();
        }
    }

    /**
     * {@code java.lang.ClassCastException: Cannot cast java.lang.Integer to int}
     * {@link Class#cast(Object)}对基本类型有坑。。。。
     */
    public static <T> T castValue(Class<T> type, Object value) {
        if (type.isPrimitive()) {
            @SuppressWarnings("unchecked") final Class<T> boxedType = (Class<T>) primitiveTypeToWrapperMap.get(type);
            return boxedType.cast(value);
        } else {
            return type.cast(value);
        }
    }

    /** 将数组对象转换为给定类型 */
    @SuppressWarnings("unchecked")
    public static <T> T castArrayValue(Class<T> arrayType, Object arrayValue) {
        if (arrayType.isAssignableFrom(arrayValue.getClass())) {
            return (T) arrayValue;
        }

        final Class<?> componentType = arrayType.getComponentType();
        final int length = Array.getLength(arrayValue);
        @SuppressWarnings("unchecked") final T castArray = (T) Array.newInstance(componentType, length);
        if (arrayType.isAssignableFrom(arrayValue.getClass())) {
            //noinspection SuspiciousSystemArraycopy
            System.arraycopy(arrayValue, 0, castArray, 0, length);
        } else {
            // System.arrayCopy并不支持对象数组到基础类型数组
            for (int index = 0; index < length; index++) {
                Object element = Array.get(arrayValue, index);
                Array.set(castArray, index, element);
            }
        }
        return castArray;
    }

    /** List转Array */
    @SuppressWarnings("unchecked")
    public static <T, E> T convertList2Array(List<?> list, Class<T> arrayType) {
        final Class<?> componentType = arrayType.getComponentType();
        final int length = list.size();

        if (list.getClass() == ArrayList.class && !componentType.isPrimitive()) {
            final E[] tempArray = (E[]) Array.newInstance(componentType, length);
            return (T) list.toArray(tempArray);
        } else {
            final T tempArray = (T) Array.newInstance(componentType, length);
            for (int index = 0; index < length; index++) {
                Object element = list.get(index);
                Array.set(tempArray, index, element);
            }
            return tempArray;
        }
    }

    /** 是否是字节数组以外的数组 */
    public static boolean isArrayExceptBytes(@Nonnull Object value) {
        return value.getClass().isArray() && !(value instanceof byte[]);
    }

    public enum ValueKind {
        /** 基本类型 -- 不可为null */
        PRIMITIVE,
        /** 引用类型 -- 可以为null */
        REFERENCE,
    }

}