/*
 *  Copyright 2023-2024 wjybxx
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to iBn writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

package cn.wjybxx.bigcat.fx;

/**
 * 玩家通信地址
 * 1. 如果不想每次创建，可以缓存在玩家Session上。
 * 2. 服务器给玩家发送消息，不需要走{@link RpcClient}，因为服务器不会向玩家发送{@link RpcRequest}。
 * 3. 服务器只需要模拟Rpc的接收过程。
 *
 * @author wjybxx
 * date - 2023/12/22
 * @deprecated 与玩家通信时，使用连接id代替更好
 */
@Deprecated
public final class PlayerAddr implements RpcAddr {

    /** 连接id */
    public final long conId;

    public PlayerAddr(long conId) {
        this.conId = conId;
    }

}