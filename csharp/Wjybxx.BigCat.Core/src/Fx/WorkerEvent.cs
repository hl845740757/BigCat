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

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// Worker线程上支持的事件
/// (以后再删除不必要的字段，正式项目会用完的)
/// </summary>
public struct WorkerEvent : IAgentEvent
{
#nullable disable
    private int type;
    private int options;
    public int intVal1;
    public int intVal2;
    public long longVal1;
    public long longVal2;
    public object obj1;
    public object obj2;
    public object obj3;

    /// <summary>
    /// 构造函数将type声明为可选值，会导致不被调用构造函数
    /// </summary>
    public static readonly Func<WorkerEvent> FACTORY = () => {
        WorkerEvent r = default;
        r.type = IAgentEvent.TYPE_INVALID;
        return r;
    };

    public WorkerEvent(int type) : this() {
        this.type = type;
    }

    public int Type {
        get => type;
        set => type = value;
    }

    public int Options {
        get => options;
        set => options = value;
    }

    public object Obj1 {
        get => obj1;
        set => obj1 = value;
    }

    public object Obj2 {
        get => obj2;
        set => obj2 = value;
    }

    public object Obj3 {
        get => obj3;
        set => obj3 = value;
    }

    public long LongVal1 {
        get => longVal1;
        set => longVal1 = value;
    }
    public long LongVal2 {
        get => longVal2;
        set => longVal2 = value;
    }

    public int IntVal1 {
        get => intVal1;
        set => intVal1 = value;
    }
    public int IntVal2 {
        get => intVal2;
        set => intVal2 = value;
    }

    public void Clean() {
        type = IAgentEvent.TYPE_INVALID;
        options = 0;
        obj1 = null;
        obj2 = null;
        obj3 = null;
    }

    public void CleanAll() {
        type = IAgentEvent.TYPE_INVALID;
        options = 0;
        obj1 = null;
        obj2 = null;
        obj3 = null;
        intVal1 = 0;
        intVal2 = 0;
        longVal1 = 0;
        longVal2 = 0;
    }

    public override string ToString() {
        return $"{nameof(type)}: {type}," +
               $" {nameof(options)}: {options}," +
               $" {nameof(obj1)}: {obj1}," +
               $" {nameof(obj2)}: {obj2}," +
               $" {nameof(obj3)}: {obj3}," +
               $" {nameof(intVal1)}: {intVal1}," +
               $" {nameof(intVal2)}: {intVal2}," +
               $" {nameof(longVal1)}: {longVal1}," +
               $" {nameof(longVal2)}: {longVal2}";
    }
}
}