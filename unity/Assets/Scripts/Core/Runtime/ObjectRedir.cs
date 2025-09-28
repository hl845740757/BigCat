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

namespace Wjybxx.BigCat.UnityCore
{
/// <summary>
/// 对象重定向
///
/// 注：用于外部为目标定义别名的情况。
/// </summary>
[Serializable]
public struct ObjectRedir
{
    public string name;
    public UnityEngine.Object target;

    public ObjectRedir(string name, UnityEngine.Object target) {
        this.name = name;
        this.target = target;
    }
}
}