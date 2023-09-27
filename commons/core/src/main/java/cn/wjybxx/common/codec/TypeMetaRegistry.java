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

import javax.annotation.Nullable;
import javax.annotation.concurrent.ThreadSafe;
import java.util.List;

/**
 * 类型id注册表
 * <p>
 * 注意：
 * 1. 必须保证同一个类在所有机器上的映射结果是相同的，这意味着你应该基于名字映射，而不能直接使用class对象的hash值。
 * 2. 一个类型{@link Class}的名字和唯一标识应尽量是稳定的，即同一个类的映射值在不同版本之间是相同的。
 * 3. id和类型之间应当是唯一映射的。
 * 4. 需要实现为线程安全的，建议实现为不可变对象（或事实不可变对象）
 *
 * @author wjybxx
 * date - 2023/4/26
 */
@ThreadSafe
public interface TypeMetaRegistry {

    /**
     * 通过类型获取类型的默认id标识
     */
    @Nullable
    TypeMeta ofType(Class<?> type);

    /**
     * 通过数字id查找类型信息
     */
    @Nullable
    TypeMeta ofId(ClassId classId);

    /**
     * 通过字符串名字找到类型信息
     */
    TypeMeta ofName(String clsName);

    default TypeMeta checkedOfType(Class<?> type) {
        TypeMeta r = ofType(type);
        if (r == null) {
            throw new DsonCodecException("classId is absent, type " + type);
        }
        return r;
    }

    default TypeMeta checkedOfId(ClassId classId) {
        TypeMeta r = ofId(classId);
        if (r == null) {
            throw new DsonCodecException("type is absent, classId " + classId);
        }
        return r;
    }

    default TypeMeta checkedOfName(String clsName) {
        TypeMeta r = ofName(clsName);
        if (r == null) {
            throw new DsonCodecException("type is absent, clsName " + clsName);
        }
        return r;
    }

    /**
     * 该方法的主要目的在于聚合多个Registry为单个Registry，以提高查询效率
     */
    List<TypeMeta> export();

}