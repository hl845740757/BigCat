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
/// 数据模型解析器
/// 
/// 注：定义为接口，以支持用户实现为<see cref="UnityEngine.MonoBehaviour"/>。
/// </summary>
public interface IDataModelResolver
{
    /// <summary>
    /// 数据模型解析器
    ///
    /// 注：如果父数据模型为null，则访问聚合数据模型。
    /// </summary>
    /// <param name="aggregationModel">总聚合模型</param>
    /// <param name="parentModel">父节点数据</param>
    /// <param name="dataAddress">当前节点的数据地址</param>
    /// <param name="uiIndex">ui元素的索引</param>
    public object Resolve(IAggregationModel aggregationModel, object? parentModel, string dataAddress, int uiIndex = -1);
}
}