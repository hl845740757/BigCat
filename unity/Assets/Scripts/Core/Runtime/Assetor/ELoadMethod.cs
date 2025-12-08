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
/// 资源对象加载方式
/// </summary>
public enum ELoadMethod : byte
{
    None = 0,
    LoadAsset,
    LoadAssetWithSubAssets,
    LoadAllAssets, // Editor并无直接对应API，不推荐使用
    LoadSceneAsset, // 加载Scene关联的资产(bundle)
    LoadBinaryAsset, // 加载指定二进制资产
    LoadAllBinaryAssets, // 加载Bundle内所有二进制资产
    LoadBundle, // 加载Bundle - 内部使用
    InstHandle, // 资产实例句柄
}
}