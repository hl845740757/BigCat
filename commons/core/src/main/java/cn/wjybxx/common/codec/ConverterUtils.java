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

package cn.wjybxx.common.codec;

import cn.wjybxx.dson.text.*;

import javax.annotation.Nullable;
import java.lang.invoke.CallSite;
import java.lang.invoke.LambdaMetafactory;
import java.lang.invoke.MethodHandles;
import java.lang.invoke.MethodType;
import java.lang.reflect.Array;
import java.lang.reflect.Constructor;
import java.util.*;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public class ConverterUtils {

    private static final MethodType SUPPLIER_INVOKE_TYPE = MethodType.methodType(Supplier.class);
    private static final MethodType SUPPLIER_GET_METHOD_TYPE = MethodType.methodType(Object.class);

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
     * 无参构造函数转lambda实例 -- 比反射构建实例要快。
     */
    public static <T> Supplier<T> noArgConstructorToSupplier(MethodHandles.Lookup lookup, Constructor<T> constructor) throws Throwable {
        Class<T> returnType = constructor.getDeclaringClass();
        CallSite callSite = LambdaMetafactory.metafactory(lookup,
                "get", SUPPLIER_INVOKE_TYPE, SUPPLIER_GET_METHOD_TYPE,
                lookup.unreflectConstructor(constructor),
                MethodType.methodType(returnType));

        @SuppressWarnings("unchecked") Supplier<T> supplier = (Supplier<T>) callSite.getTarget().invoke();
        return supplier;
    }

    // region

    /**
     * 测试右手边的类型是否可以赋值给左边的类型。
     * 基本类型和其包装类型之间将认为是可赋值的。
     *
     * @param lhsType 基类型
     * @param rhsType 测试的类型
     * @return 如果测试的类型可以赋值给基类型则返回true，否则返回false
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
     * 测试给定的值是否可以赋值给定的类型。
     * 基本类型和其包装类型之间将认为是可赋值的，但null值不可以赋值给基本类型。
     *
     * @param type  目标类型
     * @param value 测试的值
     * @return 如果目标值可以赋值给目标类型则返回true
     */
    public static boolean isAssignableValue(Class<?> type, @Nullable Object value) {
        Objects.requireNonNull(type, "Type must not be null");
        return (value != null ? isAssignable(type, value.getClass()) : !type.isPrimitive());
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

    /** List转Array */
    @SuppressWarnings("unchecked")
    public static <T, E> T convertList2Array(List<?> list, Class<T> arrayType) {
        final Class<?> componentType = arrayType.getComponentType();
        final int length = list.size();

        if (list.getClass() == ArrayList.class && !componentType.isPrimitive()) {
            final E[] tempArray = (E[]) Array.newInstance(componentType, length);
            return (T) list.toArray(tempArray);
        }
        // System.arrayCopy并不支持对象数组到基础类型数组
        final T tempArray = (T) Array.newInstance(componentType, length);
        for (int index = 0; index < length; index++) {
            Object element = list.get(index);
            Array.set(tempArray, index, element);
        }
        return tempArray;
    }
    // endregion

    // region

    /**
     * 枚举实例可能是枚举类的子类，如果枚举实例声明了代码块{}，在编解码时需要转换为声明类
     */
    public static Class<?> getEncodeClass(Object value) {
        if (value instanceof Enum<?> e) {
            return e.getDeclaringClass();
        } else {
            return value.getClass();
        }
    }

    /**
     * 注意：默认情况下map是一个数组对象，而不是普通的对象
     */
    public static <T> boolean isEncodeAsArray(Class<T> encoderClass) {
        return encoderClass.isArray()
                || Collection.class.isAssignableFrom(encoderClass)
                || Map.class.isAssignableFrom(encoderClass);
    }

    public static INumberStyle castNumberStyle(IStyle style) {
        return style instanceof INumberStyle numberStyle ? numberStyle : NumberStyle.SIMPLE;
    }

    public static StringStyle castStringStyle(IStyle style) {
        return style instanceof StringStyle stringStyle ? stringStyle : StringStyle.AUTO;
    }

    @Nullable
    public static ObjectStyle castObjectStyle(IStyle style) {
        return style instanceof ObjectStyle objectStyle ? objectStyle : null;
    }

    @SuppressWarnings("unchecked")
    public static Collection<Object> newCollection(TypeArgInfo<?> typeArgInfo, Supplier<?> factory) {
        if (typeArgInfo.factory != null) {
            return (Collection<Object>) typeArgInfo.factory.get();
        }
        if (factory != null) {
            return (Collection<Object>) factory.get();
        }
        if (Set.class.isAssignableFrom(typeArgInfo.declaredType)) {
            return new LinkedHashSet<>();
        }
        return new ArrayList<>();
    }

    @SuppressWarnings("unchecked")
    public static Map<Object, Object> newMap(TypeArgInfo<?> typeArgInfo, Supplier<?> factory) {
        if (typeArgInfo.factory != null) {
            return (Map<Object, Object>) typeArgInfo.factory.get();
        }
        if (factory != null) {
            return (Map<Object, Object>) factory.get();
        }
        return new LinkedHashMap<>();
    }

    public static TypeArgInfo<?> findComponentTypeArg(Class<?> declaredType) {
        Class<?> componentType = declaredType.getComponentType();
        if (componentType == null) {
            throw new IllegalArgumentException("declaredType is not arrayType, info " + declaredType);
        }
        if (componentType != Object.class) {
            return TypeArgInfo.of(componentType);
        }
        return TypeArgInfo.OBJECT;
    }

    // endregion
}