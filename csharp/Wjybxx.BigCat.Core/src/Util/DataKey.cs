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

namespace Wjybxx.BigCat.Util
{
/// <summary>
/// 非泛型接口用于装箱，避免用户传入奇怪的对象。
/// </summary>
public interface DataKey
{
}

/// <summary>
/// 数据键抽象，搭配<see cref="UnionValue"/>使用。
/// </summary>
/// <typeparam name="T"></typeparam>
public interface DataKey<T> : DataKey
{
    T Unbox(in UnionValue boxedValue);

    UnionValue Box(T value);
}
}