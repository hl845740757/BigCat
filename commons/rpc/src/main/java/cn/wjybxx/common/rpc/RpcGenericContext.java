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

import cn.wjybxx.common.annotation.StableName;

/**
 * rpc上下文基础接口。
 * 1. 该接口仅提供获取远端信息方案
 * 2. 当rpc方法的第一个参数为该类型时，仍由Rpc框架管理结果的返回。
 * 3. 在运行时如有特殊需求，可转换为{@link RpcContext}接口。
 *
 * @author wjybxx
 * date - 2023/4/1
 */
public interface RpcGenericContext {

    /**
     * @return 返回调用的详细信息
     */
    RpcRequest request();

    /**
     * 远端地址
     * 1.可用于在返回结果前后向目标发送额外的消息 -- 它对应的是{@link RpcRequest#srcAddr}
     * 2.本地进行模拟时，可以赋值{@link #localAddr()}
     */
    default RpcAddr remoteAddr() {
        return request().srcAddr;
    }

    /**
     * 本地地址
     * 可用于校验 -- 对应{@link RpcRequest#destAddr}
     */
    default RpcAddr localAddr() {
        return request().destAddr;
    }

    /** 当前返回值是否可共享 */
    boolean isSharable();

    /** 设置返回值是否可共享标记 -- 不论是否托管返回时机，都可以设置 */
    @StableName
    void setSharable(boolean sharable);

}