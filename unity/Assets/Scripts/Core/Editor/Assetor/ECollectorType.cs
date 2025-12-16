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

namespace Wjybxx.BigCat.Editor.Assetor
{
/// <summary>
/// 收集器类型
/// </summary>
public enum ECollectorType
{
    /// <summary>
    /// 动态主资产，会写入到清单的资源列表（可以通过代码加载）
    /// </summary>
    MainAsset = 0,
    /// <summary>
    /// Bundle级依赖资产（捆绑依赖），文件夹内所有资产都会被打包，不可以通过代码加载
    ///
    /// 注：
    /// 1.默认情况下文件夹内所有资产都会被打包，可以通过Ignore文件忽略特定文件。
    /// 2.主要用于保证依赖完整性，同一个文件夹与<see cref="DependAsset"/>模式互斥。
    /// </summary>
    DependBundle = 1,
    /// <summary>
    /// 文件级依赖资产，不可以通过代码加载
    ///
    /// 注：
    /// 1.只有编译期被主资产依赖的资产才会被打包。
    /// 2.由代码动态加载的资源在构建阶段无法分析到，因此可能导致资源遗漏。
    /// 3.额外的依赖分析会大幅影响打包速度，尤其是Editor模拟打包时。
    /// </summary>
    DependAsset = 2,
    /// <summary>
    /// 原始文件资产，会写入到清单的资源列表（可以通过代码加载）
    ///
    /// 注：默认情况下文件夹内所有资产都会被打包，可以通过Ignore文件忽略特定文件。
    /// </summary>
    RawFile = 3,
}
}