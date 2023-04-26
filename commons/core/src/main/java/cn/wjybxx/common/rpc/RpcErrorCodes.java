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
 * 0~20留给底层扩展，其它区间段，可以自定义。
 *
 * @author wjybxx
 * date 2023/4/1
 * *
 */
public class RpcErrorCodes {

    /** 调用成功的错误码 */
    public static final int SUCCESS = 0;

    // 1 - 10 表客户端异常
    /** 路由失败异常 */
    public static final int LOCAL_ROUTER_EXCEPTION = 1;
    /** 超时 */
    public static final int LOCAL_TIMEOUT = 2;
    /** 本地发生了未知错误 */
    public static final int LOCAL_UNKNOWN_EXCEPTION = 3;

    // 11 - 20 表服务器异常
    /** 表示服务器调用出现异常的错误码 */
    public static final int SERVER_EXCEPTION = 11;

}