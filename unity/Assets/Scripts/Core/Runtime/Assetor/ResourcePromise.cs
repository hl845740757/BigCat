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
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 资源加载Promise
///
/// 1.避免直接将Promise暴露给用户，否则需要大量的封装。
/// 2.用户的普通回调不能注册到这里，因为不支持删除；Await回调可以注册到这里，在任务完成的情况下会立即执行。
/// 2.继承<see cref="Promise{T}"/>是为了支持await操作，避免再分配一个对象。
/// 3.Task只需将任务结果赋值到<see cref="result"/>，由调度器发布到Promise。
/// </summary>
public sealed class ResourcePromise<T> : Promise<T>
{
    /// <summary>
    /// 任务阶段
    /// </summary>
    public ELoadPhase phase;
    /// <summary>
    /// 任务进度(不可用于判断任务是否结束)
    /// </summary>
    public float progress;
    /// <summary>
    /// 总数量
    /// </summary>
    public long totalCount;
    /// <summary>
    /// 已处理数量(成功 + 失败)
    /// </summary>
    public long processedCount;
    /// <summary>
    /// 失败数量
    /// </summary>
    public long failedCount;
    /// <summary>
    /// 任务的执行结果
    /// </summary>
    public T result;

    public void ClearProgress() {
        this.progress = 0;
        this.totalCount = 0;
        this.processedCount = 0;
        this.failedCount = 0;
    }

    public void SyncProgressFrom(ResourcePromise<T> source) {
        this.progress = source.progress;
        this.totalCount = source.totalCount;
        this.processedCount = source.processedCount;
        this.failedCount = source.failedCount;
    }
}
}