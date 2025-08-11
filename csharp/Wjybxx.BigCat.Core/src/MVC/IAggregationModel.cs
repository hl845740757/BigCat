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


namespace Wjybxx.BigCat.MVC
{
/// <summary>
/// 聚合模型
///
/// 0.包含所有Window的依赖。
/// 1.接口不能约定具体的类型，因此建议实现类隐式实现接口，提供具体类型的返回值。
/// 2.绝大部分Node都可以直接基于逻辑层数据绘制UI，少部分可能需要额外的数据支持。
/// 3.不那么追求效率的情况下，Managers可以是Injector类型。
/// </summary>
public interface IAggregationModel
{
    /// <summary>
    /// 视图层数据模型（总入口）
    /// </summary>
    object ViewModel { get; }
    /// <summary>
    /// 视图层的各种管理器
    /// </summary>
    object ViewManagers { get; }

    /// <summary>
    /// 逻辑层数据模型（总入口）
    /// </summary>
    object LogicModel { get; }
    /// <summary>
    /// 逻辑层的各种管理器
    /// </summary>
    object LogicManagers { get; }
}
}