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
using Wjybxx.Dson.Types;

namespace Wjybxx.BigCat.Util
{
/// <summary>
/// 内置Codec支持
/// </summary>
public class BuiltinCodecs
{
    public class Fixed64Codec : IDsonCodec<Fixed64>
    {
        public void WriteObject(IDsonObjectWriter writer, Fixed64 inst, Type declaredType, SerializeFeatures features) {
            writer.WriteFxp64(Fxp64.FromRaw(inst.RawValue));
        }

        public Fixed64 ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features) {
            Fxp64 fxp64 = reader.ReadFxp64();
            return Fixed64.FromRaw(fxp64.rawValue);
        }
    }

    /// <summary>
    /// APT无法正确解析泛型枚举约束，以后再处理...
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnumSetCodec<T> : IDsonCodec<EnumSet<T>> where T : struct, Enum
    {
        public void WriteObject(IDsonObjectWriter writer, EnumSet<T> inst, Type declaredType, SerializeFeatures features) {
            writer.WriteStartArray(typeof(EnumSet<T>), declaredType, SerializeFeatures.ObjectFlow);
            inst.WriteObject(writer, features);
            writer.WriteEndArray();
        }

        public EnumSet<T> ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features) {
            reader.ReadStartArray(typeof(EnumSet<T>));
            EnumSet<T> result = EnumSet<T>.NewInstance(reader);
            reader.ReadEndArray();
            return result;
        }
    }

    public class EnumSet64Codec<T> : IDsonCodec<EnumSet64<T>> where T : struct, Enum
    {
        public void WriteObject(IDsonObjectWriter writer, EnumSet64<T> inst, Type declaredType, SerializeFeatures features) {
            writer.WriteStartArray(typeof(EnumSet64<T>), declaredType, SerializeFeatures.ObjectFlow);
            inst.WriteObject(writer, features);
            writer.WriteEndArray();
        }

        public EnumSet64<T> ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features) {
            reader.ReadStartArray(typeof(EnumSet64<T>));
            EnumSet64<T> result = EnumSet64<T>.NewInstance(reader);
            reader.ReadEndArray();
            return result;
        }
    }
}
}