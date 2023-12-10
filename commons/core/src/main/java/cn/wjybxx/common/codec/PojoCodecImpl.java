package cn.wjybxx.common.codec;

import cn.wjybxx.common.codec.binary.BinaryObjectReader;
import cn.wjybxx.common.codec.binary.BinaryObjectWriter;
import cn.wjybxx.common.codec.binary.BinaryPojoCodecImpl;
import cn.wjybxx.common.codec.binary.BinaryPojoCodecScanIgnore;
import cn.wjybxx.common.codec.document.DocumentObjectReader;
import cn.wjybxx.common.codec.document.DocumentObjectWriter;
import cn.wjybxx.common.codec.document.DocumentPojoCodecImpl;
import cn.wjybxx.common.codec.document.DocumentPojoCodecScanIgnore;
import cn.wjybxx.dson.text.ObjectStyle;

import javax.annotation.Nonnull;

/**
 * 我们让生成的代码都实现该类，以减少生成的类数量
 * <p>
 * 1.继承的方法需要再次声明，以便注解处理器查找
 * 2.生成的代码不支持响应的序列化时，会自动添加响应的注解{@link BinaryPojoCodecScanIgnore}{@link DocumentPojoCodecScanIgnore}
 *
 * @author wjybxx
 * date - 2023/12/10
 */
public interface PojoCodecImpl<T> extends BinaryPojoCodecImpl<T>, DocumentPojoCodecImpl<T> {

    @Nonnull
    @Override
    Class<T> getEncoderClass();

    // Binary
    @Override
    void writeObject(BinaryObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo);

    @Override
    T readObject(BinaryObjectReader reader, TypeArgInfo<?> typeArgInfo);

    // Document
    @Override
    void writeObject(DocumentObjectWriter writer, T instance, TypeArgInfo<?> typeArgInfo, ObjectStyle style);

    @Override
    T readObject(DocumentObjectReader reader, TypeArgInfo<?> typeArgInfo);

    //
    @Override
    default boolean isWriteAsArray() {
        return ConverterUtils.isEncodeAsArray(getEncoderClass());
    }

    @Override
    default boolean autoStartEnd() {
        return true;
    }

}