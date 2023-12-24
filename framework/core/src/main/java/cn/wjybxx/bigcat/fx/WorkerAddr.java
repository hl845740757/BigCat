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

package cn.wjybxx.bigcat.fx;

import cn.wjybxx.dson.codec.binary.BinaryObjectReader;
import cn.wjybxx.dson.codec.binary.BinarySerializable;
import cn.wjybxx.common.rpc.RpcAddr;
import org.apache.commons.lang3.StringUtils;

import java.util.Objects;

/**
 * Worker地址
 * <h3>推荐方案</h3>
 * 负数用于表示特殊地址，正数表示单播地址，0表示不指定。
 * 1. {@link #serverType} 如果为-1，表示匹配所有类型的服务器
 * 2. {@link #serverId} 如果为-1，表示匹配该类型所有服务器
 * 3. {@link #workerId} 如果为null或空白，表示不指定Worker；如果为'*'，表示匹配所有worker
 *
 * @author wjybxx
 * date - 2023/10/4
 */
@BinarySerializable
public class WorkerAddr implements RpcAddr {

    /** 服务器类型 */
    public final int serverType;
    /** 服务器id */
    public final int serverId;
    /** 线程id */
    public final String workerId;

    public WorkerAddr(int serverType, int serverId) {
        this(serverType, serverId, null);
    }

    public WorkerAddr(int serverType, int serverId, String workerId) {
        this.serverType = serverType;
        this.serverId = serverId;
        this.workerId = workerId;
    }

    /** 解码函数 */
    public WorkerAddr(BinaryObjectReader reader) {
        this.serverType = reader.readInt(WorkerAddrCodec.numbers_serverType);
        this.serverId = reader.readInt(WorkerAddrCodec.numbers_serverId);
        this.workerId = reader.readString(WorkerAddrCodec.numbers_workerId);
    }

    /** 是否有workerId */
    public boolean hasWorkerId() {
        return !StringUtils.isBlank(workerId);
    }

    /** 测试除worker以外的部分是否相同 */
    public boolean equalsIgnoreWorker(WorkerAddr that) {
        return serverType == that.serverType
                && serverId == that.serverId;
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        WorkerAddr that = (WorkerAddr) o;

        if (serverType != that.serverType) return false;
        if (serverId != that.serverId) return false;
        return Objects.equals(workerId, that.workerId);
    }

    @Override
    public int hashCode() {
        int result = serverType;
        result = 31 * result + serverId;
        result = 31 * result + (workerId != null ? workerId.hashCode() : 0);
        return result;
    }

    @Override
    public String toString() {
        return "WorkerAddr{" +
                "serverType=" + serverType +
                ", serverId=" + serverId +
                ", workerId='" + workerId + '\'' +
                '}';
    }
}