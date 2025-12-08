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
using Wjybxx.BigCat.Core;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 资产Bundle抽象
///
/// 注：
/// 1.由于外部建立了全局索引，因此Bundle内部可以只根据AssetPath查询。
/// 2.Bundle抽象的主要作用是屏蔽Editor和Runtime模式资源加载的差异。
/// </summary>
public interface IAssetBundle
{
    /// <summary>
    /// 卸载Bundle
    ///
    /// 注：卸载Bundle通常很快，无需异步卸载。
    /// </summary>
    void UnloadBundle(bool unloadAllLoadedObjects);

    #region unity资产

    /// <summary>
    /// 加载主资产
    ///
    /// 注：
    /// 1.理论上外部建立了索引的情况下，不应该请求加载不存在的资产，但资产类型仍然可能不匹配。
    /// 2.资源类型不匹配返回null
    /// 3.由于异步操作需要支持转同步，且同步加载的需求较少，因此无需定义同步操作。
    /// </summary>
    /// <returns>如果资产不存在，则返回null</returns>
    ResourceTask LoadAssetAsync(string assetPath, Type assetType);

    /// <summary>
    /// 加载主资源和子资源
    /// </summary>
    ResourceTask LoadAssetWithSubAssetsAsync(string assetPath, Type assetType);

    /// <summary>
    /// 加载Bundle内指定类型的所有资产对象
    ///
    /// 注：
    /// 1.通常用于加载指定文件夹内的所有自定义资产，只有Bundle与文件夹绑定的情况下可用。
    /// 2.通过在Bundle打包文件夹内放置一个索引文件来撬动整个Bundle的加载。
    /// </summary>
    ResourceTask LoadAllAssetsAsync(Type assetType);

    #endregion

    #region 原始二进制资产

    /// <summary>
    /// 读取指定二进制资产
    /// </summary>
    /// <param name="assetPath">资产路径</param>
    /// <returns></returns>
    public BinaryAsset LoadBinaryAsset(string assetPath);

    /// <summary>
    /// 读取bundle内的所有二进制资产
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<BinaryAsset> LoadAllBinaryAssets();

    #endregion
}
}