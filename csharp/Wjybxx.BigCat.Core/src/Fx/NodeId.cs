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
using System.Runtime.CompilerServices;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// NodeId工具类
/// </summary>
public static class NodeId
{
    /** 服务id的乘系数: 10W */
    public const int NODE_TYPE_FACTOR = 10 * 10000;

    /** 通过服务器类型和服务器id计算nodeId */
    public static int MakeNodeId(int type, int sid) {
        if (type < 0 || type > 1000) {
            throw new ArgumentException("type must be [0, 999]");
        }
        if (sid < 0 || sid >= NODE_TYPE_FACTOR) {
            throw new ArgumentException("sid must between [0, 99999]");
        }
        return type * NODE_TYPE_FACTOR + sid;
    }

    /** 通过节点id计算服务器类型 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TypeOfNodeId(int nodeId) {
        return nodeId / NODE_TYPE_FACTOR;
    }

    /** 通过节点id计算服务器id */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SidOfNodeId(int nodeId) {
        return nodeId % NODE_TYPE_FACTOR;
    }
}
}