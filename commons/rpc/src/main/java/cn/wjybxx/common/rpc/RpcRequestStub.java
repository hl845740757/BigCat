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

/**
 * Rpc请求存根
 * 该接口用于为用户提供一个视图，以查看一些信息 -- 主要用于debug
 *
 * @author wjybxx
 * date 2023/4/5
 */
public interface RpcRequestStub {

    /** 请求的超时时间 */
    long getDeadline();

    /** 获取请求的目的地 */
    RpcAddr getDestAddr();

    /** 获取请求信息 */
    RpcRequest getRequest();

}