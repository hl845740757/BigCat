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
using System.Reflection;
using System.Runtime.CompilerServices;
using Google.Protobuf;

namespace Wjybxx.BigCat.Util
{
/// <summary>
/// protobuf 工具类
/// </summary>
public static class ProtobufUtils
{
#nullable disable
    /// <summary>
    /// 支持将builder和message转bytes
    /// (c#不是Builder模式)
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ToBytes(object obj) {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        if (obj is IMessage message) {
            return message.ToByteArray();
        }
        throw new ArgumentException("invalid type: " + obj.GetType());
    }

    /// <summary>
    /// 寻找protoBuf消息的parser对象
    /// (C#会生成静态的Parser属性)
    /// </summary>
    /// <param name="clazz"></param>
    /// <returns></returns>
    public static MessageParser FindParser(Type clazz) {
        PropertyInfo property = clazz.GetProperty("Parser", BindingFlags.Public | BindingFlags.Static);
        if (property == null || property.GetMethod == null) {
            throw new ArgumentException("invalid type: " + clazz);
        }
        return (MessageParser)property.GetMethod.Invoke(null, Array.Empty<object>());
    }
}
}