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
using Wjybxx.BTree;
using Blackboard = Wjybxx.BigCat.Util.Blackboard;

namespace Wjybxx.BigCat.Editor.Assetor
{
internal static class BuildUtil
{
    /// <summary>
    /// 查找父级中的指定类型节点
    /// </summary>
    public static T GetFirstAncestorOfType<T>(this Task<Blackboard> task) where T : class {
        Task<Blackboard> control = task.Control;
        if (control is T group) {
            return group;
        }
        while ((control = control.Control) != null) {
            if (control is T group2) {
                return group2;
            }
        }
        return null;
    }
}

/// <summary>
/// 黑板键
/// </summary>
internal static class BuildKeys
{
    /// <summary>
    /// 当前构建的包裹信息
    /// </summary>
    public static readonly DataKey<BuildPackageInfo> packageInfo
        = DataKeys.NewObjectKey<BuildPackageInfo>("packageInfo");
    /// <summary>
    /// 所有的资产路径缓存
    /// </summary>
    public static readonly DataKey<string[]> allAssetPaths
        = DataKeys.NewObjectKey<string[]>("allAssetPaths");
    /// <summary>
    /// 规格化的资产路径缓存
    /// </summary>
    public static readonly DataKey<PathCache> pathCache
        = DataKeys.NewObjectKey<PathCache>("pathCache");
    /// <summary>
    /// 依赖缓存
    /// </summary>
    public static readonly DataKey<DependencyCache> dependencyCache
        = DataKeys.NewObjectKey<DependencyCache>("dependencyCache");
}
}