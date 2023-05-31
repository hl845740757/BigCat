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

import cn.wjybxx.common.ObjectUtils;

import java.util.ArrayList;
import java.util.List;

/**
 * 对象头的默认结构体，可继承
 * 默认的实现是不可变对象，子类则不一定
 *
 * @author wjybxx
 * date - 2023/5/27
 */
public class ObjectHeader {

    /** 对象的类型信息 - 在编码时建议平铺，避免不必要的嵌套 */
    private final ClassDesc classDesc;
    /** 对象的本地id  - 如果为0则不编码 */
    private final long localId;

    /** 对象的标记 */
    private int flags;
    /** 字符串自定义标签 */
    private final List<String> tags;

    public ObjectHeader(ClassDesc classDesc) {
        this(classDesc, 0);
    }

    public ObjectHeader(ClassDesc classDesc, long localId) {
        this.classDesc = ObjectUtils.nullToDef(classDesc, ClassDesc.OBJECT);
        this.localId = localId;
        this.flags = 0;
        this.tags = new ArrayList<>();
    }

    public ClassDesc getClassDesc() {
        return classDesc;
    }

    public long getLocalId() {
        return localId;
    }

    public int getFlags() {
        return flags;
    }

    public ObjectHeader setFlags(int flags) {
        this.flags = flags;
        return this;
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
        if (flags != that.flags) return false;
        return classDesc.equals(that.classDesc);
    }

    @Override
    public int hashCode() {
        int result = classDesc.hashCode();
        result = 31 * result + (int) (localId ^ (localId >>> 32));
        result = 31 * result + flags;
        return result;
    }

    @Override
    public String toString() {
        return "ObjectHeader{" +
                "classDesc=" + classDesc +
                ", localId=" + localId +
                ", flags=" + flags +
                '}';
    }
}