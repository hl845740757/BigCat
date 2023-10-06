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

import cn.wjybxx.common.codec.AutoSchema;
import cn.wjybxx.common.codec.binary.BinaryObjectReader;
import cn.wjybxx.common.codec.binary.BinarySerializable;
import cn.wjybxx.common.rpc.RpcAddr;

import java.util.Objects;

/**
 * Worker地址，通常情况下我们只指定{@link #nodeId}，不关注Node内的情况；
 * 但如果期望和Node内某一个线程建立稳定连接时，可以指定{@link #workerId}
 *
 * @author wjybxx
 * date - 2023/10/4
 */
@AutoSchema
@BinarySerializable
public class WorkerAddr implements RpcAddr {

    /**
     * 部分项目可能习惯{@code type + id}的形式，多数情况下是可以合成为一个int的。
     */
    public final int nodeId;
    public final String workerId;

    public WorkerAddr(int nodeId) {
        this.nodeId = nodeId;
        this.workerId = null;
    }

    /**
     * @param nodeId   节点id
     * @param workerId 如果为null，表示不指定Worker
     */
    public WorkerAddr(int nodeId, String workerId) {
        this.nodeId = nodeId;
        this.workerId = workerId;
    }

    /** 解码函数 */
    public WorkerAddr(BinaryObjectReader reader) {
        this.nodeId = reader.readInt(WorkerAddrSchema.numbers_nodeId);
        this.workerId = reader.readString(WorkerAddrSchema.numbers_workerId);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        WorkerAddr that = (WorkerAddr) o;

        if (nodeId != that.nodeId) return false;
        return Objects.equals(workerId, that.workerId);
    }

    @Override
    public int hashCode() {
        int result = nodeId;
        result = 31 * result + (workerId != null ? workerId.hashCode() : 0);
        return result;
    }

    @Override
    public String toString() {
        return "WorkerAddr{" +
                "nodeId=" + nodeId +
                ", workerId='" + workerId + '\'' +
                '}';
    }
}