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

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// Bundle管理器
///
/// 注：该管理器为独立服务，可以不依赖其它服务运行。
/// </summary>
public interface IBundleManager
{
    /// <summary>
    /// 启用程序
    ///
    /// 注：主要为初始化Bundle信息。
    /// </summary>
    /// <returns></returns>
    ResourceTask Start();

    /// <summary>
    /// 停止程序
    /// </summary>
    ResourceTask Stop();

    /// <summary>
    /// 同步加载Bundle
    /// </summary>
    /// <returns>如果不存在关联的Bundle，则返回null</returns>
    IAssetBundle LoadBundle(AssetBundleInfo bundleInfo);

    /// <summary>
    /// 异步加载Bundle
    /// 
    /// 注：
    /// 1.新版本Unity的Bundle异步加载是真异步，任务执行期间无法转同步。
    /// 2.需要测试bundle的crc校验码是否匹配。
    /// </summary>
    /// <returns>如果不存在关联的Bundle，则返回null</returns>
    ResourceTask LoadBundleAsync(AssetBundleInfo bundleInfo);
}
}