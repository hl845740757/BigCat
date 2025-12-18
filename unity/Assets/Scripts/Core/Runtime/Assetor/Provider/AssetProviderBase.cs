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

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 普通资产提供者基类
///
/// <h3>同步加载问题</h3>
/// 如果创建Provider的是同步加载请求，那么其Bundle加载会被强制为同步的；
/// 如果关联Bundle已处于异步加载状态，则抛出异常 —— 新版本Unity不再支持Bundle异步转同步；
/// 也就是说，普通资产对象同步加载的前提是：Bundle已加载。
///
/// <h3>引用计数</h3>
/// 资产Provider在构造函数中增加对Bundle的引用计数，在销毁时释放对Bundle的引用计数。
///
/// <h3>取消信号</h3>
/// 由于Bundle是共享的，因此AssetProvider的取消信号不能发布到BundleProvider，
/// 只能传播给自己的子任务。
/// </summary>
public abstract class AssetProviderBase : Provider
{
    /// <summary>
    /// 关联的资产文件信息
    /// </summary>
    public readonly AssetFileInfo assetInfo;
    /// <summary>
    /// 关联的Bundle
    /// 注：外部先创建BundleProvider，可确保优先级相同时排前面。
    /// </summary>
    public readonly BundleProvider bundleProvider;

    protected AssetProviderBase(ResourceManager resourceMgr, ProviderId pid,
                                AssetFileInfo assetInfo, BundleProvider bundleProvider)
        : base(resourceMgr, pid) {
        this.assetInfo = assetInfo;
        this.bundleProvider = bundleProvider;
        bundleProvider.Retain();
    }

    /// <summary>
    /// 资产全路径（规格化的路径）
    /// </summary>
    public string assetPath => pid.assetPath;
    /// <summary>
    /// 请求的资产类型
    /// </summary>
    public System.Type assetType => pid.assetType;
    /// <summary>
    /// 请求的加载方式
    /// </summary>
    public ELoadMethod loadMethod => pid.loadMethod;

    protected override void OnPriorityChanged(int prevValue) {
        base.OnPriorityChanged(prevValue);
        // 确保Bundle加载器的任务处于较高的优先级
        if (bundleProvider.Priority > Priority) {
            bundleProvider.Priority = Priority;
        }
    }

    protected override void Enter(int reentryId) {
        if (bundleProvider.Priority > Priority) {
            bundleProvider.Priority = Priority;
        }
    }

    public override void Destroy() {
        if (IsDestroyed) return;
        IsDestroyed = true;
        bundleProvider.Release();
    }
}
}