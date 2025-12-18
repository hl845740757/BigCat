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

using UnityEngine;
using Wjybxx.BigCat.Assetor;

namespace Wjybxx.BigCat.UI
{
/// <summary>
/// 窗口加载器
///
/// 注：可以通过地址解析，实现同一个Window打开多份
/// </summary>
public interface WindowLoader
{
    /// <summary>
    /// 加载窗口
    /// </summary>
    /// <param name="windowAddr">窗口地址</param>
    /// <param name="timeout">超时时间，尽量实现</param>
    /// <returns></returns>
    AssetHandle LoadAsync(string windowAddr, double timeout = 0);

    /// <summary>
    /// 实例化窗口预制件
    /// 
    /// 注：用于初始化特殊数据
    /// </summary>
    /// <param name="windowAddr">关联</param>
    /// <param name="prefab">关联预制件</param>
    /// <param name="uiRoot">ui根对象</param>
    /// <returns></returns>
    GameObject Instantiate(string windowAddr, GameObject prefab, Transform uiRoot) {
        return Object.Instantiate(prefab, uiRoot);
    }
}

/// <summary>
/// 默认额窗口加载器
/// </summary>
public class DefaultWindowLoader : WindowLoader
{
    public AssetHandle LoadAsync(string windowAddr, double timeout = 0) {
        return ResourceManager.Inst.LoadAssetAsync<GameObject>(windowAddr);
    }
}
}