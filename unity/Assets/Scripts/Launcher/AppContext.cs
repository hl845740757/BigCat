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

using Wjybxx.BTree;
using Wjybxx.BTree.FSM;

namespace Wjybxx.BigCat.Launcher
{
/// <summary>
/// 应用程序上下文
///
/// 1.全局静态上下文，没有实例化的必要。
/// 2.该上下文管理应用程序的生命周期和主状态机（流程）。
/// 3.不命名为Application，以避免和unity冲突。
/// 
///
/// 理论上应当由该Context驱动所有的Update，这样才能精确控制Update逻辑，但那样的话会产生大量的转发，浪费性能；
/// 因此Context通过开关（和Update列表）来调整需要Update的逻辑。
/// </summary>
public static class AppContext
{
    /// <summary>
    /// taskEntry
    /// 其实AppContext可以继承TaskEntry，但为了避免暴露不必要的接口，我们还是不继承。
    /// </summary>
    private static readonly TaskEntry<object> taskEntry = new TaskEntry<object>();
    /// <summary>
    /// 顶层状态机 -- 避免频繁强制类型转换
    /// </summary>
    private static readonly StateMachineTask<object> fsm = new StateMachineTask<object>();

    static AppContext() {
        taskEntry.RootTask = fsm;
    }

    /// <summary>
    /// 心跳方法
    /// </summary>
    /// <param name="frame">当前帧号</param>
    public static void Update(int frame) {
        taskEntry.UpdateInlined(frame);
    }

    /// <summary>
    /// 切换应用程序状态
    /// </summary>
    /// <param name="nextState"></param>
    public static void ChangeState(Task<object> nextState) {
        fsm.ChangeState(nextState);
    }

    /// <summary>
    /// 派发事件
    /// </summary>
    /// <param name="evt"></param>
    public static void OnEvent(object evt) {
        fsm.OnEvent(evt);
    }
}
}