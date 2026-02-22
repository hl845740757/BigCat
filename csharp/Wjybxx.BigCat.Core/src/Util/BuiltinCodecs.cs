#region LICENSE

// Copyright 2026 wjybxx(845740757@qq.com)
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
using Wjybxx.Dson.Codec;

namespace Wjybxx.BigCat.Util
{
/// <summary>
/// 内置Codec支持
/// </summary>
public class BuiltinCodecs
{
    /// <summary>
    /// APT无法正确解析泛型枚举约束，以后再处理...
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnumSetCodec<T> : IDsonCodec<EnumSet<T>> where T : struct, Enum
    {
        public void WriteObject(IDsonObjectWriter writer, EnumSet<T> inst, Type declaredType, SerializeFeatures _) {
            const SerializeFeatures features = SerializeFeatures.WriteAsArray | SerializeFeatures.ObjectFlow;
            writer.WriteStartArray(typeof(EnumSet<T>), declaredType, features);
            inst.WriteObject(writer);
            writer.WriteEndArray();
        }

        public EnumSet<T> ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object> factory = null) {
            reader.ReadStartArray(typeof(EnumSet<T>));
            EnumSet<T> result = EnumSet<T>.NewInstance(reader);
            reader.ReadEndArray();
            return result;
        }
    }

    public class EnumSet64Codec<T> : IDsonCodec<EnumSet64<T>> where T : struct, Enum
    {
        public void WriteObject(IDsonObjectWriter writer, EnumSet64<T> inst, Type declaredType, SerializeFeatures _) {
            const SerializeFeatures features = SerializeFeatures.WriteAsArray | SerializeFeatures.ObjectFlow;
            writer.WriteStartArray(typeof(EnumSet64<T>), declaredType, features);
            inst.WriteObject(writer);
            writer.WriteEndArray();
        }

        public EnumSet64<T> ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object> factory = null) {
            reader.ReadStartArray(typeof(EnumSet64<T>));
            EnumSet64<T> result = EnumSet64<T>.NewInstance(reader);
            reader.ReadEndArray();
            return result;
        }
    }
}
}