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
/// 资源加载Promise
///
/// 注：
/// 1.避免直接将Promise暴露给用户，否则需要大量的封装 -- 用户总是通过<see cref="AssetHandle"/>访问。
/// 2.Task只需要将结果写入Promise，由调度器通知监听器。
/// </summary>
public sealed class ResourcePromise<T>
{
    /// <summary>
    /// 任务阶段
    /// </summary>
    public ELoadStatus status;
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
    /// <summary>
    /// 错误码
    /// </summary>
    public ResourceErrorCode errorCode;

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