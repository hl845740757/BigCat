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

package cn.wjybxx.common.dson.codec;

import cn.wjybxx.common.ObjectUtils;
import cn.wjybxx.common.Preconditions;
import org.apache.commons.lang3.StringUtils;

import java.util.Objects;

/**
 * 类型描述
 * 注意：这里equals相等，则真实类型一定相等；这里equals不相同，不能推出真实类型不相等，
 * 因为解码的时候可能信息不全，可能只包含两个值中的某一个。
 *
 * @author wjybxx
 * date - 2023/5/27
 */
public final class ClassDesc {

    public static final ClassDesc OBJECT = new ClassDesc(null, 0);

    /** 对象的类型名，可以使用点号(.)表达不同的命名空间 */
    private final String className;
    /** 对象的类型id -- {@link ClassId} */
    private final long classId;

    public ClassDesc(String className, ClassId classId) {
        this(className, classId.getGuid());
    }

    public ClassDesc(String className, long classId) {
        this.className = ObjectUtils.nullToEmpty(Preconditions.checkNotContainsWhiteSpace(className, null));
        this.classId = classId;
    }

    public boolean hasClassName() {
        return !StringUtils.isEmpty(className);
    }

    public boolean hasClassId() {
        return classId != 0;
    }

    public String getClassName() {
        return className;
    }

    public long getClassId() {
        return classId;
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        ClassDesc that = (ClassDesc) o;

        if (classId != that.classId) return false;
        return Objects.equals(className, that.className);
    }

    @Override
    public int hashCode() {
        int result = className != null ? className.hashCode() : 0;
        result = 31 * result + (int) (classId ^ (classId >>> 32));
        return result;
    }

    @Override
    public String toString() {
        return "ClassDesc{" +
                "className='" + className + '\'' +
                ", classId=" + classId +
                '}';
    }
}