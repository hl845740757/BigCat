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

using Wjybxx.BigCat.CoreEditor.UIElements;

namespace Wjybxx.BigCat.CoreEditor.DataScript
{
internal interface IVarField : IPrefixLabel
{
    /// <summary>
    /// 完成数据绑定之后调用
    /// </summary>
    /// <param name="editor"></param>
    /// <param name="variable"></param>
    void Bind(DataGraphEditor editor, Variable variable);

    /// <summary>
    /// 刷新UI，需要递归刷新子节点数据
    /// </summary>
    void Refresh();
}
}