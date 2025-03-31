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
 *
 * <h3>时序要求</h3>
 * 1.发给同一个target的消息必须保证先发的先到
 * 2.单播和广播消息之间最好也保证顺序 —— 使用双channel或双topic的方式可能存在时序问题。
 * 3.尽可能避免修改协议对象的数据
 *
 * <h3>进程内可共享对象</h3>
 * 1.{@link RpcRequest}和{@link RpcResponse}中的方法参数可能是未序列化的，以允许进程内共享对象。
 * 2.如果方法参数或结果是不可共享的，则已在Worker线程序列化；否则由Router决定是否序列化，以及序列化的时机。
 * 3.可通过{@link RpcProtocol#isNullOrBytes()}判断是否已序列化。
 *
 * @author wjybxx
 * date - 2023/10/28
 */
public interface RpcRouter {

    /**
     * 发送一个协议
     * 注：该方法在Node线程调用
     *
     * @param protocol 要发送的协议
     * @return 是否发送成功
     */
    boolean send(RpcProtocol protocol);

    /**
     * 测试给定地址似乎否是本地地址(进程内地址)
     * 1.通常用于判断数据是否可共享 -- ‘本地单播’时可直接传递原始对象。
     * 2.用于本地服务调用优化，避免不必要的序列化和拷贝 -- 比如：调用本地的DB服务，Http服务。
     */
    boolean isLocalAddr(RpcAddr addr);

    /** 判断是否是单播地址 -- 只有每一级都是单播的情况下才可以返回true */
    boolean isUnicastAddr(RpcAddr addr);

    /** 测试是否是本地单播地址 -- 测试给定的地址在worker层是否是单播地址 */
    boolean isWorkerUnicastAddr(RpcAddr addr);

    /** 测试是否是本地广播地址 -- 测试给定的地址在worker层是否是广播地址 */
    boolean isWorkerBroadcastAddr(RpcAddr addr);

}