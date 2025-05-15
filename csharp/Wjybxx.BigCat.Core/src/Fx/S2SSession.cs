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

using System;
using System.Collections.Generic;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// 服务器之间的Session。
/// 
/// <h3>无状态Session方法</h3>
/// 如果想处理旧连接的请求包，比如通过MQ通信，那么需要保持<see cref="sessionId"/>一致，
/// 根据两个服务器的<see cref="nodeId"/>计算稳定的<see cref="sessionId"/>即可。
/// 在这种情况下，应当避免使用Call，仅使用Send，因为Call是有状态的。
/// </summary>
public sealed class S2SSession
{
#nullable disable
    // 基础数据
    /** 会话Id */
    public readonly long sessionId;
    /** 节点Id */
    public readonly int nodeId;
    /** 连接状态 */
    private int state;
    /** 进入状态的时间 */
    private long enterTime;
    /** 上次ping时间戳 */
    private long lastPingTime;

    // 服务器数据 -- 提供常用字段
    /** 外网地址 */
    private string outHost;
    /** 外网端口 */
    private int outPort;
    /** 网络连接数量 -- 包含尚未登录的玩家 */
    private int socketCount;
    /** 玩家数量 */
    private int playerCount;
    /** 场景数量 */
    private int sceneCount;
    /** 房间数量 */
    private int roomCount;
    /** 用户数据 -- 组合扩展 */
    private object userData;

    // rpc数据
    /** 请求id分配器 */
    private long sequencer = 0;
    /** 发出的请求信息 */
    internal readonly Dictionary<long, RpcRequestStub> stubMap = new(100);

    public S2SSession(long sessionId, int nodeId) {
        this.sessionId = sessionId;
        this.nodeId = nodeId;
    }

    /** 服务器id */
    public int ServerType => NodeId.TypeOfNodeId(nodeId);

    /** 服务器id */
    public int ServerId => NodeId.SidOfNodeId(nodeId);

    #region props

    public int State {
        get => state;
        set => state = value;
    }
    public long EnterTime {
        get => enterTime;
        set => enterTime = value;
    }
    public long LastPingTime {
        get => lastPingTime;
        set => lastPingTime = value;
    }
    public string OutHost {
        get => outHost;
        set => outHost = value;
    }
    public int OutPort {
        get => outPort;
        set => outPort = value;
    }
    public int SocketCount {
        get => socketCount;
        set => socketCount = value;
    }
    public int PlayerCount {
        get => playerCount;
        set => playerCount = value;
    }
    public int SceneCount {
        get => sceneCount;
        set => sceneCount = value;
    }
    public int RoomCount {
        get => roomCount;
        set => roomCount = value;
    }
    public object UserData {
        get => userData;
        set => userData = value;
    }

    #endregion

    #region internal

    internal long NextRequestId() {
        return ++sequencer;
    }

    #endregion
}
}