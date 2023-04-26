package cn.wjybxx.common.dson.codec;

import cn.wjybxx.common.dson.ClassId;
import cn.wjybxx.common.dson.DsonCodecException;

import javax.annotation.Nullable;
import javax.annotation.concurrent.ThreadSafe;
import java.util.Map;

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
public interface ClassIdRegistry<T extends ClassId> {

    /**
     * 通过类型获取类型的字符串标识
     */
    @Nullable
    T ofType(Class<?> type);

    /**
     * 通过字符串名字找到类型信息
     */
    @Nullable
    Class<?> ofId(T classId);

    default T checkedOfType(Class<?> type) {
        T r = ofType(type);
        if (r == null) {
            throw new DsonCodecException("classId is absent, type " + type);
        }
        return r;
    }

    default Class<?> checkedOfId(T classId) {
        Class<?> r = ofId(classId);
        if (r == null) {
            throw new DsonCodecException("type is absent, classId " + classId);
        }
        return r;
    }

    /**
     * 导出为一个Map结构
     * 该方法的主要目的在于聚合多个Registry为单个Registry，以提高查询效率
     */
    Map<Class<?>, T> export();

}