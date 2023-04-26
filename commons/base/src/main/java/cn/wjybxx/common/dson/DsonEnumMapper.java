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

import javax.annotation.Nullable;
import java.util.List;

/**
 * @author wjybxx
 * date 2023/4/1
 */
public interface DsonEnumMapper<T extends DsonEnum> {

    /**
     * 获取映射的所有枚举实例（声明序）。
     *
     * @return 不可变的list，以支持共享。
     */
    List<T> values();

    /**
     * 获取有序的所有枚举实例(根据number排序)
     *
     * @return 不可变的list，以支持共享。
     */
    List<T> sortedValues();

    /**
     * 枚举对应的数字是否是连续的
     * 注意：如果集合为空这里也将返回true，就像我们认为空集合是有序的一样。
     * <p>
     * Q：该方法的作用？
     * A：我们在某些场景要求枚举的数字必须是连续的，以支持一些优化，缓存在这里可以避免运行时的遍历检查。
     *
     * @return 枚举值是否连续
     */
    boolean isContinuous();

    /**
     * 通过数字找到对应的枚举
     *
     * @param number 枚举的唯一编号
     * @return T 如果不存在，则返回null，而不是抛出异常
     */
    @Nullable
    T forNumber(int number);

    /**
     * 通过数字找到对应的枚举
     *
     * @param number 枚举的唯一编号
     * @return T number对应的枚举
     * @throws IllegalArgumentException 如果number对应的枚举不存在，则抛出异常
     */
    default T checkedForNumber(int number) {
        final T result = forNumber(number);
        if (null == result) {
            throw new IllegalArgumentException("No enum constant, number " + number);
        }
        return result;
    }

    /**
     * @param number 枚举的唯一编号
     * @param def    默认值
     * @return T number对应的枚举或默认值
     */
    default T forNumber(int number, T def) {
        final T result = forNumber(number);
        return result == null ? def : result;
    }

    default int size() {
        return values().size();
    }

    default boolean isEmpty() {
        return values().isEmpty();
    }

}