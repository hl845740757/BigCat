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
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.UI
{
/// <summary>
/// 窗口加载器
///
/// 注：
/// 1.暂不考虑Window池化，超过最大生命周期时直接销毁。
/// 2.虽然命名为Load，实际应该返回资源的副本对象 —— 即对资源进行实例化。
/// 3.尽量实现加载超时功能。
/// </summary>
public interface WindowLoader
{
    /// <summary>
    /// 同步加载窗口
    /// </summary>
    /// <param name="windowAddr">窗口地址</param>
    /// <param name="timeout">超时时间</param>
    /// <returns></returns>
    GameObject Load(string windowAddr, double timeout = 0);

    /// <summary>
    /// 异步加载窗口
    /// </summary>
    /// <param name="windowAddr">窗口地址</param>
    /// <param name="timeout">超时时间</param>
    /// <returns></returns>
    ValueFuture<GameObject> LoadAsync(string windowAddr, double timeout = 0);
}
}