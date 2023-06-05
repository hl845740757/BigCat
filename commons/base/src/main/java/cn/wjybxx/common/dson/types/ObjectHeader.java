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

import cn.wjybxx.common.CollectionUtils;

import javax.annotation.concurrent.Immutable;
import java.util.List;

/**
 * 对象头的默认结构体
 *
 * @author wjybxx
 * date - 2023/5/27
 */
@Immutable
public final class ObjectHeader {

    /** 对象的类型名 */
    private final String className;
    /** 数组成员/Object-Value的类型名 */
    private final String compClassName;
    /** 对象的唯一id */
    private final String guid;
    /** 对象的本地id  - 如果为0则不编码 */
    private final long localId;
    /** 字符串自定义标签 */
    private final List<String> tags;

    public ObjectHeader(String className) {
        this(className, null, null, 0);
    }

    public ObjectHeader(String className, String compClassName, String guid, long localId) {
        this.className = className;
        this.compClassName = compClassName;
        this.guid = guid;
        this.localId = localId;
        this.tags = CollectionUtils.newSmallArrayList();
    }

    public String getClassName() {
        return className;
    }

    public String getCompClassName() {
        return compClassName;
    }

    public String getGuid() {
        return guid;
    }

    public long getLocalId() {
        return localId;
    }

    public List<String> getTags() {
        return tags;
    }

    //

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        ObjectHeader that = (ObjectHeader) o;

        if (localId != that.localId) return false;
        if (!className.equals(that.className)) return false;
        return tags.equals(that.tags);
    }

    @Override
    public int hashCode() {
        int result = className.hashCode();
        result = 31 * result + (int) (localId ^ (localId >>> 32));
        result = 31 * result + tags.hashCode();
        return result;
    }

    @Override
    public String toString() {
        return "ObjectHeader{" +
                "className='" + className + '\'' +
                ", localId=" + localId +
                ", tags=" + tags +
                '}';
    }
}