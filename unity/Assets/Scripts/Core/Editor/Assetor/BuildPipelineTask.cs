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
using UnityEditor;
using Wjybxx.BTree;
using Wjybxx.BTree.Branch;
using Wjybxx.Commons;
using Wjybxx.Dson.Codec.Attributes;
using Blackboard = Wjybxx.BigCat.Util.Blackboard;

namespace Wjybxx.BigCat.Editor.Assetor
{
/// <summary>
/// 构建管线
///
/// 注：可以通过<see cref="Task{T}.Name"/>区别Editor管线和真实管线。
/// </summary>
[DsonSerializable(NamespaceAliases = new string[]
{
    "BigCatUtil = Wjybxx.BigCat.Util",
})]
public class BuildPipelineTask : SingleRunningChildBranch<Blackboard>
{
    /// <summary>
    /// 构建目标
    /// </summary>
    public BuildTarget buildTarget;
    /// <summary>
    /// 压缩方式
    /// </summary>
    public ECompression compression;
    /// <summary>
    /// 构建前的预处理任务
    /// </summary>
    [SerializeReference]
    public List<Task<Blackboard>> preBuildTasks = new();
    /// <summary>
    /// 构建任务
    /// </summary>
    [SerializeReference]
    public List<Task<Blackboard>> buildTasks = new();
    /// <summary>
    /// 构建后的钩子任务(加密 + 拷贝)
    /// </summary>
    [SerializeReference]
    public List<Task<Blackboard>> postBuildTasks = new();

    protected override void BeforeEnter() {
        base.BeforeEnter();
        children.Clear();
        children.AddRange(preBuildTasks);
        children.AddRange(buildTasks);
        children.AddRange(postBuildTasks);
        //
        BuildPackageInfo packageInfo = blackboard.Get(BuildKeys.packageInfo);
        if (packageInfo == null) {
            throw new InvalidOperationException("packageInfo is missing");
        }
        packageInfo.buildTime = DateTime.Now.ToString("s");
        //
        // 检测当前是否正在构建资源包
        if (UnityEditor.BuildPipeline.isBuildingPlayer) {
            throw new Exception("The pipeline is building, please try again after finish !");
        }
    }
}
}