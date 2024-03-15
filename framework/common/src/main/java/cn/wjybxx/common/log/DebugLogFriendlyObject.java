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

package cn.wjybxx.common.log;

import javax.annotation.Nonnull;

/**
 * 对debug友好的对象
 * 如果一个对象实现了该接口，则在打印日志时会根据日志环境调用对应的方法，如果没有实现该对象，则始终调用对象的{@link #toString()}方法。
 *
 * @author wjybxx
 * date 2023/4/1
 */
public interface DebugLogFriendlyObject {

    /**
     * @return 生成简单的日志信息，关键信息必须包含
     */
    @Nonnull
    String toSimpleLog();

    /**
     * @return 生成详细的日志信息，通常可以调用{@link #toString()}方法。
     */
    @Nonnull
    String toDetailLog();

}