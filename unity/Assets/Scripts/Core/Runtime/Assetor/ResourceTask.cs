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
using System.Runtime.CompilerServices;
using Wjybxx.BTree;
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Logger;
using ILogger = Wjybxx.Commons.Logger.ILogger;

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
    protected static readonly ILogger logger = LoggerFactory.GetLogger(typeof(Provider));
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
    /// 监听器列表
    /// </summary>
    private List<CallbackItem> _callbacks;
    /// <summary>
    /// 任务优先级
    ///
    /// 注：数值越小优先级越高。
    /// </summary>
    private int _priority;

    protected ResourceTask() {
        taskId = ++_nextTaskId;
    }

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

    #region callback

    /// <summary>
    /// 资源任务的await实现
    /// </summary>
    /// <returns></returns>
    public Awaiter GetAwaiter() => new Awaiter(this);

    /// <summary>
    /// 当前通知状态
    /// </summary>
    public bool IsNotifying {
        get => (flags & MASK_NOTIFYING) != 0;
        private set => flags = BitFlags.Set(flags, MASK_NOTIFYING, value);
    }

    public void RegisterCallback(Action action) {
        RegisterCallbackImpl(action, default);
    }

    public void UnregisterCallback(Action action) {
        UnregisterCallbackImpl(action, default);
    }

    public void RegisterCallback(Action<ResourceTask> action) {
        RegisterCallbackImpl(action, default);
    }

    public void UnregisterCallback(Action<ResourceTask> action) {
        UnregisterCallbackImpl(action, default);
    }

    public void RegisterCallback(Action<AssetHandle> action, AssetHandle handle) {
        RegisterCallbackImpl(action, handle);
    }

    public void UnregisterCallback(Action<AssetHandle> action, AssetHandle handle) {
        UnregisterCallbackImpl(action, handle);
    }

    private void RegisterCallbackImpl(Delegate action, AssetHandle handle) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        _callbacks ??= new List<CallbackItem>();
        _callbacks.Add(new CallbackItem(action, handle));
    }

    private void UnregisterCallbackImpl(Delegate action, AssetHandle handle) {
        List<CallbackItem> callbacks = _callbacks;
        if (callbacks == null || callbacks.Count == 0) return;
        for (int index = callbacks.Count - 1; index >= 0; index--) {
            CallbackItem wrapper = callbacks[index];
            if (wrapper.action != action || wrapper.handle != handle) {
                continue;
            }
            if (IsNotifying) {
                callbacks[index] = default;
            } else {
                callbacks.RemoveAt(index);
            }
            return;
        }
    }

    /// <summary>
    /// 删除Handle关联的所有监听器
    /// </summary>
    /// <param name="handle"></param>
    public void UnregisterHandleCallbacks(AssetHandle handle) {
        List<CallbackItem> callbacks = _callbacks;
        if (callbacks == null || callbacks.Count == 0) return;
        for (int index = callbacks.Count - 1; index >= 0; index--) {
            CallbackItem wrapper = callbacks[index];
            if (wrapper.handle != handle) {
                continue;
            }
            if (IsNotifying) {
                callbacks[index] = default;
            } else {
                callbacks.RemoveAt(index);
            }
        }
    }

    /// <summary>
    /// 通知监听器
    /// </summary>
    public void NotifyListeners() {
        List<CallbackItem> callbacks = _callbacks;
        if (callbacks == null || callbacks.Count == 0) return;
        // 当前迭代过程中新增的回调立即通知没有问题，因为也是延迟执行的
        IsNotifying = true;
        for (int i = 0; i < callbacks.Count; i++) {
            var wrapper = callbacks[i];
            if (wrapper.action == null) continue;
            callbacks[i] = default; // 先清理
            try {
                switch (wrapper.action) {
                    case Action action1: // await
                        action1.Invoke();
                        break;
                    case Action<AssetHandle> action2: // handle
                        action2.Invoke(wrapper.handle);
                        break;
                    case Action<ResourceTask> action3: // 其它
                        action3.Invoke(this);
                        break;
                    default: throw new AssertionError();
                }
            }
            catch (Exception ex) {
                logger.Warn(ex);
            }
        }
        callbacks.Clear();
        IsNotifying = false;
    }

    private readonly struct CallbackItem : IEquatable<CallbackItem>
    {
        public readonly Delegate action; // Action或Action<Handle>或Action<Task>
        public readonly AssetHandle handle;

        public CallbackItem(Delegate action, AssetHandle handle = default) {
            this.action = action;
            this.handle = handle;
        }

        public bool Equals(CallbackItem other) {
            return handle.Equals(other.handle) && Equals(action, other.action);
        }

        public override bool Equals(object obj) {
            return obj is CallbackItem other && Equals(other);
        }

        public override int GetHashCode() {
            return (handle.GetHashCode() * 397) ^ (action != null ? action.GetHashCode() : 0);
        }
    }

    /// <summary>
    /// 注意：该Awaiter是不暴露给用户的，仅限资源管理层任务之间交互
    /// </summary>
    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly ResourceTask _task;

        internal Awaiter(ResourceTask task) {
            _task = task;
        }

        // 1.IsCompleted
        // IsCompleted只在Start后调用一次，EventLoop可以通过接口查询是否已在线程中
        public bool IsCompleted => _task.IsCompleted;

        // 2. GetResult
        // 状态机只在IsCompleted为true时，和OnCompleted后调用GetResult，因此在目标线程中 -- 不可手动调用
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetResult() {
            return _task.promise.result;
        }

        // 3. OnCompleted
        /// <summary>
        /// 添加一个Future完成时的回调。
        /// </summary>
        /// <param name="continuation">回调任务</param>
        public void OnCompleted(Action continuation) {
            if (continuation == null) throw new ArgumentNullException(nameof(continuation));
            _task.RegisterCallback(continuation);
        }

        public void UnsafeOnCompleted(Action continuation) {
            if (continuation == null) throw new ArgumentNullException(nameof(continuation));
            _task.RegisterCallback(continuation);
        }
    }

    #endregion

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