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

import cn.wjybxx.common.log.DebugLogFriendlyObject;

/**
 * Rpc协议的抽象基类
 *
 * @author wjybxx
 * date - 2023/9/11
 */
public abstract class RpcProtocol implements DebugLogFriendlyObject {

    /** 连接id */
    protected long conId;
    /** 发送方节点id */
    protected RpcAddr srcAddr;
    /** 接收方节点id -- 如果是广播，接收方字段是否有值取决于实现 */
    protected RpcAddr destAddr;

    /** 不序列化；未来可能在注解上配置 */
    private transient int ctl;

    public RpcProtocol() {
    }

    public RpcProtocol(long conId, RpcAddr srcAddr, RpcAddr destAddr) {
        this.conId = conId;
        this.srcAddr = srcAddr;
        this.destAddr = destAddr;
    }

    /** 方法参数或结果是否可共享 */
    public final boolean isSharable() {
        return ctl > 0;
    }

    public final void setSharable(boolean value) {
        ctl = value ? 1 : 0;
    }

    public int getCtl() {
        return ctl;
    }

    public void setCtl(int ctl) {
        this.ctl = ctl;
    }

    // region getter/setter
    public long getConId() {
        return conId;
    }

    public RpcProtocol setConId(long conId) {
        this.conId = conId;
        return this;
    }

    public RpcAddr getSrcAddr() {
        return srcAddr;
    }

    public RpcProtocol setSrcAddr(RpcAddr srcAddr) {
        this.srcAddr = srcAddr;
        return this;
    }

    public RpcAddr getDestAddr() {
        return destAddr;
    }

    public RpcProtocol setDestAddr(RpcAddr destAddr) {
        this.destAddr = destAddr;
        return this;
    }
    // endregion
}