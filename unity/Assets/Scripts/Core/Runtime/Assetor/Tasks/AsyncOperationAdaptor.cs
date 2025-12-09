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
using UnityEngine;

namespace Wjybxx.BigCat.Assetor.Tasks
{
/// <summary>
/// Unity异步操作适配器
///
/// 注；只适用简单的场景，不支持异步转同步。
/// </summary>
public class AsyncOperationAdaptor : ResourceTask
{
    private readonly Func<object, AsyncOperation> _factory;
    private readonly object _ctx;
    private AsyncOperation _asyncOperation;

    public AsyncOperationAdaptor(Func<object, AsyncOperation> factory, object ctx) {
        _factory = factory;
        _ctx = ctx;
    }

    protected override void Enter(int reentryId) {
        _asyncOperation = _factory(_ctx);
        _asyncOperation.priority = Priority;
    }

    protected override void Execute() {
        promise.progress = _asyncOperation.progress;
        if (_asyncOperation.isDone) {
            promise.progress = 1f;
            SetSuccess();
        }
    }

    protected override void Exit() {
        _asyncOperation = null;
    }
}
}