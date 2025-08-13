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
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Logger;

namespace Wjybxx.BigCat.MVC
{
/// <summary>
/// 监听器容器
///
/// 注：请使用<see cref="Create"/>创建实例。
/// </summary>
public readonly struct ListenerContainer
{
    private static readonly ILogger logger = LoggerFactory.GetLogger(typeof(ListenerContainer));
    public readonly SmallDynamicArray<IDataModelListener?> listeners;

    public static ListenerContainer Create(int capacity) {
        return new ListenerContainer(capacity);
    }

    private ListenerContainer(int capacity) : this() {
        listeners = new SmallDynamicArray<IDataModelListener>(capacity);
    }

    public void Add(IDataModelListener listener) {
        if (listener == null) throw new ArgumentNullException(nameof(listener));
        listeners.Add(listener);
    }

    public void Remove(IDataModelListener listener) {
        listeners.Remove(listener);
    }

    public void Broadcast(object? eventData = null) {
        SmallDynamicArray<IDataModelListener> array = listeners;
        if (array.Length == 0) {
            return;
        }
        array.BeginItr();
        try {
            for (int idx = 0, len = array.Length; idx < len; idx++) {
                IDataModelListener listener = array[idx];
                if (listener == null) continue;
                try {
                    listener.OnDataChanged(eventData);
                }
                catch (Exception ex) {
                    logger.Info(ex, listener.GetType().Name);
                }
            }
        }
        finally {
            array.EndItr();
        }
    }
}
}