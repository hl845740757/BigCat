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

import cn.wjybxx.concurrent.EventLoop;
import cn.wjybxx.concurrent.EventLoopFactory;
import cn.wjybxx.concurrent.EventLoopGroup;

/**
 * @author wjybxx
 * date - 2023/10/4
 */
public interface WorkerFactory extends EventLoopFactory {

    @Deprecated
    @Override
    default EventLoop newChild(EventLoopGroup parent, int index, Object extraInfo) {
        return newChild((Node) parent, index, (WorkerCtx) extraInfo);
    }

    /**
     * @param workerCtx node为worker分配的上下文
     */
    Worker newChild(Node parent, int index, WorkerCtx workerCtx);

}