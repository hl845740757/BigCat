package cn.wjybxx.common.dson;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/4/28
 */
public interface DsonValueLite {

    @Nonnull
    DsonType getDsonType();

}