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
using Wjybxx.Commons.Attributes;

namespace Wjybxx.BigCat.Fx
{
/// <summary>
/// Rpc使用的序列化接口
///
/// TODO 改为返回Bytebuf
/// </summary>
[ThreadSafe]
public interface IRpcSerializer
{
    /// <summary>
    /// 序列化
    /// </summary>
    /// <param name="value">要序列化的对象</param>
    /// <param name="declaredType">对象的声明类型（方法参数或返回值的声明类型）；非泛型</param>
    /// <returns></returns>
    byte[] Write(object value, Type declaredType);

    /// <summary>
    /// 反序列化
    /// </summary>
    /// <param name="source">字节数组</param>
    /// <param name="declaredType">对象的声明类型（方法参数或返回值的声明类型）；非泛型</param>
    /// <returns></returns>
    object Read(byte[] source, Type declaredType);
}
}