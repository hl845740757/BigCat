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

package cn.wjybxx.bigcat.common.codec.binary;

import javax.annotation.Nullable;
import javax.annotation.concurrent.ThreadSafe;

/**
 * 编解码器注册表
 * 需要实现为线程安全的，建议实现为不可变对象（或事实不可变对象）
 *
 * @author wjybxx
 * date 2023/3/31
 */
@ThreadSafe
public interface BinaryCodecRegistry {

    /**
     * 获取指定类class对应的编解码器
     */
    @Nullable
    <T> BinaryPojoCodec<T> get(Class<T> clazz);

    default <T> BinaryPojoCodec<T> checkedGet(Class<T> clazz) {
        BinaryPojoCodec<T> codec = get(clazz);
        if (codec == null) {
            throw new RuntimeException("codec is absent, clazz" + clazz);
        }
        return codec;
    }

}