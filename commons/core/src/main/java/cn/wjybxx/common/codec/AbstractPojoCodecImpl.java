package cn.wjybxx.common.codec;

import cn.wjybxx.common.codec.binary.BinaryObjectReader;
import cn.wjybxx.common.codec.binary.BinaryObjectWriter;
import cn.wjybxx.common.codec.document.AbstractDocumentPojoCodecImpl;

import java.util.function.Supplier;

/**
 * 继承{@link AbstractDocumentPojoCodecImpl}而不是二进制的模板类，是因我们自定义解析文档型编码的情况更多。
 *
 * @author wjybxx
 * date - 2023/12/10
 */
@SuppressWarnings("unused")
public abstract class AbstractPojoCodecImpl<T> extends AbstractDocumentPojoCodecImpl<T> implements PojoCodecImpl<T> {

    @Override
    public final T readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo) {
        @SuppressWarnings("unchecked") Supplier<? extends T> factory = (Supplier<? extends T>) typeArgInfo.factory;
        final T instance;
        if (factory != null) {
            instance = factory.get();
        } else {
            instance = newInstance(reader, typeArgInfo);
        }
        readFields(reader, instance, typeArgInfo);
        afterDecode(reader, instance, typeArgInfo);
        return instance;
    }

    /**
     * 创建一个对象，如果是一个抽象类，应该抛出异常
     */
    protected abstract T newInstance(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo);

    /**
     * 从输入流中读取所有序列化的字段到指定实例上。
     *
     * @param instance 可以是子类实例
     */
    public abstract void readFields(BinaryObjectReader reader, T instance, TypeArgInfo<?> typeArgInfo);

    /**
     * 用于执行用户序列化完成的钩子方法
     */
    protected void afterDecode(BinaryObjectReader reader, T instance, TypeArgInfo<?> typeArgInfo) {

    }

    @Override
    public abstract void writeObject(BinaryObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo);

}