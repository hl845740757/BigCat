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

import cn.wjybxx.bigcat.rpc.RpcMethod;
import cn.wjybxx.bigcat.rpc.RpcService;

import java.util.List;
import java.util.StringJoiner;

/**
 * @author wjybxx
 * date 2023/10/29
 */
@RpcService(serviceId = 1)
public class RpcServiceExample {

    @RpcMethod(methodId = 1, customData = "{interval : 500}")
    public String echo(String msg) {
        return msg;
    }

    @RpcMethod(methodId = 4)
    public String join(List<String> args) {
        StringJoiner joiner = new StringJoiner(",");
        for (String arg : args) {
            joiner.add(arg);
        }
        return joiner.toString();
    }

}