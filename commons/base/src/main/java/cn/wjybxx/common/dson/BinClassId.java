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

package cn.wjybxx.common.dson;

/**
 * 在二进制编码中，包体大小是比较重要的，因此使用数字来映射类型
 *
 * @author wjybxx
 * date 2023/3/31
 */
public class BinClassId implements ClassId {

    /** 默认命名空间 */
    public static final int DEFAULT_NAMESPACE = 0;
    /** 无效命名空间 */
    public static final int INVALID_NAMESPACE = 255;
    /** 对象的默认的类型id */
    public static final BinClassId OBJECT = new BinClassId(0, 0);

    /**
     * 1. 当使用算法生成id时可以减少冲突。
     * 2. 可以表示来源。
     * 3. 用户的命名空间都应该大于0
     * 4. 0为保留命名空间（底层使用）
     */
    private final int namespace;
    /**
     * localClassId - id应尽量保持稳定。
     * 最简单的方式是计算类的简单名的hash，{@link Class#getSimpleName}
     */
    private final int lclassId;

    /**
     * @param namespace 0是保留命名空间，用户应避免使用
     */
    public BinClassId(int namespace, int lclassId) {
        if (namespace < 0 || namespace >= INVALID_NAMESPACE) {
            throw new IllegalArgumentException("invalid namespace " + namespace);
        }
        this.namespace = namespace;
        this.lclassId = lclassId;
    }

    public int getNamespace() {
        return namespace;
    }

    public int getLclassId() {
        return lclassId;
    }

    public long getGuid() {
        return Dsons.makeClassGuid(namespace, lclassId);
    }

    public boolean isObjectClassId() {
        if (this == OBJECT) {
            return true;
        }
        return namespace == 0 && lclassId == 0;
    }

    public boolean isDefaultNameSpace() {
        return namespace == 0;
    }

    public static BinClassId ofGuid(long guid) {
        int namespace = Dsons.namespaceOfClassGuid(guid);
        int lclassId = Dsons.lclassIdOfClassGuid(guid);
        return new BinClassId(namespace, lclassId);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) {
            return true;
        }

        if (o == null || getClass() != o.getClass()) {
            return false;
        }

        BinClassId that = (BinClassId) o;
        return namespace == that.namespace && lclassId == that.lclassId;
    }

    @Override
    public int hashCode() {
        return 31 * namespace + lclassId;
    }

    @Override
    public String toString() {
        return "BinClassId{" +
                "namespace=" + namespace +
                ", classId=" + lclassId +
                '}';
    }

}