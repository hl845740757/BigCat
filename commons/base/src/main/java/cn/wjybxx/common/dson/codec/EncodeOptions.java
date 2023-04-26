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

package cn.wjybxx.common.dson.codec;

import cn.wjybxx.common.OptionalBool;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.Immutable;
import java.util.Collection;
import java.util.Map;

/**
 * @author wjybxx
 * date - 2023/4/17
 */
@Immutable
public class EncodeOptions {

    public static final EncodeOptions DEFAULT = new EncodeOptions(null, OptionalBool.EMPTY);
    public static final EncodeOptions ARRAY = new EncodeOptions(Collection.class, OptionalBool.FALSE);
    public static final EncodeOptions MAP = new EncodeOptions(Map.class, OptionalBool.TRUE);
    public static final EncodeOptions MAP_SKIP_NULL = new EncodeOptions(Map.class, OptionalBool.FALSE);

    /**
     * 上下文类型
     * 如果为null，则表示由对象的运行时类型动态确定；
     * 如果为{@link Collection}表示写为数组
     * 如果为{@link Map}表示写为KV对象结构
     */
    public final Class<?> contextClass;
    /**
     * 是否写入对象内的null值
     * 1.只在写文档上下文中生效
     * 2.对于一般的对象可不写入，但对于Map这样存在containsKey测试的数据结构，则必须写入。
     * 3.默认根据容器对象是否是Map的子类型进行判断
     */
    public final OptionalBool appendNull;

    public EncodeOptions(@Nullable Class<?> contextClass, @Nonnull OptionalBool appendNull) {
        this.contextClass = contextClass;
        this.appendNull = appendNull;
    }

}