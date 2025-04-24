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
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Inject;

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// Node用于管理Worker的数据
///
/// 1.上下文中的数据只应Node读写，因此不保证对其它线程的可见性
/// 2.该对象仅用于底层Node和Worker交互，用户不应该使用
/// </summary>
[NotThreadSafe]
public sealed class WorkerControlData
{
#nullable disable
    internal S2SRpcClient rpcClient;
    internal bool? manualClose;

    /// <summary>
    /// 是否手动关闭
    /// </summary>
    internal bool IsManualClose => manualClose != null && manualClose.Value;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="worker"></param>
    /// <param name="unstarted">是否尚未启动</param>
    internal void Init(Worker worker, bool unstarted) {
        // 如果未设置手动关闭，则在Worker已启动的情况下默认手动关闭
        if (manualClose == null) {
            manualClose = !unstarted;
        }
        this.rpcClient = (S2SRpcClient)worker.Injector.GetInstance<RpcClient>();
    }
}
}