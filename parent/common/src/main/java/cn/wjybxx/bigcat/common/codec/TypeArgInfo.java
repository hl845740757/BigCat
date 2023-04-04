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

import cn.wjybxx.bigcat.common.annotation.NameIsStable;

import java.util.*;
import java.util.function.Supplier;

/**
 * 对象的泛型参数信息
 * <p>
 * Java的泛型擦除导致我们只能在一定程度上省略类型信息。
 * 要想将类型信息省略做到极致，要付出极高的代价，APT和编解码代码都会变得极其复杂，我选择放弃~
 * 其实，如果不考虑嵌套泛型，其实我们已经尽量的减少了类型信息，已经做得足够好了
 *
 * @param <T> T最好使用原始类型，不要再带有泛型，否则你会痛苦的 -- 因为 xxx.class是不包含泛型信息的。
 * @author wjybxx
 * date 2023/3/31
 */
public final class TypeArgInfo<T> {

    /** 类型的声明类型 */
    public final Class<T> declaredType;
    /** 对象工厂 */
    public final Supplier<? extends T> factory;

    /** 集合的Element、Map的Key */
    public final Class<?> typeArg1;
    /** Map的Value */
    public final Class<?> typeArg2;

    /**
     * @param declaredType 对象的声明类型
     */
    public TypeArgInfo(Class<T> declaredType) {
        this.declaredType = Objects.requireNonNull(declaredType);
        this.typeArg1 = Object.class;
        this.typeArg2 = Object.class;
        this.factory = null;
    }

    public TypeArgInfo(Class<T> declaredType, Class<?> typeArg1, Class<?> typeArg2) {
        this.declaredType = Objects.requireNonNull(declaredType);
        this.typeArg1 = nullToObjectClass(typeArg1);
        this.typeArg2 = nullToObjectClass(typeArg2);
        this.factory = null;
    }

    public TypeArgInfo(Class<T> declaredType, Supplier<? extends T> factory, Class<?> typeArg1, Class<?> typeArg2) {
        this.declaredType = declaredType;
        this.factory = factory;
        this.typeArg1 = typeArg1;
        this.typeArg2 = typeArg2;
    }

    private static Class<?> nullToObjectClass(Class<?> typeArg1) {
        return typeArg1 == null ? Object.class : typeArg1;
    }

    @Override
    public String toString() {
        return "TypeArgInfo{" +
                "declaredType=" + declaredType +
                ", typeArg1=" + typeArg1 +
                ", typeArg2=" + typeArg2 +
                '}';
    }

    // 一些常量

    /** 这里不能调用of...因为of可能返回该对象 */
    public static final TypeArgInfo<Object> OBJECT = new TypeArgInfo<>(Object.class);

    @SuppressWarnings("rawtypes")
    public static final TypeArgInfo<ArrayList> ARRAYLIST =
            new TypeArgInfo<>(ArrayList.class, ArrayList::new, Object.class, Object.class);
    @SuppressWarnings("rawtypes")
    public static final TypeArgInfo<LinkedHashSet> LINKEDHASHSET =
            new TypeArgInfo<>(LinkedHashSet.class, LinkedHashSet::new, Object.class, Object.class);

    @SuppressWarnings("rawtypes")
    public static final TypeArgInfo<LinkedHashMap> OBJECT_LINKEDHASHMAP =
            new TypeArgInfo<>(LinkedHashMap.class, LinkedHashMap::new, Object.class, Object.class);
    @SuppressWarnings("rawtypes")
    public static final TypeArgInfo<LinkedHashMap> STRING_LINKEDHASHMAP =
            new TypeArgInfo<>(LinkedHashMap.class, LinkedHashMap::new, String.class, Object.class);


    // 工厂方法

    @SuppressWarnings("unchecked")
    @NameIsStable(comment = "生成的代码会调用")
    public static <T> TypeArgInfo<T> of(Class<T> declaredType) {
        if (declaredType == Object.class) {
            return (TypeArgInfo<T>) OBJECT;
        }
        return new TypeArgInfo<>(declaredType);
    }

    @NameIsStable(comment = "生成的代码会调用")
    public static <T> TypeArgInfo<T> of(Class<T> declaredType, Class<?> typeArg1, Class<?> typeArg2) {
        return new TypeArgInfo<>(declaredType, typeArg1, typeArg2);
    }

    @NameIsStable(comment = "生成的代码会调用")
    public static <T> TypeArgInfo<T> of(Class<T> declaredType, Supplier<? extends T> factory, Class<?> typeArg1, Class<?> typeArg2) {
        return new TypeArgInfo<>(declaredType, factory, typeArg1, typeArg2);
    }

    public static <T> TypeArgInfo<T> ofArray(Class<T> declaredType) {
        return new TypeArgInfo<>(declaredType, null, null);
    }

    @SuppressWarnings("rawtypes")
    public static <T extends Collection> TypeArgInfo<T> ofCollection(Class<T> declaredType, Supplier<? extends T> factory,
                                                                     Class<?> elementType) {
        return new TypeArgInfo<>(declaredType, factory, elementType, null);
    }

    @SuppressWarnings("rawtypes")
    public static <T extends Map> TypeArgInfo<T> ofMap(Class<T> declaredType, Supplier<? extends T> factory,
                                                       Class<?> keyType, Class<?> valueType) {
        return new TypeArgInfo<>(declaredType, factory, keyType, valueType);
    }

}