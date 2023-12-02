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

package cn.wjybxx.common.collect;

import org.apache.commons.lang3.ArrayUtils;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Collection;
import java.util.Comparator;
import java.util.Objects;
import java.util.function.Consumer;
import java.util.function.ObjIntConsumer;
import java.util.function.Predicate;

/**
 * 迭代期间延迟压缩空间的List，在迭代期间删除元素只会清理元素，不会减少size，而插入元素会添加到List末尾并增加size
 * 1.不支持插入Null -- 理论上做的到，但会导致较高的复杂度，也很少有需要。
 * 2.未实现{@link Iterable}接口，因为不能按照正常方式迭代
 * <h3>使用方式</h3>
 * <pre><code>
 *     list.beginItr();
 *     try {
 *         for(int i = 0, size = list.size();i < size; i++){
 *              E e = list.get(i);
 *              if (e == null) {
 *                  continue;
 *              }
 *              doSomething(e);
 *         }
 *     } finally {
 *         list.endItr();
 *     }
 * </code></pre>
 * PS：
 * 1.该List主要用于事件监听器列表和对象列表等场景。
 * 2.使用{@link #forEach(Consumer)}可能有更好的迭代速度。
 *
 * @author wjybxx
 * date 2023/4/6
 */
public interface DelayedCompressList<E> {

    int INDEX_NOT_FOUND = ArrayUtils.INDEX_NOT_FOUND;

    // 迭代api

    /** 开始迭代 */
    void beginItr();

    /** 迭代结束 -- 必须在finally块中调用，否则可能使List处于无效状态 */
    void endItr();

    /** 当前是否正在迭代 */
    boolean isIterating();

    /** 是否处于延迟压缩状态；是否在迭代期间删除了元素 */
    boolean isDelayed();

    //

    /**
     * @return 如果添加元素成功则返回true
     * @throws NullPointerException 如果e为null
     */
    boolean add(E e);

    /**
     * 获取指定位置的元素
     *
     * @return 如果指定位置的元素已删除，则返回null
     */
    @Nullable
    E get(int index);

    /**
     * 将给定元素赋值到给定位置
     *
     * @return 该位置的前一个值
     * @throws NullPointerException 如果e为null
     */
    E set(int index, E e);

    /**
     * 根据equals相等删除元素
     *
     * @return 如果元素在集合中则删除并返回true
     */
    default boolean remove(Object e) {
        if (e == null) return false;
        int i = index(e);
        if (i >= 0) {
            removeAt(i);
            return true;
        }
        return false;
    }

    /**
     * 根据引用相等删除元素
     *
     * @return 如果元素在集合中则删除并返回true
     */
    default boolean removeRef(Object e) {
        if (e == null) return false;
        int i = indexOfRef(e);
        if (i >= 0) {
            removeAt(i);
            return true;
        }
        return false;
    }

    /**
     * 删除给定位置的元素
     *
     * @return 如果指定位置存在元素，则返回对应的元素，否则返回Null
     */
    E removeAt(int index);

    /**
     * @apiNote 在迭代期间清理元素不会更新size
     */
    void clear();

    /**
     * 基于equals查找元素在List中的位置
     *
     * @param e 如果null，表示查询第一个删除的的元素位置
     * @return 如果元素不在集合中，则返回-1
     */
    int index(@Nullable Object e);

    /**
     * 基于equals逆向查找元素在List中的位置
     *
     * @param e 如果null，表示查询最后一个删除的的元素位置
     * @return 如果元素不在集合中，则返回-1
     */
    int lastIndex(@Nullable Object e);

    /**
     * 基于引用相等查找元素在List中的位置
     *
     * @param e 如果null，表示查询第一个删除的的元素位置
     * @return 如果元素不在集合中，则返回-1
     */
    int indexOfRef(@Nullable Object e);

    /**
     * 基于引用相等逆向查找元素在List中的位置
     *
     * @param e 如果null，表示查询最后一个删除的的元素位置
     * @return 如果元素不在集合中，则返回-1
     */
    int lastIndexOfRef(@Nullable Object e);

    /** 基于equals查询一个元素是否在List中 */
    default boolean contains(Object e) {
        return index(e) >= 0;
    }

    /** 基于引用相等查询一个元素是否在List中 */
    default boolean containsRef(Object e) {
        return indexOfRef(e) >= 0;
    }

    /**
     * 获取list的当前大小
     * 注意：迭代期间删除的元素并不会导致size变化，因此该值是一个不准确的值。
     */
    int size();

    default boolean isEmpty() {
        return size() == 0;
    }

    /**
     * 获取list的真实大小
     * 如果当前正在迭代，则可能产生遍历统计的情况，要注意开销问题。
     */
    int realSize();

    /**
     * 查询List是否真的为空
     * 如果当前正在迭代，则可能产生遍历统计的情况，要注意开销问题。
     */
    boolean isRealEmpty();

    /**
     * @throws IllegalStateException 如果当前正在迭代
     */
    void sort(@Nonnull Comparator<? super E> comparator);

    // region 辅助方法

    /** 批量添加元素 */
    default boolean addAll(@Nonnull Collection<? extends E> c) {
        boolean r = false;
        for (E e : c) {
            r |= add(e);
        }
        return r;
    }

    /**
     * 自定义index查询；自定义查询时不支持查找null
     *
     * @param predicate 查询过程不可以修改当前List的状态
     */
    default int indexCustom(Predicate<? super E> predicate) {
        Objects.requireNonNull(predicate);
        int size = size();
        if (size == 0) {
            return INDEX_NOT_FOUND;
        }
        for (int index = 0; index < size; index++) {
            final E e = get(index);
            if (e != null && predicate.test(e)) {
                return index;
            }
        }
        return INDEX_NOT_FOUND;
    }

    default int lastIndexCustom(Predicate<? super E> predicate) {
        Objects.requireNonNull(predicate);
        int size = size();
        if (size == 0) {
            return INDEX_NOT_FOUND;
        }
        for (int index = size - 1; index >= 0; index--) {
            final E e = get(index);
            if (e != null && predicate.test(e)) {
                return index;
            }
        }
        return INDEX_NOT_FOUND;
    }

    /**
     * 迭代List内的元素，该快捷方式不会迭代迭代期间新增的元素
     * 如果需要元素的下标，请使用{@link #forEach(ObjIntConsumer)}
     */
    default void forEach(Consumer<? super E> action) {
        Objects.requireNonNull(action);
        int size = size();
        if (size == 0) {
            return;
        }
        beginItr();
        try {
            for (int index = 0; index < size; index++) {
                final E e = get(index);
                if (e != null) {
                    action.accept(e);
                }
            }
        } finally {
            endItr();
        }
    }

    /**
     * 迭代List内的元素，该快捷方式不会迭代迭代期间新增的元素
     *
     * @param action 参数1为对应元素，参数2为下标 -- 返回index以方便快速删除
     */
    default void forEach(ObjIntConsumer<? super E> action) {
        Objects.requireNonNull(action);
        int size = size();
        if (size == 0) {
            return;
        }
        beginItr();
        try {
            for (int index = 0; index < size; index++) {
                final E e = get(index);
                if (e != null) {
                    action.accept(e, index);
                }
            }
        } finally {
            endItr();
        }
    }

    /**
     * 将List中的元素写入目标集合
     *
     * @param out 结果集
     * @return 添加的元素个数
     */
    default int collectTo(Collection<? super E> out) {
        Objects.requireNonNull(out);
        int size = size();
        if (size == 0) {
            return 0;
        }
        int count = 0;
        for (int index = 0; index < size; index++) {
            final E e = get(index);
            if (e != null) {
                out.add(e);
                count++;
            }
        }
        return count;
    }

    // endregion

}