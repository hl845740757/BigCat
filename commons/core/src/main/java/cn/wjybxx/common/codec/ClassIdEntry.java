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

import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/7/29
 */
public class ClassIdEntry<T> {

    public final Class<?> clazz;
    public final List<T> classIds;

    public ClassIdEntry(Class<?> clazz, List<T> classIds) {
        this.clazz = Objects.requireNonNull(clazz);
        this.classIds = Objects.requireNonNull(classIds);
    }

    public ClassIdEntry<T> toImmutable() {
        return new ClassIdEntry<>(clazz, List.copyOf(classIds));
    }

    public static <T> ClassIdEntry<T> of(Class<?> clazz, T classId) {
        return new ClassIdEntry<>(clazz, List.of(classId));
    }

    @SafeVarargs
    public static <T> ClassIdEntry<T> of(Class<?> clazz, T... classId) {
        return new ClassIdEntry<>(clazz, List.of(classId));
    }

    public static <T> ClassIdEntry<T> of(Class<?> clazz, List<T> classId) {
        return new ClassIdEntry<>(clazz, classId);
    }

    @Override
    public String toString() {
        return "ClassIdEntry{" +
                "clazz=" + clazz +
                ", classIds=" + classIds +
                '}';
    }
}