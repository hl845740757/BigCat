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

import cn.wjybxx.common.ex.ErrorCodeException;

/**
 * 0~100留给底层扩展，其它区间段，可以自定义。
 * 当远程抛出{@link ErrorCodeException}且code大于100时，本地也将抛出{@link ErrorCodeException}
 *
 * @author wjybxx
 * date 2023/4/1
 * *
 */
public class RpcErrorCodes {

    /** 调用成功的错误码 */
    public static final int SUCCESS = 0;
    // 1 - 10 为特殊状态码，应用层不可见


    // 11 - 30 表客户端异常
    /** 路由失败异常 */
    public static final int LOCAL_ROUTER_EXCEPTION = 11;
    /** 超时 */
    public static final int LOCAL_TIMEOUT = 12;
    /** 本地中断了线程 */
    public static final int LOCAL_INTERRUPTED = 13;
    /** 本地发生了未知错误 */
    public static final int LOCAL_UNKNOWN_EXCEPTION = 14;
    /** 本地反序列化请求或结果失败 */
    public static final int LOCAL_DESERIALIZE_FAILED = 15;

    // 31 - 50 表服务器异常
    /** 表示服务器调用出现异常的错误码 */
    public static final int SERVER_EXCEPTION = 31;
    /** 不支持的接口调用 */
    public static final int SERVER_UNSUPPORTED_INTERFACE = 32;
    /** 连接状态错误 */
    public static final int SERVER_CONNECTION_STATE_ERROR = 33;
    /** 服务端反序列化请求失败 */
    public static final int SERVER_DESERIALIZE_FAILED = 34;

    public static boolean isUserCode(int code) {
        return code > 100;
    }
}