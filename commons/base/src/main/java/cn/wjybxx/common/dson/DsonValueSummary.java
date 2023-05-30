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

package cn.wjybxx.common.dson;

import java.util.Objects;

/**
 * 一个{@link DsonValue}的摘要信息
 *
 * @author wjybxx
 * date - 2023/4/23
 */
public final class DsonValueSummary {

    private final DsonType dsonType;
    private final int length;
    private final byte subType;
    private final ClassId classId;

    public DsonValueSummary(DsonType dsonType, int length, byte subType, ClassId classId) {
        this.dsonType = Objects.requireNonNull(dsonType);
        this.length = length;
        this.subType = subType;
        this.classId = classId;
    }

    public DsonType getDsonType() {
        return dsonType;
    }

    /** 数据的长度，注意{@link DsonType#EXT_STRING}的长度不包含 subType */
    public int getLength() {
        return length;
    }

    /** 数据子类型 */
    public byte getSubType() {
        return subType;
    }

    /** 如果当前是一个Array或Object，则有该值 */
    public ClassId getClassId() {
        return classId;
    }

    //

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonValueSummary that = (DsonValueSummary) o;

        if (length != that.length) return false;
        if (subType != that.subType) return false;
        if (dsonType != that.dsonType) return false;
        return Objects.equals(classId, that.classId);
    }

    @Override
    public int hashCode() {
        int result = dsonType.hashCode();
        result = 31 * result + length;
        result = 31 * result + (int) subType;
        result = 31 * result + (classId != null ? classId.hashCode() : 0);
        return result;
    }

    @Override
    public String toString() {
        return "DsonValueSummary{" +
                "dsonType=" + dsonType +
                ", length=" + length +
                ", subType=" + subType +
                ", classId=" + classId +
                '}';
    }
}