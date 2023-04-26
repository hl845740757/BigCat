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

package cn.wjybxx.common.dson.binary;

import cn.wjybxx.common.dson.BinClassId;
import cn.wjybxx.common.dson.DsonCodecException;

import javax.annotation.Nullable;
import javax.annotation.concurrent.ThreadSafe;
import java.util.Map;

/**
 * 类型映射器。
 * 注意：
 * 1. 必须保证同一个类在所有机器上的映射结果是相同的，这意味着你应该基于名字映射，而不能直接使用class对象的hash值。
 * 2. 一个类型{@link Class}的名字和唯一标识应尽量是稳定的，即同一个类的映射值在不同版本之间是相同的。
 * 3. id和类型之间应当是唯一映射的。
 * 4. 需要实现为线程安全的，建议实现为不可变对象（或事实不可变对象）
 *
 * @author wjybxx
 * date 2023/3/31
 */
@ThreadSafe
public interface TypeIdRegistry {

    /**
     * 通过类型获取数字标识符
     *
     * @return 如果不存在则返回null
     */
    @Nullable
    BinClassId ofType(Class<?> type);

    /**
     * 通过数字id找到类型信息
     *
     * @return 如果不存在则返回null
     */
    @Nullable
    Class<?> ofId(BinClassId classId);

    default BinClassId checkedOfType(Class<?> type) {
        BinClassId classId = ofType(type);
        if (classId == null) {
            throw new DsonCodecException("classId is absent, type " + type.getName());
        }
        return classId;
    }

    default Class<?> checkedOfId(BinClassId classId) {
        Class<?> type = ofId(classId);
        if (type == null) {
            throw new DsonCodecException("type is absent, classId " + classId);
        }
        return type;
    }

    /**
     * 导出为一个Map结构
     * 该方法的主要目的在于聚合多个Registry为单个Registry，以提高查询效率
     */
    Map<Class<?>, BinClassId> export();

}