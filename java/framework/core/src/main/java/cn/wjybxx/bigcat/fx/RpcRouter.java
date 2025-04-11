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

/**
 * Rpc路由器
 * 1.该接口主要用于支持自定义地址解析；查询地址特性的方法可能多线程访问，需要保证【线程安全】。
 * 2.在收到请求时应当调用{@link RpcSupport#onRcvRequest(RpcRequest)}
 * 3.在收到响应时应当调用{@link RpcSupport#onRcvResponse(RpcResponse)}
 * 4.Router和{@link RpcSupport}都是Node上的模块，需要双向绑定。
 * 5.默认情况下，
 *
 * <h3>时序要求</h3>
 * 1.发给同一个Node的消息必须保证先发的先到
 * 2.单播和广播消息之间最好也保证顺序 —— 使用双channel或双topic的方式可能存在时序问题。
 * 3.尽可能避免修改协议对象的数据
 *
 * <h3>延迟序列化</h3>
 * 1.{@link RpcRequest}和{@link RpcResponse}中的方法参数可能是未序列化的，以允许进程内共享对象和延迟序列化。
 * 2.如果方法参数或结果是不可共享的，则已在Worker线程序列化；否则由Router决定是否序列化，以及序列化的时机。
 * 3.可通过{@link RpcProtocol#isNullOrBytes()}判断是否已序列化。
 *
 * <h3>协议池化管理</h3>
 * 为减少频繁创建{@link RpcRequest}和{@link RpcResponse}带来的GC压力，我们对请求和响应都进行了池化。
 * 发送时，Rpc框架在发送请求时，会将必要数据拷贝下来，不再持有请求对象的引用，Router应当在发包之后将请求对象归还到池中。
 * 接收时，Rpc框架在处理完请求和响应后，会将请求和响应归还到池中。
 * 暂未实现为引用计数方案，目前来看不太需要 —— Request和Response的声明周期相对简单。
 * <p>
 * {@link RpcRequest#acquire()}、{@link RpcRequest#release(RpcRequest)}、
 * {@link RpcResponse#acquire()}、{@link RpcResponse#release(RpcResponse)}
 *
 * @author wjybxx
 * date - 2023/10/28
 */
public interface RpcRouter {

    /**
     * 发送一个协议
     * (Node线程调用，发到网络时才被调用)
     *
     * @param protocol 要发送的协议
     */
    void send(RpcProtocol protocol);

}