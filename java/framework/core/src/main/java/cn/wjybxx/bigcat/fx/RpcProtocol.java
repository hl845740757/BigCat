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

import java.util.HexFormat;

/**
 * Rpc协议的抽象基类
 * 1.该对象通常不序列化，而是手工编码为对应网络包
 * 2.{@link #data}为null时尽量还是特殊处理，空字节数组未必兼容
 *
 * @author wjybxx
 * date - 2023/9/11
 */
public abstract class RpcProtocol {

    /** 会话id */
    protected long sessionId;
    /** 发送方地址 -- 服务器通信使用 */
    protected WorkerAddr srcAddr;
    /** 接收方地址 -- 服务器通信使用 */
    protected WorkerAddr destAddr;

    /**
     * 方法参数或结果
     * <p>
     * 1.可能情况：null,string,bytes,结构
     * 2.bytes 表示已经序列化; TODO 改为Bytebuf或Chunk，允许池化
     * 3.null 表示无参数和结果，或是用户的参数和结果为null；在写入最终协议时需要区分
     * 4.string 表示错误信息
     * 5.结构 表示正常的参数和结构
     */
    protected Object data;
    /** 方法参数或结果是否可共享 */
    protected transient boolean sharable;

    public RpcProtocol() {
    }

    public RpcProtocol(long sessionId, WorkerAddr srcAddr, WorkerAddr destAddr) {
        this.sessionId = sessionId;
        this.srcAddr = srcAddr;
        this.destAddr = destAddr;
    }

    // region internal

    /** 数据部分是否是null或bytes -- 不需要序列化或已经序列化 */
    public final boolean isNullOrBytes() {
        return data == null || data instanceof byte[];
    }

    /** 数据部分是否是bytes -- 需要反序列化 */
    public final boolean isBytes() {
        return data instanceof byte[];
    }

    /** data转bytes返回 */
    public final byte[] getBytes() {
        return (byte[]) data;
    }

    protected void reset() {
        sessionId = 0;
        srcAddr = null;
        destAddr = null;
        data = null;
        sharable = false;
    }

    // endregion

    // region getter/setter

    public long getSessionId() {
        return sessionId;
    }

    public void setSessionId(long sessionId) {
        this.sessionId = sessionId;
    }

    public WorkerAddr getSrcAddr() {
        return srcAddr;
    }

    public void setSrcAddr(WorkerAddr srcAddr) {
        this.srcAddr = srcAddr;
    }

    public WorkerAddr getDestAddr() {
        return destAddr;
    }

    public void setDestAddr(WorkerAddr destAddr) {
        this.destAddr = destAddr;
    }

    public Object getData() {
        return data;
    }

    public void setData(Object data) {
        this.data = data;
    }

    public boolean isSharable() {
        return sharable;
    }

    public void setSharable(boolean sharable) {
        this.sharable = sharable;
    }
    // endregion

    protected final String dataToString() {
        if (data == null) {
            return "null";
        }
        if (data instanceof byte[] bytes) {
            return HexFormat.of().formatHex((bytes)); // 其实意义不大?
        }
        return data.toString();
    }
}