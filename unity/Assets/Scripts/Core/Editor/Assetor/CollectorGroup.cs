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
using Wjybxx.BTree;
using Wjybxx.BTree.Branch;
using Wjybxx.Commons;
using Wjybxx.Dson.Codec.Attributes;
using Blackboard = Wjybxx.BigCat.Util.Blackboard;

namespace Wjybxx.BigCat.Editor.Assetor
{
/// <summary>
/// 收集器组
/// </summary>
[DsonSerializable(NamespaceAliases = new string[]
{
    "BigCatUtil = Wjybxx.BigCat.Util",
})]
public class CollectorGroup : Sequence<Blackboard>
{
    /// <summary>
    /// 启用状态
    /// </summary>
    public bool enabled;
    /// <summary>
    /// 为Bundle附加的标签
    /// </summary>
    public HashSet<string> bundleTags = new HashSet<string>();

    /// <summary>
    /// 收集之前的预处理任务（用于导入外部资源文件）
    /// </summary>
    [SerializeReference]
    public List<Task<Blackboard>> preTasks = new();
    /// <summary>
    /// 关联的收集器
    /// </summary>
    [SerializeReference]
    public List<Collector> collectors = new();
    /// <summary>
    /// 资产分类器
    /// </summary>
    [SerializeReference]
    public IAssetClassifier classifier;

    protected override void Enter(int reentryId) {
        children.Clear();
        children.AddRange(preTasks);
        children.AddRange(collectors);
        if (enabled) {
            base.Enter(reentryId);
        } else {
            SetSuccess();
        }
    }
}
}