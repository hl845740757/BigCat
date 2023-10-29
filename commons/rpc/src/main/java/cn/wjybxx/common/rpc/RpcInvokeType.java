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
 * @author wjybxx
 * date - 2023/9/14
 */
public class RpcInvokeType {

    public static final int ONEWAY = 1;
    public static final int CALL = 2;
    public static final int SYNC_CALL = 3;

    /** 是否是消息 -- 远程不需要结果 */
    public static boolean isMessage(int type) {
        return type == ONEWAY;
    }

    /** 是否是调用 -- 远程需要结果 */
    public static boolean isCall(int type) {
        return type == CALL || type == SYNC_CALL;
    }

}