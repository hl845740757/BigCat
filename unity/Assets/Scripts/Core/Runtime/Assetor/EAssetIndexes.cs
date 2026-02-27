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
    FileName = 0x01,
    /// <summary>
    /// 文件夹+文件名索引
    /// </summary>
    FolderAndFileName = 0x02,
    /// <summary>
    /// 相对指定祖先节点的路径
    /// 
    /// 1.使用该索引方式时，应当避免再启用前一种索引
    /// 2.该索引打包时必须唯一(长路径索引必须唯一)，运行时仍然允许覆盖
    /// </summary>
    RelativeToAncestor = 0x04,
    /// <summary>
    /// 相对收集器目录的Path
    /// 
    /// 1.该方式与打包有一定的依赖，尽量减少使用
    /// 2.该索引打包时必须唯一(长路径索引必须唯一)，运行时仍然允许覆盖
    /// </summary>
    RelativeToCollector = 0x08,
    /// <summary>
    /// 资产对象类型名 + 文件名(无后缀) 
    ///
    /// <code>SpriteGroup:sm_body8001</code>
    /// 0.该索引主要解决自定义资产文件只能使用asset扩展名导致的索引冲突问题。
    /// 1.开启该索引时，应当避免再开启文件名索引。
    /// 2.支持<see cref="AssetTypeAliasAttribute"/>指定额外别名。
    /// </summary>
    TypeAndFileName = 0x10,
    /// <summary>
    /// 资产对象类型名 + 文件夹 + 文件名(无后缀)
    /// 
    /// <code>AudioClip:Music/Login</code>
    /// 0.该索引主要解决具有多扩展名类型资产的索引问题。
    /// 1.开启该索引时，应当避免再开启文件夹索引。
    /// 2.支持<see cref="AssetTypeAliasAttribute"/>指定额外别名。
    /// </summary>
    TypeAndFolderName = 0x20,
}
}