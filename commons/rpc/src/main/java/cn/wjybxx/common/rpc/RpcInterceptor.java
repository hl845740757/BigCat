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
 * rpc派发的拦截器
 *
 * @author wjybxx
 * date - 2023/9/16
 */
public interface RpcInterceptor {

    /**
     * 测试请求是否可以派发 -- 通常检查连接的有效性和会话状态。
     * 1.走到这里的时候，方法参数已反序列化
     *
     * @return 错误码，返回0表示可以执行，其它则表示不可以执行
     */
    int test(RpcRequest request);

}