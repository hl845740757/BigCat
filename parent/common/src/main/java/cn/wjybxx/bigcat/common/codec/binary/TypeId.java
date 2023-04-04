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

package cn.wjybxx.bigcat.common.codec.binary;

import cn.wjybxx.bigcat.common.CommonMathUtils;

/**
 * @author wjybxx
 * date 2023/3/31
 */
public class TypeId {

    /**
     * 1. 当使用算法生成id时可以减少冲突。
     * 2. 可以表示来源。
     * 3. 0为保留命名空间（底层使用）
     */
    private final byte namespace;
    /**
     * class尽量保持稳定。
     * 最简单的方式是计算类的简单名的hash。 {@link Class#getSimpleName()}
     */
    private final int classId;

    /**
     * @param namespace 0是保留命名空间，用户应避免使用
     */
    public TypeId(byte namespace, int classId) {
        if (namespace < 0) {
            throw new IllegalArgumentException("invalid namespace");
        }
        if (namespace == 0 && classId == 0) {
            throw new IllegalArgumentException("invalid typeId{0,0}");
        }
        this.namespace = namespace;
        this.classId = classId;
    }

    public TypeId(int namespace, int classId) {
        this((byte) namespace, classId);
    }

    public byte getNamespace() {
        return namespace;
    }

    public int getClassId() {
        return classId;
    }

    public long toGuid() {
        return CommonMathUtils.composeIntToLong(namespace, classId);
    }

    public static TypeId ofGuid(long guid) {
        byte namespace = (byte) CommonMathUtils.higherIntOfLong(guid);
        int classId = CommonMathUtils.lowerIntOfLong(guid);
        return new TypeId(namespace, classId);
    }

    public static byte parseNamespace(long guid) {
        return (byte) CommonMathUtils.higherIntOfLong(guid);
    }

    public static int parseClassId(long guid) {
        return CommonMathUtils.lowerIntOfLong(guid);
    }

    public static long toGuid(byte namespace, int classId) {
        return CommonMathUtils.composeIntToLong(namespace, classId);
    }

    /** 是否是默认空间 */
    public static boolean isDefaultNameSpace(long guid) {
        // 其实可以通过大小测试决定
        return CommonMathUtils.higherIntOfLong(guid) == 0;
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) {
            return true;
        }

        if (o == null || getClass() != o.getClass()) {
            return false;
        }

        TypeId that = (TypeId) o;
        return namespace == that.namespace && classId == that.classId;
    }

    @Override
    public int hashCode() {
        return 31 * (int) namespace + classId;
    }

    @Override
    public String toString() {
        return "TypeId{" +
                "namespace=" + namespace +
                ", classId=" + classId +
                '}';
    }

}