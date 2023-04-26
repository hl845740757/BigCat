package cn.wjybxx.common.dson.codec;

import cn.wjybxx.common.dson.ClassId;

import javax.annotation.Nonnull;

/**
 * 类型id映射函数
 * <p>
 * 1.classId和类型之间应当是唯一映射的，如果是字符串别名，应尽量保持简短
 * 2.在文档型编解码中，可读性是比较重要的，因此
 * 3.提供Mapper主要为方便通过算法映射
 *
 * @author wjybxx
 * date - 2023/4/26
 */
@FunctionalInterface
public interface ClassIdMapper<T extends ClassId> {

    @Nonnull
    T map(Class<?> type);

}