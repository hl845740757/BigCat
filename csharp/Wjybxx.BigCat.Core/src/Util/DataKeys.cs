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
using System.Collections.Concurrent;
using Wjybxx.Commons;

#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif

// ReSharper disable UnusedMember.Global
namespace Wjybxx.BigCat.Util
{
/// <summary>
/// 这里提供了默认的<see cref="DataKey{T}"/>实现
///
/// <h3>非常量</h3>
/// 我们没有实现为<see cref="AbstractConstant"/>，是因为多数情况下对灵活性和易用性的要求高于性能；
/// 只有极致追求性能的场景，我们才需要使用<see cref="AbstractConstant"/>通过引用相等代替equals，以及严格校验Key的相等性。
///
/// <h3>泛型</h3>
/// 理论上，多数场景下我们只需要name相等即镶嵌，但默认的Equals还是校验了类型，即泛型参数不仅仅用于避免拆装箱，也用于测试Key的相等性。
/// 也就是说，不同地方的类型声明必须是相同的，否则将无法读写黑板。
///
/// <h3>池化</h3>
/// 可以通过<see cref="Intern"/>方法将Key放入常量池，可以有效提高查找效率，减少内存开销。
/// 
/// </summary>
public static class DataKeys
{
    public const int TYPE_INT32 = 1;
    public const int TYPE_INT64 = 2;
    public const int TYPE_FLOAT = 3;
    public const int TYPE_DOUBLE = 4;
    public const int TYPE_BOOL = 5;
    public const int TYPE_STRING = 6;
    public const int TYPE_OBJECT = 7;
    private const int TYPE_NULLABLE = 8;

    public const int TYPE_UINT32 = 9;
    public const int TYPE_UINT64 = 10;

    public const int TYPE_VECTOR3 = 11;
    public const int TYPE_VECTOR4 = 12;
    public const int TYPE_QUATERNION = 13;

    public const int TYPE_VECTOR2 = 14;
    public const int TYPE_VECTOR2_INT = 15;
    public const int TYPE_VECTOR3_INT = 16;

    public const int TYPE_COLOR = 17;
    public const int TYPE_COLOR32 = 18;
    public const int TYPE_RECT = 19;
    public const int TYPE_KEY_CODE = 20;

    public const int TYPE_BITSET = 21;

    #region 池化

    /// <summary>
    /// 池化的对象
    /// </summary>
    private static readonly ConcurrentDictionary<(string, Type), DataKey> internedKeys = new();

    /// <summary>
    /// 尝试获取被池化的key
    /// </summary>
    public static bool TryGetInterned(string name, Type dataType, out DataKey key) {
        (string, Type) cacheKey = (name, dataType);
        return internedKeys.TryGetValue(cacheKey, out key);
    }

    /// <summary>
    /// 将DataKey加入常量池，如果Key已存在于常量池，则返回常量池中的对象，否则返回当前对象。 
    /// </summary>
    public static DataKey Intern(DataKey key) {
        if (key == null) {
            throw new ArgumentNullException(nameof(key));
        }
        (string, Type) cacheKey = (key.Name, key.DataType);
        if (internedKeys.TryAdd(cacheKey, key)) {
            return key;
        }
        return internedKeys[cacheKey];
    }

    /// <summary>
    /// 将DataKey加入常量池，如果Key已存在于常量池，则返回常量池中的对象，否则返回当前对象。 
    /// </summary>
    public static DataKey<T> Intern<T>(DataKey<T> key) {
        if (key == null) {
            throw new ArgumentNullException(nameof(key));
        }
        (string, Type) cacheKey = (key.Name, key.DataType);
        if (internedKeys.TryAdd(cacheKey, key)) {
            return key;
        }
        return (DataKey<T>)internedKeys[cacheKey];
    }

    #endregion

    #region 工厂方法

    public static DataKey<int> NewIntKey(string name) {
        return new IntKey(name);
    }

    public static DataKey<long> NewLongKey(string name) {
        return new LongKey(name);
    }

    public static DataKey<float> NewFloatKey(string name) {
        return new FloatKey(name);
    }

    public static DataKey<double> NewDoubleKey(string name) {
        return new DoubleKey(name);
    }

    public static DataKey<bool> NewBoolKey(string name) {
        return new BoolKey(name);
    }

    public static DataKey<string> NewStringKey(string name) {
        return new StringKey(name);
    }

    public static DataKey<object> NewObjectKey(string name) {
        return new ObjectKey<object>(name);
    }

    public static DataKey<T> NewObjectKey<T>(string name) {
        return new ObjectKey<T>(name);
    }

    public static DataKey<uint> NewUIntKey(string name) {
        return new UIntKey(name);
    }

    public static DataKey<ulong> NewULongKey(string name) {
        return new ULongKey(name);
    }

#if UNITY_2021_3_OR_NEWER
    public static DataKey<Vector3> NewVector3Key(string name) {
        return new Vector3Key(name);
    }

    public static DataKey<Vector4> NewVector4Key(string name) {
        return new Vector4Key(name);
    }

    public static DataKey<Quaternion> NewQuaternionKey(string name) {
        return new QuaternionKey(name);
    }

    public static DataKey<Vector2> NewVector2Key(string name) {
        return new Vector2Key(name);
    }

    public static DataKey<Vector2Int> NewVector2IntKey(string name) {
        return new Vector2IntKey(name);
    }

    public static DataKey<Vector3Int> NewVector3IntKey(string name) {
        return new Vector3IntKey(name);
    }

    public static DataKey<Color> NewColorKey(string name) {
        return new ColorKey(name);
    }

    public static DataKey<Color32> NewColor32Key(string name) {
        return new Color32Key(name);
    }

    public static DataKey<Rect> NewRectKey(string name) {
        return new RectKey(name);
    }

    public static DataKey<KeyCode> NewKeyCodeKey(string name) {
        return new KeyCodeKey(name);
    }
#endif

    public static DataKey<GBitSet> NewGBitSetKey(string name) {
        return new BitSetKey(name);
    }

    #endregion

    #region key实现

    public class ObjectKey<T> : DataKey<T>
    {
        public ObjectKey(string name) : base(name) {
        }

        public override T Unbox(in UnionValue boxedValue) {
            return (T)boxedValue.obj1;
        }

        public override UnionValue Box(T value) {
            return new UnionValue(TYPE_OBJECT) { obj1 = value };
        }
    }

    public class IntKey : DataKey<int>
    {
        public IntKey(string name) : base(name) {
        }

        public override int Unbox(in UnionValue boxedValue) {
            return boxedValue.val;
        }

        public override UnionValue Box(int value) {
            return new UnionValue(TYPE_INT32) { val = value };
        }
    }

    public class LongKey : DataKey<long>
    {
        public LongKey(string name) : base(name) {
        }

        public override long Unbox(in UnionValue boxedValue) {
            return boxedValue.lv1;
        }

        public override UnionValue Box(long value) {
            return new UnionValue(TYPE_INT64) { lv1 = value };
        }
    }

    public class FloatKey : DataKey<float>
    {
        public FloatKey(string name) : base(name) {
        }

        public override float Unbox(in UnionValue boxedValue) {
            return boxedValue.fVal;
        }

        public override UnionValue Box(float value) {
            return new UnionValue(TYPE_FLOAT) { fVal = value };
        }
    }

    public class DoubleKey : DataKey<double>
    {
        public DoubleKey(string name) : base(name) {
        }

        public override double Unbox(in UnionValue boxedValue) {
            return boxedValue.dv1;
        }

        public override UnionValue Box(double value) {
            return new UnionValue(TYPE_DOUBLE) { dv1 = value };
        }
    }

    public class BoolKey : DataKey<bool>
    {
        public BoolKey(string name) : base(name) {
        }

        public override bool Unbox(in UnionValue boxedValue) {
            return boxedValue.val > 0;
        }

        public override UnionValue Box(bool value) {
            return new UnionValue(TYPE_BOOL) { val = value ? 1 : 0 };
        }
    }

    public class StringKey : DataKey<string>
    {
        public StringKey(string name) : base(name) {
        }

        public override string Unbox(in UnionValue boxedValue) {
            return (string)boxedValue.obj1;
        }

        public override UnionValue Box(string value) {
            return new UnionValue(TYPE_STRING) { obj1 = value };
        }
    }

    public class UIntKey : DataKey<uint>
    {
        public UIntKey(string name) : base(name) {
        }

        public override uint Unbox(in UnionValue boxedValue) {
            return (uint)boxedValue.val;
        }

        public override UnionValue Box(uint value) {
            return new UnionValue(TYPE_UINT32) { val = (int)value };
        }
    }

    public class ULongKey : DataKey<ulong>
    {
        public ULongKey(string name) : base(name) {
        }

        public override ulong Unbox(in UnionValue boxedValue) {
            return (ulong)boxedValue.lv1;
        }

        public override UnionValue Box(ulong value) {
            return new UnionValue(TYPE_UINT64) { lv1 = (long)value };
        }
    }

    public class BitSetKey : DataKey<GBitSet>
    {
        public BitSetKey(string name) : base(name) {
        }

        public override GBitSet Unbox(in UnionValue boxedValue) {
            return new GBitSet()
            {
                LowBits = boxedValue.lv1,
                HighBits = boxedValue.lv2,
            };
        }

        public override UnionValue Box(GBitSet value) {
            return new UnionValue(TYPE_BITSET)
            {
                lv1 = value.LowBits,
                lv2 = value.HighBits,
            };
        }
    }

#if UNITY_2021_3_OR_NEWER
    public class Vector3Key : DataKey<Vector3>
    {
        public Vector3Key(string name) : base(name) {
        }

        public override Vector3 Unbox(in UnionValue boxedValue) {
            return new Vector3(
                (float)boxedValue.dv1,
                (float)boxedValue.dv2,
                (float)boxedValue.dv3);
        }

        public override UnionValue Box(Vector3 value) {
            return new UnionValue(TYPE_VECTOR3)
            {
                dv1 = value.x,
                dv2 = value.y,
                dv3 = value.z
            };
        }
    }

    public class Vector4Key : DataKey<Vector4>
    {
        public Vector4Key(string name) : base(name) {
        }

        public override Vector4 Unbox(in UnionValue boxedValue) {
            return new Vector4()
            {
                x = (float)boxedValue.dv1,
                y = (float)boxedValue.dv2,
                z = (float)boxedValue.dv3,
                w = boxedValue.fVal
            };
        }

        public override UnionValue Box(Vector4 value) {
            return new UnionValue(TYPE_VECTOR4)
            {
                dv1 = value.x,
                dv2 = value.y,
                dv3 = value.z,
                fVal = value.w
            };
        }
    }

    public class QuaternionKey : DataKey<Quaternion>
    {
        public QuaternionKey(string name) : base(name) {
        }

        public override Quaternion Unbox(in UnionValue boxedValue) {
            return new Quaternion()
            {
                x = (float)boxedValue.dv1,
                y = (float)boxedValue.dv2,
                z = (float)boxedValue.dv3,
                w = boxedValue.fVal
            };
        }

        public override UnionValue Box(Quaternion value) {
            return new UnionValue(TYPE_QUATERNION)
            {
                dv1 = value.x,
                dv2 = value.y,
                dv3 = value.z,
                fVal = value.w
            };
        }
    }

    public class Vector2Key : DataKey<Vector2>
    {
        public Vector2Key(string name) : base(name) {
        }

        public override Vector2 Unbox(in UnionValue boxedValue) {
            return new Vector2()
            {
                x = (float)boxedValue.dv1,
                y = (float)boxedValue.dv2,
            };
        }

        public override UnionValue Box(Vector2 value) {
            return new UnionValue(TYPE_VECTOR2)
            {
                dv1 = value.x,
                dv2 = value.y,
            };
        }
    }

    public class Vector2IntKey : DataKey<Vector2Int>
    {
        public Vector2IntKey(string name) : base(name) {
        }

        public override Vector2Int Unbox(in UnionValue boxedValue) {
            return new Vector2Int()
            {
                x = (int)boxedValue.lv1,
                y = (int)boxedValue.lv2,
            };
        }

        public override UnionValue Box(Vector2Int value) {
            return new UnionValue(TYPE_VECTOR2_INT)
            {
                lv1 = value.x,
                lv2 = value.y,
            };
        }
    }

    public class Vector3IntKey : DataKey<Vector3Int>
    {
        public Vector3IntKey(string name) : base(name) {
        }

        public override Vector3Int Unbox(in UnionValue boxedValue) {
            return new Vector3Int()
            {
                x = (int)boxedValue.lv1,
                y = (int)boxedValue.lv2,
                z = (int)boxedValue.lv3
            };
        }

        public override UnionValue Box(Vector3Int value) {
            return new UnionValue(TYPE_VECTOR3_INT)
            {
                lv1 = value.x,
                lv2 = value.y,
                lv3 = value.z,
            };
        }
    }

    public class ColorKey : DataKey<Color>
    {
        public ColorKey(string name) : base(name) {
        }

        public override Color Unbox(in UnionValue boxedValue) {
            return new Color()
            {
                r = (float)boxedValue.dv1,
                g = (float)boxedValue.dv2,
                b = (float)boxedValue.dv3,
                a = boxedValue.fVal
            };
        }

        public override UnionValue Box(Color value) {
            return new UnionValue(TYPE_COLOR)
            {
                dv1 = value.r,
                dv2 = value.g,
                dv3 = value.b,
                fVal = value.a
            };
        }
    }

    public class Color32Key : DataKey<Color32>
    {
        public Color32Key(string name) : base(name) {
        }

        public override Color32 Unbox(in UnionValue boxedValue) {
            int rgba = boxedValue.val;
            byte r = (byte)(rgba & 0xff);
            byte g = (byte)(rgba >> 8);
            byte b = (byte)(rgba >> 16);
            byte a = (byte)(rgba >> 24);
            return new Color32(r, g, b, a);
        }

        public override UnionValue Box(Color32 color) {
            int rgba = color.r | color.g << 8 | color.b << 16 | color.a << 24;
            return new UnionValue(TYPE_COLOR32) { val = rgba };
        }
    }

    public class RectKey : DataKey<Rect>
    {
        public RectKey(string name) : base(name) {
        }

        public override Rect Unbox(in UnionValue boxedValue) {
            return new Rect()
            {
                x = (float)boxedValue.dv1,
                y = (float)boxedValue.dv2,
                width = (float)boxedValue.dv3,
                height = boxedValue.fVal
            };
        }

        public override UnionValue Box(Rect value) {
            return new UnionValue(TYPE_RECT)
            {
                dv1 = value.x,
                dv2 = value.y,
                dv3 = value.width,
                fVal = value.height
            };
        }
    }

    public class KeyCodeKey : DataKey<KeyCode>
    {
        public KeyCodeKey(string name) : base(name) {
        }

        public override KeyCode Unbox(in UnionValue boxedValue) {
            return (KeyCode)boxedValue.val;
        }

        public override UnionValue Box(KeyCode value) {
            return new UnionValue(TYPE_KEY_CODE) { val = (int)value };
        }
    }
#endif

    #endregion
}
}