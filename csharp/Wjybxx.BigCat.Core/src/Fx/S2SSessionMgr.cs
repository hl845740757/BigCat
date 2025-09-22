#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System.Collections.Generic;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// 服务器间的Session管理器
/// 
/// 1.Session如何管理取决于用户，因此实际使用时应该绑定该类的子类；该类是非抽象的，以允许我们测试期间直接绑定该类。
/// 2.在创建新的Session和删除Session时，应当调用<see cref="S2SRpcClient.AddSession(long)"/>
/// 和<see cref="S2SRpcClient.RemoveSession(long)"/>，以保证数据一致性。
/// 3.该管理器负责{@link S2SRpcClient}的配置初始化。
///
/// PS：其实可以数据和行为分离实现逻辑的扩展的，但会导致更多的抽象，体验未必更好。
/// 
/// </summary>
public class S2SSessionMgr : EventLoopModule
{
    /** 服务器会话超时时间 */
    protected long sessionTimeoutMs = 15 * 1000;
    /** 服务器间的Session */
    protected readonly Dictionary<long, S2SSession> sessionMap = new(10);
    /** nodeId到Session的映射 */
    protected readonly Dictionary<int, S2SSession> nodeId2SessionMap = new(10);

    #region props

    /// <summary>
    /// 会话超时时间
    /// </summary>
    public long SessionTimeoutMs {
        get => sessionTimeoutMs;
        set => sessionTimeoutMs = value;
    }

    /// <summary>
    /// SessionId到Session的字典 -- 外部不可修改
    /// </summary>
    public Dictionary<long, S2SSession> SessionMap => sessionMap;
    /// <summary>
    /// NodeId到Session的字典 -- 外部不可修改
    /// </summary>
    public Dictionary<int, S2SSession> NodeId2SessionMap => nodeId2SessionMap;

    /// <summary>
    /// 通过SessionId查询Session
    /// </summary>
    public S2SSession? GetSession(long sessionId) {
        sessionMap.TryGetValue(sessionId, out S2SSession r);
        return r;
    }

    /// <summary>
    /// 通过NodeId查询Session
    /// </summary>
    public S2SSession? GetSessionOfNode(int nodeId) {
        nodeId2SessionMap.TryGetValue(nodeId, out S2SSession r);
        return r;
    }

    #endregion

    /// <summary>
    /// 测试请求是否可以派发 -- 通常检查连接的有效性和会话状态。
    /// 1.走到这里的时候，方法参数已反序列化
    /// 2.该方法在Worker线程执行（即主线程执行）
    /// 3.信息应该来源于方法的切面数据<see cref="RpcMethodRegistry.GetProxyData"/>
    /// 4.错误码约定见<see cref="RpcErrorCodes"/>
    /// </summary>
    /// <param name="request"></param>
    /// <returns>错误码，0表示可执行</returns>
    public virtual int Test(RpcRequest request) {
        // 0表示本地虚拟Session
        if (request.SessionId == 0) {
            return 0;
        }
        if (!sessionMap.TryGetValue(request.SessionId, out S2SSession session)) {
            // 默认1为连接管理服务，不要求Session存在
            if (request.ServiceId == 1) {
                return 0;
            }
            return RpcErrorCodes.SERVER_SESSION_NOT_EXIST;
        }
        return 0;
    }
}
}