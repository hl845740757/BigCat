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

package cn.wjybxx.common.codec.binary;

import cn.wjybxx.common.codec.DsonCodecException;

import javax.annotation.Nullable;
import javax.annotation.concurrent.ThreadSafe;

/**
 * 编解码器注册表
 * <p>
 * 1.需要实现为线程安全的，建议实现为不可变对象（或事实不可变对象）
 * 2.查找codec是一个匹配过程，可以返回兼容类型的codec
 *
 * @author wjybxx
 * date 2023/3/31
 */
@ThreadSafe
public interface BinaryCodecRegistry {

    /**
     * 查找指定class对应的编解码器
     */
    @Nullable
    <T> BinaryPojoCodec<T> get(Class<T> clazz);

    default <T> BinaryPojoCodec<T> checkedGet(Class<T> clazz) {
        BinaryPojoCodec<T> codec = get(clazz);
        if (codec == null) {
            throw new DsonCodecException("codec is absent, clazz" + clazz);
        }
        return codec;
    }

}