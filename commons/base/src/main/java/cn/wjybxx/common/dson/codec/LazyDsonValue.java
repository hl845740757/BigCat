package cn.wjybxx.common.dson.codec;

import cn.wjybxx.common.dson.DsonType;
import cn.wjybxx.common.dson.DsonValueLite;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * 用户延迟解析
 *
 * @author wjybxx
 * date - 2023/4/28
 */
public final class LazyDsonValue implements DsonValueLite {

    private final DsonType dsonType;
    /** data只包含数据部分，不包含长度信息 */
    private final byte[] data;

    LazyDsonValue(DsonType dsonType, byte[] data) {
        this.dsonType = dsonType;
        this.data = Objects.requireNonNull(data);
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return dsonType;
    }

    public int getLength() {
        return data.length;
    }

    public byte[] getData() {
        return data;
    }

}