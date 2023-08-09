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

import cn.wjybxx.dson.text.ObjectStyle;

import java.util.List;
import java.util.Objects;

/**
 * 类型的元数据
 * 不使用Schema这样的东西，是因为Schema包含的信息太多，难以手动维护。
 * 另外，Schema是属于Codec的一部分，是低层次的数据，而TypeMeta是更高层的配置。
 *
 * @author wjybxx
 * date - 2023/7/29
 */
public class TypeMeta<T> {

    public final Class<?> clazz;
    /** 文本编码时的输出格式 */
    public final ObjectStyle style;
    /** 支持的类型id -- 为空将被忽略 */
    public final List<T> classIds;

    public TypeMeta(Class<?> clazz, ObjectStyle style, List<T> classIds) {
        this.clazz = Objects.requireNonNull(clazz);
        this.style = Objects.requireNonNull(style);
        this.classIds = Objects.requireNonNull(classIds);
    }

    public T mainClassId() {
        return classIds.get(0);
    }

    public TypeMeta<T> toImmutable() {
        return new TypeMeta<>(clazz, style, List.copyOf(classIds));
    }

    public static <T> TypeMeta<T> of(Class<?> clazz, T classId) {
        return new TypeMeta<>(clazz, ObjectStyle.INDENT, List.of(classId));
    }

    @SafeVarargs
    public static <T> TypeMeta<T> of(Class<?> clazz, T... classId) {
        return new TypeMeta<>(clazz, ObjectStyle.INDENT, List.of(classId));
    }

    public static <T> TypeMeta<T> of(Class<?> clazz, List<T> classId) {
        return new TypeMeta<>(clazz, ObjectStyle.INDENT, classId);
    }

    public static <T> TypeMeta<T> of(Class<?> clazz, ObjectStyle style, T classId) {
        return new TypeMeta<>(clazz, style, List.of(classId));
    }

    @SafeVarargs
    public static <T> TypeMeta<T> of(Class<?> clazz, ObjectStyle style, T... classId) {
        return new TypeMeta<>(clazz, style, List.of(classId));
    }

    public static <T> TypeMeta<T> of(Class<?> clazz, ObjectStyle style, List<T> classId) {
        return new TypeMeta<>(clazz, style, classId);
    }

    @Override
    public String toString() {
        return "TypeMeta{" +
                "clazz=" + clazz +
                ", style=" + style +
                ", classIds=" + classIds +
                '}';
    }
}