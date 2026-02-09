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
using UnityEngine;
using Wjybxx.Dson;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Types;

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// 内建Codec支持
/// </summary>
public static class BuiltinCodecs
{
    public class AABBCodec : IDsonCodec<MinMaxAABB>
    {
        public void WriteObject(IDsonObjectWriter writer, MinMaxAABB inst, Type declaredType, SerializeFeatures _) {
            const SerializeFeatures features = SerializeFeatures.Double4AsVector | SerializeFeatures.Double4Len3;
            writer.WriteStartObject(typeof(MinMaxAABB));
            writer.WriteDouble4("min", inst.min.ToDouble4(), features);
            writer.WriteDouble4("size", inst.Size.ToDouble4(), features);
            writer.WriteEndObject();
        }

        public MinMaxAABB ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object> factory = null) {
            // 支持Min+Max、Min+Size
            reader.ReadStartObject(typeof(MinMaxAABB), DeserializeFeatures.PassiveRandomRead);
            Vector3 min = reader.ReadDouble4("min").ToVector3();
            Vector3 max = reader.ReadName() switch
            {
                "max" => reader.ReadDouble4().ToVector3(),
                "size" => min + reader.ReadDouble4().ToVector3(),
                _ => throw new InvalidOperationException("Unknown MinMaxAABB format"),
            };
            reader.ReadEndObject();
            return new MinMaxAABB(min, max);
        }
    }

    public class Euler32Codec : IDsonCodec<Euler32>
    {
        public void WriteObject(IDsonObjectWriter writer, Euler32 inst, Type declaredType, SerializeFeatures _) {
            const SerializeFeatures features = SerializeFeatures.Double4AsVector
                                               | SerializeFeatures.Double4AsInt
                                               | SerializeFeatures.Double4Len3;
            writer.WriteDouble4(new Double4(inst.x, inst.y, inst.z), features);
        }

        public Euler32 ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object> factory = null) {
            Double4 quad = reader.ReadDouble4();
            return new Euler32((int)quad.v0, (int)quad.v1, (int)quad.v2);
        }
    }
}
}