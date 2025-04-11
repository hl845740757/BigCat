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

import it.unimi.dsi.fastutil.longs.Long2ObjectMap;
import it.unimi.dsi.fastutil.longs.Long2ObjectOpenHashMap;

/**
 * 服务器之间的Session。
 *
 * @author wjybxx
 * date - 2025/4/10
 */
public final class S2SSession {

    // 基础数据
    /** 会话Id */
    public final long sessionId;
    /** 节点Id */
    public final int nodeId;
    /** 连接状态 */
    private int state;
    /** 进入状态的时间 */
    private long enterTime;
    /** 上次ping时间戳 */
    private long lastPingTime;

    // 用户数据
    /** 外网地址 */
    private String outHost;
    /** 外网端口 */
    private int outPort;
    /** 玩家数量 */
    private int playerCount;
    /** 场景数量 */
    private int sceneCount;
    /** 房间数量 */
    private int roomCount;
    /** 用户数据 -- 组合扩展 */
    private Object userData;

    // rpc数据
    /** 请求id分配器 */
    private long sequencer = 0;
    /** 发出的请求信息 */
    final Long2ObjectMap<RpcRequestStub> stubMap = new Long2ObjectOpenHashMap<>();

    public S2SSession(long sessionId, int nodeId, long lastPingTime) {
        this.sessionId = sessionId;
        this.nodeId = nodeId;
        this.lastPingTime = lastPingTime;
    }

    /** 服务器id */
    public int getType() {
        return NodeId.typeOfNodeId(nodeId);
    }

    /** 服务器id */
    public int getSid() {
        return NodeId.sidOfNodeId(nodeId);
    }

    // region getter/setter

    public int getState() {
        return state;
    }

    public void setState(int state) {
        this.state = state;
    }

    public long getEnterTime() {
        return enterTime;
    }

    public void setEnterTime(long enterTime) {
        this.enterTime = enterTime;
    }

    public long getLastPingTime() {
        return lastPingTime;
    }

    public void setLastPingTime(long lastPingTime) {
        this.lastPingTime = lastPingTime;
    }

    public String getOutHost() {
        return outHost;
    }

    public void setOutHost(String outHost) {
        this.outHost = outHost;
    }

    public int getOutPort() {
        return outPort;
    }

    public void setOutPort(int outPort) {
        this.outPort = outPort;
    }

    public int getPlayerCount() {
        return playerCount;
    }

    public void setPlayerCount(int playerCount) {
        this.playerCount = playerCount;
    }

    public int getSceneCount() {
        return sceneCount;
    }

    public void setSceneCount(int sceneCount) {
        this.sceneCount = sceneCount;
    }

    public Object getUserData() {
        return userData;
    }

    public void setUserData(Object userData) {
        this.userData = userData;
    }

    public int getRoomCount() {
        return roomCount;
    }

    public void setRoomCount(int roomCount) {
        this.roomCount = roomCount;
    }
    // endregion

    // region internal

    long nextRequestId() {
        return ++sequencer;
    }

    // endregion
}