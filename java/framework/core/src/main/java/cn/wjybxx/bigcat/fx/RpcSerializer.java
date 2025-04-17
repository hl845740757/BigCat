/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.bigcat.fx;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.ThreadSafe;

/**
 * 用于Rpc通信序列化对象
 * TODO 改为返回Bytebuf
 *
 * @author wjybxx
 * date - 2023/10/28
 */
@ThreadSafe
public interface RpcSerializer {

    /***
     * 序列化
     *
     * @param value 要序列化的对象
     * @param declaredType 对象的声明类型（方法参数或返回值的声明类型）；非泛型
     */
    @Nonnull
    byte[] write(@Nonnull Object value, Class<?> declaredType);

    /**
     * 反序列化
     *
     * @param source       字节数组
     * @param declaredType 对象的声明类型（方法参数或返回值的声明类型）；非泛型
     */
    Object read(@Nonnull byte[] source, Class<?> declaredType);

}