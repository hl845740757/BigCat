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
/// 资产文件的索引方式
/// 
/// 1.资产索引不提供唯一性保证，也不建议维护唯一索引
/// 2.同类型索引之间，索引重复时，排在后面的Asset文件覆盖前面的。
/// 3.默认索引都包含文件扩展名，通过配置文件指定哪些类型的文件可以建立无扩展名的索引。
/// </summary>
[Flags]
public enum EAssetIndexes
{
    None = 0,
    /// <summary>
    /// 文件名索引
    /// </summary>
    FileName = 1,
    /// <summary>
    /// 文件夹+文件名索引
    /// </summary>
    FolderAndFileName = 2,
    /// <summary>
    /// 支持索引深度
    /// 
    /// 注：
    /// 1.使用该索引方式时，应当避免再启用前一种索引
    /// 2.该索引必须唯一（打包时），长路径索引必须唯一；运行时仍然允许覆盖
    /// </summary>
    FolderAndFileNamePlus = 3,
    /// <summary>
    /// 相对收集器目录的Path
    /// 注：
    /// 1.该方式与打包有一定的依赖，尽量减少使用
    /// 2.该索引必须唯一（打包时），长路径索引必须唯一；运行时仍然允许覆盖
    /// </summary>
    RelativeToCollector = 4,
}
}