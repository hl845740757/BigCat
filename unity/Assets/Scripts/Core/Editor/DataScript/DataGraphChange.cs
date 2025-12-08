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

namespace Wjybxx.BigCat.Editor.DataScript
{
/// <summary>
/// 由于后续的场景编辑器也依赖数据图，为保持场景数据和GraphView的数据同步，还是需要简单的MVC架子
///
/// 注：
/// 1.用户不可以保留这部分List的引用，也不应该修改其中的数据。
/// 2.为避免一致性问题，我们不为<see cref="DataNode"/>设计单独的数据变化事件。
/// </summary>
public class DataGraphChange
{
    public List<DataNode> insetNodes;
    public List<DataNode> deleteNodes;
    public List<DataNode> updateNodes;
    public Dictionary<long, long> prevLocalIds;

    /// <summary>
    /// 由于事件对象可能是被池化的，因此通过属性测试代替null测试
    /// </summary>
    public bool hasInsertNodes => insetNodes != null && insetNodes.Count > 0;
    public bool hasDeleteNodes => deleteNodes != null && deleteNodes.Count > 0;
    public bool hasUpdateNodes => updateNodes != null && updateNodes.Count > 0;
    public bool hasLocalIds => prevLocalIds != null && prevLocalIds.Count > 0;

    /// <summary>
    /// 是否是数据变化的Node之一
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public bool IsUpdated(DataNode node) {
        return updateNodes != null && updateNodes.Contains(node);
    }

    /// <summary>
    /// 查询NodeId是否发生变更
    /// </summary>
    /// <returns></returns>
    public bool IsLocalIdChanged(DataNode node) {
        return prevLocalIds != null && prevLocalIds.ContainsKey(node.localId);
    }

    /// <summary>
    /// 获取Node的前一个id
    /// </summary>
    /// <returns></returns>
    public bool GetPrevLocalId(DataNode node, out long prevLocalId) {
        if (prevLocalIds != null && prevLocalIds.TryGetValue(node.localId, out prevLocalId)) {
            return true;
        }
        prevLocalId = 0;
        return false;
    }

    #region 池化

    internal static DataGraphChange Create() {
        return new DataGraphChange()
        {
            insetNodes = new List<DataNode>(),
            deleteNodes = new List<DataNode>(),
            updateNodes = new List<DataNode>(),
            prevLocalIds = new Dictionary<long, long>()
        };
    }

    internal bool IsEmpty => insetNodes.Count == 0
                             && deleteNodes.Count == 0
                             && updateNodes.Count == 0;

    internal void Clear() {
        insetNodes.Clear();
        deleteNodes.Clear();
        updateNodes.Clear();
        prevLocalIds.Clear();
    }

    #endregion
}
}