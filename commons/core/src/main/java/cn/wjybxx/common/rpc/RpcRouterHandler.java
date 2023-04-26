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

/**
 * 该接口负责真正的消息路由。
 * 实现要求：发给同一个target的消息必须保证先发的先到！！！
 *
 * @author wjybxx
 * date 2023/4/1
 */
public interface RpcRouterHandler {

    /**
     * 单播一个协议
     *
     * @param target 协议的目标方
     * @param proto  要路由的协议
     * @return 如果不能发送，则返回false，请确保正确的进行了实现。
     */
    boolean send(NodeSpec target, Object proto);

    /**
     * 广播一个协议
     *
     * @param scopeSpec 广播范围描述
     * @param proto     要路由的协议
     * @return 如果不能发送，则返回false，请确保正确的进行了实现。
     */
    boolean broadcast(ScopeSpec scopeSpec, Object proto);

}