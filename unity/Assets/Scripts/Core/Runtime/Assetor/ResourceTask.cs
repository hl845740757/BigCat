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
using System.Runtime.CompilerServices;
using Wjybxx.BTree;
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BigCat.Assetor
{
/// <summary>
/// 资源加载任务的基类
///
/// 注：
/// 1.为了减少开销和抽象，内部对象之间直接通过<see cref="ResourceTask"/>交互。
/// 2.子类如果有多子节点的需求，可以组合<see cref="BranchTask{T}"/>实现。
/// 3.为避免过多的类型，该类不实现为泛型(Promise参数)，约定好返回值的真实类型即可。
/// 4.需要关注<see cref="Blackboard"/>中的异步转同步请求。
/// </summary>
public abstract class ResourceTask : Decorator<Blackboard>
{
    private static int _nextTaskId;

    /// <summary>
    /// 任务id
    /// </summary>
    public readonly int taskId;
    /// <summary>
    /// 关联的Promise
    /// </summary>
    public readonly ResourcePromise<object> promise = new ResourcePromise<object>();
    /// <summary>
    /// 任务优先级
    ///
    /// 注：数值越小优先级越高。
    /// </summary>
    private int _priority;

    protected ResourceTask() {
        taskId = ++_nextTaskId;
    }

    public ValueFuture Future => new ValueFuture(promise);

    /// <summary>
    /// 关联的调度器
    /// </summary>
    public TaskScheduler Scheduler {
        get {
            Task<Blackboard> control = Control;
            if (control is TaskScheduler scheduler) {
                return scheduler;
            }
            if (control == null) return null;
            while ((control = control.Control) != null) {
                if (control is TaskScheduler scheduler2) {
                    return scheduler2;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// 任务的优先级
    /// </summary>
    public int Priority {
        get => _priority;
        set {
            int prev = _priority;
            if (prev == value) return;
            _priority = value;
            OnPriorityChanged(prev);
        }
    }

    /// <summary>
    /// 用户自定义Flags
    /// </summary>
    public bool GetFlag(int index) {
        if (index < 0 || index > 8) throw new ArgumentOutOfRangeException(nameof(index));
        return BitFlags.GetAt(flags, index + USER_FLAGS_OFFSET);
    }

    public void SetFlag(int index, bool value) {
        if (index < 0 || index > 8) throw new ArgumentOutOfRangeException(nameof(index));
        flags = BitFlags.SetAt(flags, index + USER_FLAGS_OFFSET, value);
    }

    /// <summary>
    /// 任务的优先级发生变更
    /// 注：可能需要同步到关联的加载任务；由于Bundle加载任务可能被共享，因此通常只应该提升优先级。
    /// </summary>
    protected virtual void OnPriorityChanged(int prevValue) {
        Scheduler?.OnPriorityChanged(this, prevValue);
    }

    /// <summary>
    /// 在资源加载这块更推荐轮询代替事件驱动，可以降低复杂度
    /// </summary>
    /// <param name="child"></param>
    protected override void OnChildCompleted(Task<Blackboard> child) {
    }

    /// <summary>
    /// 通知监听器
    /// </summary>
    public virtual void NotifyListeners() {
    }

    /// <summary>
    /// 是否已销毁
    /// </summary>
    public bool IsDestroyed {
        get => (flags & MASK_DESTROYED) != 0;
        protected set {
            if (!value) throw new InvalidOperationException();
            flags |= MASK_DESTROYED;
        }
    }

    /// <summary>
    /// 销毁对象
    /// </summary>
    public virtual void Destroy() {
        IsDestroyed = true;
    }

    /// <summary>
    /// 检查任务是否已销毁
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ThrowIfDestroyed() {
        if (IsDestroyed) throw new InvalidOperationException("Object has already been destroyed.");
    }

    public override bool Equals(object obj) {
        return this == obj;
    }

    public override int GetHashCode() {
        return taskId;
    }

    #region util

    /// <summary>
    /// 是否已销毁
    ///
    /// 注：高8位是Task控制流标识，次高8位开放给用户，所以Task只可使用低16位。
    /// </summary>
    private const int MASK_DESTROYED = 0x01;
    /// <summary>
    /// 正在通知监听器
    /// </summary>
    protected const int MASK_NOTIFYING = 0x02;
    /// <summary>
    /// 用户标记位偏移
    /// </summary>
    private const int USER_FLAGS_OFFSET = 16;

    #endregion
}
}