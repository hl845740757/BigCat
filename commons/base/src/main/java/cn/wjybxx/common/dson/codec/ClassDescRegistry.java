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

import cn.wjybxx.common.dson.DsonCodecException;

import javax.annotation.Nullable;
import java.util.Map;

/**
 * @author wjybxx
 * date - 2023/5/28
 */
public interface ClassDescRegistry {

    /** 通过类型查询类型描述 */
    @Nullable
    ClassDesc ofType(Class<?> type);

    /** 通过数字id查找类型描述 */
    @Nullable
    ClassDesc ofId(long classId);

    /** 通过字符串名字找到类型描述 */
    @Nullable
    ClassDesc ofName(String name);

    @Nullable
    Class<?> typeOfId(long classId);

    @Nullable
    Class<?> typeOfName(String name);

    default ClassDesc checkedOfType(Class<?> type) {
        ClassDesc r = ofType(type);
        if (r == null) {
            throw new DsonCodecException("classId is absent, type " + type);
        }
        return r;
    }

    default ClassDesc checkedOfId(long classId) {
        ClassDesc r = ofId(classId);
        if (r == null) {
            throw new DsonCodecException("type is absent, classId " + classId);
        }
        return r;
    }

    default ClassDesc checkedOfName(String name) {
        ClassDesc r = ofName(name);
        if (r == null) {
            throw new DsonCodecException("type is absent, name " + name);
        }
        return r;
    }

    default Class<?> checkedTypeOfId(int classId) {
        Class<?> r = typeOfId(classId);
        if (r == null) {
            throw new DsonCodecException("type is absent, classId " + classId);
        }
        return r;
    }

    default Class<?> checkedTypeOfName(String name) {
        Class<?> r = typeOfName(name);
        if (r == null) {
            throw new DsonCodecException("type is absent, name " + name);
        }
        return r;
    }

    /**
     * 导出为一个Map结构
     * 该方法的主要目的在于聚合多个Registry为单个Registry，以提高查询效率
     */
    Map<Class<?>, ClassDesc> export();

}