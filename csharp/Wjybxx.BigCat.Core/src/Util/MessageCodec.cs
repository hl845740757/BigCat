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
using Google.Protobuf;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCat.Util
{
/// <summary>
/// protobuf的Message的解码器
/// Message会序列化为字节数组，因此不可以作为顶层对象。
/// (Message作为Rpc方法的参数或结果时会被特殊处理，不会走用户的序列化。)
/// 
/// C#的自循环泛型，其如何运行的更难理解。
/// </summary>
/// <typeparam name="T"></typeparam>
public class MessageCodec<T> : IDsonCodec<T> where T : IMessage<T>
{
    private readonly MessageParser<T> parser;

    public MessageCodec(MessageParser<T> parser) {
        this.parser = parser;
    }

    public Type GetEncoderType() => typeof(T);

    public bool AutoStartEnd => false;

    public void WriteObject(IDsonObjectWriter writer, in T inst, Type declaredType, ObjectStyle style) {
        writer.WriteBytes(null, inst.ToByteArray());
    }

    public T ReadObject(IDsonObjectReader reader, Func<object>? factory = null) {
        byte[] bytes = reader.ReadBytes(reader.CurrentName);
        if (bytes == null) {
            return default;
        }
        return parser.ParseFrom(bytes);
    }
}
}