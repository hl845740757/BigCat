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

namespace Wjybxx.BigCat.Gameplay
{
/// <summary>
/// 专用于为<see cref="Scene"/>和<see cref="GameUnit"/>分配id。
///
/// PS：是否保证全局唯一，由实现类确定；如果不需要入库的话，通常无需全局唯一。
/// </summary>
public interface SceneIdMgr
{
    /// <summary>
    /// 生成下一个id
    /// </summary>
    /// <returns></returns>
    long Next();
}
}