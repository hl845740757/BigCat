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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Wjybxx.BTree;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Logger;
using TaskStatus = Wjybxx.BTree.TaskStatus;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 任务调度器
///
/// 注：
/// 1.虽然目前兼容<see cref="ResourceTask"/>和普通Task，但尽量都使用<see cref="ResourceTask"/>。
/// 2.不建议复用该对象，资源任务有复杂的状态，很难保证Reset的正确性；当需要清理数据时，停止当前调度器，创建新的调度器即可。
/// </summary>
public sealed class TaskScheduler : BranchTask<Blackboard>
{
    private static readonly ILogger logger = LoggerFactory.GetLogger(typeof(TaskScheduler));
    public static TaskScheduler Current { get; set; } // 注：尽量不使用
    //
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private readonly List<ResourceTask> _delayedNotifyTasks = new List<ResourceTask>();
    //
    private long _frameTime;
    private long _maxTimeSlice = 100;
    private bool _needSort;

    /// <summary>
    /// 单帧最大时间片
    /// </summary>
    public long MaxTimeSlice {
        get => _maxTimeSlice;
        set => _maxTimeSlice = value;
    }

    /// <summary>
    /// 当前帧时间
    /// </summary>
    public long FrameTime {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _frameTime;
    }
    /// <summary>
    /// 真实时间
    /// </summary>
    public long RealTime {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _stopwatch.ElapsedMilliseconds;
    }

    /// <summary>
    /// 重置数据
    /// </summary>
    public override void ResetForRestart() {
        // 子节点都不重用
        children.Clear();
        base.ResetForRestart();
        //
        _stopwatch.Reset();
        _delayedNotifyTasks.Clear();
        _frameTime = 0;
        _needSort = false;
    }

    protected override void Enter(int reentryId) {
        _stopwatch.Restart();
    }

    /// <summary>
    /// 调度所有任务
    /// </summary>
    protected override void Execute() {
        _frameTime = RealTime;
        // 暂时忽略回调任务的耗时
        CheckNotifyTasks();
        if (_needSort) {
            _needSort = false;
            children.Sort(TaskComparator.Inst);
        }
        // 新增的任务也可以在当前帧执行，只要还有可运行时间
        for (int index = 0; index < children.Count; index++) {
            Task<Blackboard> child = children[index];
            try {
                if (child.Status == TaskStatus.NEW) {
                    child.IsBreakInline = true; // 忽略内联-避免不必要的回调
                    Template_StartChild(child, true);
                } else if (child.IsRunning) {
                    child.Template_Execute(true);
                }
            }
            catch (Exception ex) {
                logger.Warn(ex, "task.Execute caught exception");
                child.SetFailed(TaskStatus.ERROR); // 强制失败
            }
            if (child.IsCompleted) {
                children.RemoveAt(index--);
            }
            // 检测超时
            if (RealTime - _frameTime >= _maxTimeSlice) {
                break;
            }
        }
        CheckNotifyTasks();
    }

    protected override int AddChildImpl(Task<Blackboard> task) {
        // 插入高优先级任务的情况下才需要重排序
        // 理论上也可以采用插入排序，但如果添加元素的频率较高，和延迟排序的开销不好评估
        int index = base.AddChildImpl(task);
        if (!_needSort && children.Count > 1
                       && TaskComparator.Inst.Compare(task, children[index - 1]) < 0) {
            _needSort = true;
        }
        task.SetControl(this);
        task.Blackboard ??= new Blackboard();
        task.CancelToken ??= new CancelToken();
        return index;
    }

    protected override void OnChildRunning(Task<Blackboard> child, bool starting) {
    }

    protected override void OnChildCompleted(Task<Blackboard> child) {
        if (child is ResourceTask task) {
            task.promise.TrySetResult(null); // promise优先
            task.NotifyListeners();
        }
    }

    protected override void OnEventImpl(object eventObj) {
    }

    /// <summary>
    /// 通知监听器
    /// </summary>
    private void CheckNotifyTasks() {
        if (_delayedNotifyTasks.Count == 0) return;
        for (int index = 0; index < _delayedNotifyTasks.Count; index++) {
            ResourceTask task = _delayedNotifyTasks[index];
            task.NotifyListeners();
        }
        _delayedNotifyTasks.Clear();
    }

    /// <summary>
    /// 延迟通知监听器
    /// </summary>
    /// <param name="task"></param>
    public void DelayNotifyListener(ResourceTask task) {
        Debug.Assert(task.IsCompleted);
        _delayedNotifyTasks.Add(task);
    }

    /// <summary>
    /// 任务优先级改变事件，需要重排序任务队列
    /// </summary>
    public void OnPriorityChanged(ResourceTask task, int prev) {
        if (task.Control == this && !task.IsCompleted) { // 可能尚未运行，因此不能测试Running
            _needSort = true;
        }
    }

    /// <summary>
    /// 阻塞等待任务完成
    /// </summary>
    /// <param name="task">要等待的任务</param>
    /// <param name="deadline">截止时间</param>
    /// <exception cref="TimeoutException">执行超时</exception>
    /// <exception cref="BlockingOperationException">当前不支持同步等待</exception>
    public void WaitForCompletion(Task<Blackboard> task, long deadline) {
        if (task.IsCompleted) return;
        //
        Blackboard blackboard = task.Blackboard;
        if (blackboard.isWaitForCompletion) {
            throw new Exception("Recursively call WaitForComplete");
        }
        blackboard.isWaitForCompletion = true;
        blackboard.stopwatch = _stopwatch;
        blackboard.deadline = deadline;
        try {
            if (task.Status == TaskStatus.NEW) {
                task.IsBreakInline = true;
                Template_StartChild(task, true);
            } else if (task.IsRunning) {
                task.Template_Execute(true);
            }
        }
        catch (Exception ex) {
            if (ex is BlockingOperationException || ex is TimeoutException) {
                throw;
            }
            logger.Warn(ex, "task.Execute caught exception");
            task.SetFailed(TaskStatus.ERROR);
        }
        finally {
            blackboard.isWaitForCompletion = false;
            blackboard.stopwatch = null;
            blackboard.deadline = 0;
        }
        // 如果没有抛出超时异常，也没有进入完成状态，则证明不支持同步完成
        if (!task.IsCompleted) {
            throw new BlockingOperationException($"Task {task.GetType()} does not support synchronous completion");
        }
    }

    private class TaskComparator : IComparer<Task<Blackboard>>
    {
        public static readonly TaskComparator Inst = new TaskComparator();

        public int Compare(Task<Blackboard> x, Task<Blackboard> y) {
            if (x == y) return 0;
            if (x is ResourceTask lhs) {
                if (y is ResourceTask rhs) {
                    int r = lhs.Priority.CompareTo(rhs.Priority);
                    return r != 0 ? r : lhs.taskId.CompareTo(rhs.taskId);
                }
                return -1;
            }
            return y is ResourceTask ? 1 : 0;
        }
    }
}
}