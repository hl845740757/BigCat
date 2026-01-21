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
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Codec.Attributes;
using Wjybxx.Dson.Types;

namespace Wjybxx.BigCat.Core
{
/// <summary>
/// 用于为Unity的常用类型生成DsonCodec
///
/// 注：
/// 1.常用数据结构已转为<see cref="Double4"/>存储，可以有效减少DsonObject数量。
/// 2.Codec类定义为public的，以方便扫描（只扫描ExportTypes）。
/// </summary>
// [UsedForReflectionBasedGenerator]
[DsonCodecLinkerGroup]
public class UnityCodecLinker
{
    // private Vector2 _vector2;
    // private Vector3 _vector3;
    // private Vector4 _vector4;
    // private Quaternion _quaternion;
    // private Vector2Int _vector2Int; // 命名不规范，需要特殊映射...
    // private Vector3Int _vector3Int; // 命名不规范，需要特殊映射...
    // private Color _color;

// ReSharper disable All
    public class Vector2Codec : IDsonCodec<Vector2>
    {
        public void WriteObject(IDsonObjectWriter writer, Vector2 inst, Type declaredType, SerializeFeatures _) {
            const SerializeFeatures features = SerializeFeatures.Double4AsVector | SerializeFeatures.Double4Len2;
            writer.WriteDouble4(new Double4(inst.x, inst.y, 0), features);
        }

        public Vector2 ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object> factory = null) {
            Double4 quad = reader.ReadDouble4();
            return new Vector2((float)quad.v0, (float)quad.v1);
        }
    };

    public class Vector3Codec : IDsonCodec<Vector3>
    {
        public void WriteObject(IDsonObjectWriter writer, Vector3 inst, Type declaredType, SerializeFeatures _) {
            const SerializeFeatures features = SerializeFeatures.Double4AsVector | SerializeFeatures.Double4Len3;
            writer.WriteDouble4(new Double4(inst.x, inst.y, inst.z), features);
        }

        public Vector3 ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object> factory = null) {
            Double4 quad = reader.ReadDouble4();
            return new Vector3((float)quad.v0, (float)quad.v1, (float)quad.v2);
        }
    };

    public class Vector4Codec : IDsonCodec<Vector4>
    {
        public void WriteObject(IDsonObjectWriter writer, Vector4 inst, Type declaredType, SerializeFeatures _) {
            const SerializeFeatures features = SerializeFeatures.Double4AsVector;
            writer.WriteDouble4(new Double4(inst.x, inst.y, inst.z, inst.w), features);
        }

        public Vector4 ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object> factory = null) {
            Double4 quad = reader.ReadDouble4();
            return new Vector4((float)quad.v0, (float)quad.v1, (float)quad.v2, (float)quad.v3);
        }
    };

    public class QuaternionCodec : IDsonCodec<Quaternion>
    {
        public void WriteObject(IDsonObjectWriter writer, Quaternion inst, Type declaredType, SerializeFeatures _) {
            const SerializeFeatures features = SerializeFeatures.Double4AsVector;
            writer.WriteDouble4(new Double4(inst.x, inst.y, inst.z, inst.w), features);
        }

        public Quaternion ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object> factory = null) {
            Double4 quad = reader.ReadDouble4();
            return new Quaternion((float)quad.v0, (float)quad.v1, (float)quad.v2, (float)quad.v3);
        }
    };

    public class Vector2IntCodec : IDsonCodec<Vector2Int>
    {
        public void WriteObject(IDsonObjectWriter writer, Vector2Int inst, Type declaredType, SerializeFeatures _) {
            const SerializeFeatures features = SerializeFeatures.Double4AsVector
                                               | SerializeFeatures.Double4AsInt
                                               | SerializeFeatures.Double4Len2;
            writer.WriteDouble4(new Double4(inst.x, inst.y, 0), features);
        }

        public Vector2Int ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object> factory = null) {
            Double4 quad = reader.ReadDouble4();
            return new Vector2Int((int)quad.v0, (int)quad.v1);
        }
    };

    public class Vector3IntCodec : IDsonCodec<Vector3Int>
    {
        public void WriteObject(IDsonObjectWriter writer, Vector3Int inst, Type declaredType, SerializeFeatures _) {
            const SerializeFeatures features = SerializeFeatures.Double4AsVector
                                               | SerializeFeatures.Double4AsInt
                                               | SerializeFeatures.Double4Len3;
            writer.WriteDouble4(new Double4(inst.x, inst.y, inst.z), features);
        }

        public Vector3Int ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object> factory = null) {
            Double4 quad = reader.ReadDouble4();
            return new Vector3Int((int)quad.v0, (int)quad.v1, (int)quad.v2);
        }
    };

    public class ColorCodec : IDsonCodec<Color>
    {
        public void WriteObject(IDsonObjectWriter writer, Color inst, Type declaredType, SerializeFeatures _) {
            const SerializeFeatures features = SerializeFeatures.Double4AsRgba;
            writer.WriteDouble4(new Double4(inst.r, inst.g, inst.b, inst.a), features);
        }

        public Color ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object> factory = null) {
            Double4 quad = reader.ReadDouble4();
            return new Vector4((float)quad.v0, (float)quad.v1, (float)quad.v2, (float)quad.v3);
        }
    };
}
}