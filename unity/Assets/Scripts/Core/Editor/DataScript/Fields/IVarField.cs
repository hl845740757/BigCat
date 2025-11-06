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

using UnityEngine.UIElements;
using Wjybxx.BigCat.CoreEditor.UIElements;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
/// <summary>
/// 变量字段抽象
/// 
/// 注：
/// 1.字段自身不应该阻断ValueChanged事件传播，需要上报给应用层，由应用层决定是否停止传播。
/// 2.字段类避免使用<see cref="VisualElement"/>的userData属性，保留给用户。
/// </summary>
internal interface IVarField : IPrefixLabel
{
    /// <summary>
    /// 绑定数据
    /// </summary>
    /// <param name="editor"></param>
    /// <param name="variable"></param>
    void Bind(DataEditor editor, Variable variable);

    /// <summary>
    /// 解除绑定
    /// </summary>
    void Unbind();

    /// <summary>
    /// 刷新UI，需要递归刷新子节点数据
    ///
    /// 注：rebuild参数需要递归传递给子节点。
    /// </summary>
    /// <param name="rebuild"></param>
    void Refresh(bool rebuild = false);
}
}