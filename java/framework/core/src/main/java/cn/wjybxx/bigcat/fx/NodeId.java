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

/**
 * NodeId工具类
 *
 * @author wjybxx
 * date - 2025/4/10
 */
public final class NodeId {

    /** 服务器类型的乘系数 */
    public static final int NODE_TYPE_FACTOR = 10000;

    /** 通过服务器类型和服务器id计算nodeId */
    public static int makeNodeId(int type, int sid) {
        if (type < 0 || type > 1000) {
            throw new IllegalArgumentException("type must be [0, 999]");
        }
        if (sid < 0 || sid >= NODE_TYPE_FACTOR) {
            throw new IllegalArgumentException("sid must between [0, 99999]");
        }
        return type * NODE_TYPE_FACTOR + sid;
    }

    /** 通过节点id计算服务器类型 */
    public static int typeOfNodeId(int nodeId) {
        return nodeId / NODE_TYPE_FACTOR;
    }

    /** 通过节点id计算服务器id */
    public static int sidOfNodeId(int nodeId) {
        return nodeId % NODE_TYPE_FACTOR;
    }
}