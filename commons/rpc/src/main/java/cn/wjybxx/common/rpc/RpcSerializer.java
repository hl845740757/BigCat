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

package cn.wjybxx.common.rpc;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.ThreadSafe;

/**
 * 用于Rpc通信序列化对象
 *
 * @author wjybxx
 * date - 2023/10/28
 */
@ThreadSafe
public interface RpcSerializer {

    /** 类型信息需要写入最终的bytes，进行自解释 */
    @Nonnull
    byte[] write(@Nonnull Object value);

    Object read(@Nonnull byte[] source);

}