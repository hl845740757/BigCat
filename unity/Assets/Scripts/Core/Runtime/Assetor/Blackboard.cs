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
using System.Diagnostics;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 任务黑板
///
/// 注：此处不需要高度灵活的黑板，平铺字段开销更低。
/// </summary>
public class Blackboard
{
    public bool isWaitForCompletion;
    public Stopwatch stopwatch;
    public long deadline;

    /// <summary>
    /// 检查是否超时
    /// </summary>
    public void CheckTimeout() {
        if (deadline > 0 && stopwatch != null
                         && stopwatch.ElapsedMilliseconds >= deadline) {
            throw new TimeoutException("Timeout waiting for complete");
        }
    }
}
}