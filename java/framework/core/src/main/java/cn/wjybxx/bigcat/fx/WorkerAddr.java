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

import cn.wjybxx.dsoncodec.DsonObjectReader;
import cn.wjybxx.dsoncodec.TypeInfo;
import cn.wjybxx.dsoncodec.annotations.DsonProperty;
import cn.wjybxx.dsoncodec.annotations.DsonSerializable;

import java.util.Objects;

/**
 * Worker地址
 *
 * @author wjybxx
 * date - 2023/10/4
 */
@DsonSerializable
public final class WorkerAddr {

    /**
     * 服务器节点id。
     * <p>
     * 1.服务器地址应当是一个稳定值，就像主机的hostname。
     * 2.发送Rpc时通常精确指定
     * 3.NodeId通常是可编码的，通常包含服务器类型和服务器id。
     * 4.虽然int类型随扩展性有限，但对于游戏服务器而言足够。
     * 5.工具类{@link NodeId}
     */
    @DsonProperty(name = "nid")
    public final int nodeId;
    /**
     * WorkerId，线程Id
     * <p>
     * 1.线程id通常不具有稳定性，用户应尽可能避免线程id。
     * 2.发送Rpc时通常不指定。
     * 3.永远不应该发到网络中！
     */
    @DsonProperty(name = "wid")
    public final String workerId;

    public WorkerAddr(int nodeId, String workerId) {
        this.nodeId = nodeId;
        this.workerId = workerId;
    }

    /** 解码函数 */
    public WorkerAddr(DsonObjectReader reader, TypeInfo typeInfo) {
        this.nodeId = reader.readInt(WorkerAddrCodec.names_nodeId);
        this.workerId = reader.readString(WorkerAddrCodec.names_workerId);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        WorkerAddr that = (WorkerAddr) o;
        return nodeId == that.nodeId && Objects.equals(workerId, that.workerId);
    }

    @Override
    public int hashCode() {
        int result = nodeId;
        result = 31 * result + Objects.hashCode(workerId);
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