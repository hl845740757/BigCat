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
/// 资源加载错误码
/// </summary>
public enum ResourceErrorCode
{
    /// <summary>
    /// 未知 
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Bundle加载失败
    /// </summary>
    BundleLoadFailed = 1,
    /// <summary>
    /// Bundle下载失败
    /// </summary>
    BundleDownloadFailed = 2,
    /// <summary>
    /// Bundle验证失败(CRC32)
    /// </summary>
    BundleVerifyFailed = 3,
    /// <summary>
    /// Bundle重命名失败(临时文件转正式文件失败)
    /// </summary>
    BundleRenameFailed = 4,
    /// <summary>
    /// Bundle导入失败
    /// </summary>
    BundleImportFailed = 5,
    /// <summary>
    /// Bundle文件不存在
    /// </summary>
    BundleFileNotFound = 6,
    /// <summary>
    /// 找不到资产文件
    /// </summary>
    AssetFileNotFound = 7,
}
}