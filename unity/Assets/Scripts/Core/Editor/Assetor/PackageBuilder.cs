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

using Wjybxx.BigCat.Util;
using Wjybxx.BTree.Branch;
using Wjybxx.Commons;
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.BigCat.Editor.Assetor
{
[DsonSerializable(NamespaceAliases = new string[]
{
    "BigCatUtil = Wjybxx.BigCat.Util",
})]
public class PackageBuilder : Sequence<Blackboard>
{
    /// <summary>
    /// 资源收集管线
    /// </summary>
    [SerializeReference]
    public CollectorPackage package;
    /// <summary>
    /// 打包管线
    /// </summary>
    [SerializeReference]
    public BuildPipelineTask buildTask;

    protected override void BeforeEnter() {
        base.BeforeEnter();
        children.Clear();
        AddChild(package);
        AddChild(buildTask);
    }
}
}