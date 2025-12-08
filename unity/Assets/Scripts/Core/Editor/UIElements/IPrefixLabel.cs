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

namespace Wjybxx.BigCat.Editor.UIElements
{
/// <summary>
/// 为字段类定义Label读写方法
///
/// 注：C#中public类可以实现internal接口，但不能继承internal类，因为接口中的方法在子类中必须显式定义。
/// </summary>
internal interface IPrefixLabel
{
    public string label { get; set; }
}
}