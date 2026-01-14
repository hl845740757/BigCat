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
/// 异常提供者（用于表示资源不存在）
///
/// 注：该Provider可以不显式释放，因为数量很少，使得我们可以随时查看当前的资源缺失信息。
/// </summary>
public class ErrorProvider : Provider
{
    private static readonly Blackboard sharedBlackboard = new Blackboard();
    private static readonly CancelToken sharedCancelToken = new CancelToken();

    private readonly ResourceErrorCode _errorCode;

    public ErrorProvider(ResourceManager resourceMgr, ProviderId pid,
                         ResourceErrorCode errorCode = ResourceErrorCode.AssetFileNotFound)
        : base(resourceMgr, pid) {
        _errorCode = errorCode;
        // 避免不必要的分配
        this.IsManualCheckCancel = true;
        blackboard = sharedBlackboard;
        cancelToken = sharedCancelToken;
    }

    protected override void Execute() {
        promise.errorCode = _errorCode;
        SetFailed(TaskStatus.ERROR);
    }
}
}