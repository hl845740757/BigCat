/*
 *  Copyright 2023 wjybxx
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

import cn.wjybxx.common.rpc.RpcAddr;

/**
 * 玩家通信地址
 * (如果不想每次创建，可以缓存在玩家Session上)
 *
 * @author wjybxx
 * date - 2023/12/22
 */
public final class PlayerAddr implements RpcAddr {

    public final long playerGuid;

    public PlayerAddr(long playerGuid) {
        this.playerGuid = playerGuid;
    }

}