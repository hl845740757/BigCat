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
using Wjybxx.BTree;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 用于驱动<see cref="TaskScheduler"/>
///
/// 注：用户项目中可以不使用该MonoBehavior，仿照实现即可。
/// </summary>
public sealed class MonoScheduler : MonoBehaviour
{
    private TaskEntry<Blackboard> _taskEntry;
    private TaskScheduler _scheduler;

    /// <summary>
    /// 关联的调度器
    /// </summary>
    public TaskScheduler Scheduler => _scheduler;

    private void Awake() {
        _scheduler = new TaskScheduler();
        _taskEntry = new TaskEntry<Blackboard>()
        {
            RootTask = _scheduler,
            Blackboard = new Blackboard()
        };
        TaskScheduler.Current ??= _scheduler;
        _taskEntry.Update(); // 不可延迟到Start方法启动
    }

    private void Update() {
        _taskEntry.Template_Execute(false);
    }

    private void OnDestroy() {
        if (TaskScheduler.Current == _scheduler) {
            TaskScheduler.Current = null;
        }
    }
}
}