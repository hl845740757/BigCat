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

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// 依赖注入接口
/// </summary>
public interface Injector
{
    /// <summary>
    /// 获取指定类型的实例
    ///
    /// 如果未注册，则抛出异常
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>返回类型应当是T或T的子类型</returns>
    T GetInstance<T>();

    /// <summary>
    /// 获取指定类型的实例
    ///
    /// 如果未注册，则抛出异常
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>返回类型应当是T或T的子类型</returns>
    object GetInstance(Type type);
}
}