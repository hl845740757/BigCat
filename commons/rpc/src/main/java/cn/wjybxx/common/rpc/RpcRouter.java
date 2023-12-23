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
 * 该接口负责真正的消息路由。
 * 实现要求：
 * 1. 发给同一个target的消息必须保证先发的先到！！！
 * 2. 单播和广播消息之间最好也保证顺序 —— 使用双channel或双topic的方式可能存在时序问题。
 *
 * @author wjybxx
 * date 2023/4/1
 */
public interface RpcRouter {

    /**
     * 发送一个协议
     * 1.发送的时候不可以修改proto的内容
     * 2.如果转发时要重定向等，应当先拷贝，再修改拷贝后的实例；或者不编码原始proto的目标地址{@link RpcProtocol#getDestAddr()}
     *
     * @param protocol 要路由的协议
     * @return 如果不能发送，则返回false，请确保正确的进行了实现。
     */
    boolean send(RpcProtocol protocol);

}