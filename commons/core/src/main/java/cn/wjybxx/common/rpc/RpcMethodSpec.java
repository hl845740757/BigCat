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
package cn.wjybxx.common.rpc;


import cn.wjybxx.common.log.DebugLogFriendlyObject;

import javax.annotation.Nonnull;

/**
 * rpc方法描述信息
 * <p>
 * 实现要求：
 * 1. 能快速定位到调用的方法。
 * 2. 额外数据尽可能的少，包体尽可能的小。
 *
 * @param <V> 用于捕获返回值类型（不要删除）
 * @author wjybxx
 * date 2023/4/1
 */
@SuppressWarnings("unused")
public interface RpcMethodSpec<V> extends DebugLogFriendlyObject {

    // 方便消除生成代码里的奇怪代码

    int getInt(int index);

    long getLong(int index);

    float getFloat(int index);

    double getDouble(int index);

    boolean getBoolean(int index);

    String getString(int index);

    Object getObject(int index);

    //

    /**
     * @return 生成简单的日志信息，包括定位到的方法，以及方法参数个数，但不包含参数的详细信息
     */
    @Nonnull
    @Override
    String toSimpleLog();

    /**
     * @return 生成详细的日志信息，在简单的日志信息基础上，还需要打印详细的参数信息
     */
    @Nonnull
    @Override
    String toDetailLog();

}