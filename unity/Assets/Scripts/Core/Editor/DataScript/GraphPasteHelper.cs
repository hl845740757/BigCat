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
using UnityEngine;
using Wjybxx.Commons;

namespace Wjybxx.BigCat.Editor.DataScript
{
/// <summary>
/// 方法对象
///
/// 限定访问数据，保证安全性
/// </summary>
internal class GraphPasteHelper
{
    private readonly DataGraph graph;
    private readonly List<DataNode> srcNodes;
    private readonly List<DataNode> destNodes;
    private readonly Dictionary<long, long> idMap;

    public GraphPasteHelper(DataGraph graph, List<DataNode> srcNodes) {
        this.graph = graph;
        this.srcNodes = srcNodes;
        this.destNodes = new List<DataNode>(srcNodes.Count);
        this.idMap = new Dictionary<long, long>(srcNodes.Count);
    }

    public List<DataNode> Execute() {
        // 拷贝Nodes
        foreach (DataNode srcNode in srcNodes) {
            DataNode copyNode = graph.CopyNode(srcNode);
            copyNode.localId = graph.NextLocalId();
            copyNode.position += new Vector2(50, 50); // 错开
            idMap[srcNode.localId] = copyNode.localId;
            destNodes.Add(copyNode);
        }
        foreach (DataNode destNode in destNodes) {
            foreach (Variable outputField in destNode.outputFields) {
                if (!outputField.isCollectionType) {
                    // 非集合类型只需要修正引用
                    ObjectPath objectPath = outputField.objectPathValue;
                    if (objectPath.HasCollection) {
                        continue;
                    }
                    if (idMap.TryGetValue(objectPath.localId, out long newLocalId)) {
                        objectPath.localId = newLocalId;
                    } else {
                        objectPath.localId = 0;
                    }
                    outputField.objectPathValue = objectPath;
                    continue;
                }
                // 集合类型，需要删除无效引用
                for (int index = outputField.Count - 1; index >= 0; index--) {
                    ObjectPath objectPath = outputField[index].objectPathValue;
                    if (objectPath.HasCollection) {
                        continue;
                    }
                    if (idMap.TryGetValue(objectPath.localId, out long newLocalId)) {
                        objectPath.localId = newLocalId;
                        outputField[index].objectPathValue = objectPath;
                    } else {
                        outputField.RemoveAt(index);
                    }
                }
            }
        }
        return destNodes;
    }
}
}