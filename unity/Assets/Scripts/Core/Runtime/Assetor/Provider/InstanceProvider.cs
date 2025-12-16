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
using Wjybxx.BTree;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 实例对象提供者
/// </summary>
public class InstanceProvider : Provider
{
    private static readonly Blackboard sharedBlackboard = new Blackboard();
    private static readonly CancelToken sharedCancelToken = new CancelToken();
    //
    internal readonly AssetHandle backendHandle;

    public InstanceProvider(ResourceManager resourceMgr, ProviderId pid,
                            AssetHandle backendHandle, UnityEngine.Object inst)
        : base(resourceMgr, pid) {
        this.backendHandle = backendHandle;
        this.promise.result = inst;
        backendHandle.Retain();
        // 避免不必要的分配
        this.IsManualCheckCancel = true;
        blackboard = sharedBlackboard;
        cancelToken = sharedCancelToken;
    }

    public override void Destroy() {
        if (IsDestroyed) return;
        IsDestroyed = true;
        backendHandle.Release();
    }

    protected override void Execute() {
        SetSuccess();
    }
}
}