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

using UnityEngine.InputSystem;

namespace Wjybxx.BigCat.Control
{
/// <summary>
/// 按键重绑定的结果
/// </summary>
public readonly struct InputRebindResult
{
    /// <summary>
    /// 是否完成(false表示被取消或超时)
    /// </summary>
    public readonly bool completed;
    /// <summary>
    /// 被重绑定的Action
    /// </summary>
    public readonly InputAction action;
    /// <summary>
    /// 被重绑定的binding下标
    /// </summary>
    public readonly int bindingIndex;
    /// <summary>
    /// 新的控件路径
    ///
    /// 注：被取消时为null。
    /// </summary>
    public readonly string newPath;

    public InputRebindResult(bool completed, InputAction action, int bindingIndex, string newPath) {
        this.completed = completed;
        this.action = action;
        this.bindingIndex = bindingIndex;
        this.newPath = newPath;
    }

    public override string ToString() {
        return $"InputRebindResult(completed: {completed}, action: {action?.name}, " +
               $"bindingIndex: {bindingIndex}, newPath: {newPath})";
    }
}
}
