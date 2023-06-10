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

package cn.wjybxx.common.dson.types;

import org.apache.commons.lang3.StringUtils;

import javax.annotation.concurrent.Immutable;
import java.util.Objects;

/**
 * 对象引用的默认结构体
 * 注意：相等性比较时只比较 guid 和 localId，只要指向的是同一个对象就认为相等
 *
 * @author wjybxx
 * date - 2023/5/26
 */
@Immutable
public final class ObjectRef {

    public static final String FIELDS_NAMESPACE = "namespace";
    public static final String FIELDS_LOCAL_ID = "localId";
    public static final String FIELDS_TYPE = "type";
    public static final String FIELDS_POLICY = "policy";

    /** 引用对象所属的命名空间 - 也可能就是对象的id */
    private final String namespace;
    /** 引用对象的本地id - 如果目标对象是容器中的一员，该值是其容器内编号 */
    private final String localId;
    /** 引用的对象的大类型 - 给业务使用的，用于快速引用分析 */
    private final int type;
    /** 引用的解析策略 - 0：默认 1：解析为引用 2：内联复制，3：不解析 */
    private final int policy;

    public ObjectRef(String namespace, String localId) {
        this(namespace, localId, 0, 0);
    }

    public ObjectRef(String namespace, String localId, int type, int policy) {
        if (StringUtils.isBlank(namespace) && StringUtils.isBlank(localId)) {
            throw new IllegalArgumentException("both namespace and localId are blank");
        }
        this.localId = localId;
        this.namespace = namespace;
        this.type = type;
        this.policy = policy;
    }

    public boolean hasNamespace() {
        return !StringUtils.isBlank(namespace);
    }

    public boolean hasLocalId() {
        return !StringUtils.isBlank(localId);
    }

    public String getLocalId() {
        return localId;
    }

    public String getNamespace() {
        return namespace;
    }

    public int getPolicy() {
        return policy;
    }

    public int getType() {
        return type;
    }

    //

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        ObjectRef objectRef = (ObjectRef) o;

        if (type != objectRef.type) return false;
        if (policy != objectRef.policy) return false;
        if (!Objects.equals(namespace, objectRef.namespace)) return false;
        return Objects.equals(localId, objectRef.localId);
    }

    @Override
    public int hashCode() {
        int result = namespace != null ? namespace.hashCode() : 0;
        result = 31 * result + (localId != null ? localId.hashCode() : 0);
        result = 31 * result + type;
        result = 31 * result + policy;
        return result;
    }

    @Override
    public String toString() {
        return "ObjectRef{" +
                "namespace='" + namespace + '\'' +
                ", localId='" + localId + '\'' +
                ", type=" + type +
                ", policy=" + policy +
                '}';
    }
}