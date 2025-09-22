/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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

import cn.wjybxx.concurrent.EventLoopModule;
import it.unimi.dsi.fastutil.ints.Int2ObjectMap;
import it.unimi.dsi.fastutil.ints.Int2ObjectOpenHashMap;
import it.unimi.dsi.fastutil.longs.Long2ObjectMap;
import it.unimi.dsi.fastutil.longs.Long2ObjectOpenHashMap;

/**
 * 服务器间的Session管理器
 * <p>
 * 1.Session如何管理取决于用户，因此实际使用时应该绑定该类的子类；该类是非抽象的，以允许我们测试期间直接绑定该类。
 * 2.在创建新的Session和删除Session时，应当调用{@link S2SRpcClient#addSession(long)}
 * 和{@link S2SRpcClient#removeSession(long)}，以保证数据一致性。
 * 3.该管理器负责{@link S2SRpcClient}的配置初始化。
 * <p>
 * PS：其实可以数据和行为分离实现逻辑的扩展的，但会导致更多的抽象，体验未必更好。
 *
 * @author wjybxx
 * date - 2025/4/11
 */
public class S2SSessionMgr extends EventLoopModule {

    /** 服务器会话超时时间 */
    protected long sessionTimeoutMs = 15 * 1000;
    /** 服务器间的Session */
    protected final Long2ObjectMap<S2SSession> sessionMap = new Long2ObjectOpenHashMap<>(10);
    /** nodeId到Session的映射 */
    protected final Int2ObjectMap<S2SSession> nodeId2SessionMap = new Int2ObjectOpenHashMap<>(10);

    // region getter/setter

    /** 会话超时时间 */
    public long getSessionTimeoutMs() {
        return sessionTimeoutMs;
    }

    public void setSessionTimeoutMs(long sessionTimeoutMs) {
        this.sessionTimeoutMs = sessionTimeoutMs;
    }

    /** 外部可读不可修改 */
    public Long2ObjectMap<S2SSession> getSessionMap() {
        return sessionMap;
    }

    /** 外部可读不可修改 */
    public Int2ObjectMap<S2SSession> getNodeId2SessionMap() {
        return nodeId2SessionMap;
    }

    /** 通过SessionId查询 */
    public S2SSession getSession(long sessionId) {
        return sessionMap.get(sessionId);
    }

    /** 通过NodeId查询 */
    public S2SSession getSessionOfNode(int nodeId) {
        return nodeId2SessionMap.get(nodeId);
    }

    // endregion

    // region rpc支持

    /**
     * 测试请求是否可以执行 -- 通常检查连接的有效性和会话状态。
     * 1.走到这里的时候，方法参数已反序列化
     * 2.该方法在Worker线程执行（即主线程执行）
     * 3.信息应该来源于方法的切面数据{@link RpcMethodRegistry#getProxyData(int, int)}
     * 4.错误码约定见{@link RpcErrorCodes}
     *
     * @return 错误码，返回0表示可以执行，其它则表示不可以执行
     */
    public int test(RpcRequest request) {
        // 0表示本地虚拟Session
        if (request.getSessionId() == 0) {
            return 0;
        }
        S2SSession session = sessionMap.get(request.getSessionId());
        if (session == null) {
            // 默认1为连接管理服务，不要求Session存在
            if (request.getServiceId() == 1) {
                return 0;
            }
            return RpcErrorCodes.SERVER_SESSION_NOT_EXIST;
        }
        return 0;
    }
    // endregion
}