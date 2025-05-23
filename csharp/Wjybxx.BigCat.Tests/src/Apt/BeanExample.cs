#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
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
using System.Collections.Generic;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Codec.Attributes;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests.Apt
{
/// <summary>
/// 用于测试继承问题
/// </summary>
public class BaseTypeExample
{
    /// <summary>
    /// 需要序列化
    /// </summary>
    private int value;

    /// <summary>
    /// 测试字段忽略
    /// </summary>
    [DsonIgnore] public int ignoreA;
    /// <summary>
    /// 测试字段忽略
    /// </summary>
    [DsonIgnore] private int ignoreB;

    /// <summary>
    /// 测试字段忽略
    /// </summary>
    [NonSerialized] public int ignoreC;
    /// <summary>
    /// 测试字段忽略
    /// </summary>
    [NonSerialized] public int ignoreD;

    public int Value {
        get => value;
        set => this.value = value;
    }
    public int IgnoreA {
        get => ignoreA;
        set => ignoreA = value;
    }
    public int IgnoreB {
        get => ignoreB;
        set => ignoreB = value;
    }
    public int IgnoreC {
        get => ignoreC;
        set => ignoreC = value;
    }
    public int IgnoreD {
        get => ignoreD;
        set => ignoreD = value;
    }
}

/// <summary>
/// 
/// </summary>
[DsonSerializable]
public class BeanExample : BaseTypeExample
{
    [DsonProperty(Name = "_name", StringStyle = StringStyle.AutoQuote)]
    private string? name;

    [DsonProperty(NumberStyle = NumberStyle.Unsigned)]
    private int age;

    public string? Name {
        get => name;
        set => name = value;
    }

    public int Age {
        get => age;
        set => age = value;
    }

    /// <summary>
    /// 测试自动属性
    /// </summary>
    [DsonProperty(WriteProxy = "WriteType", ReadProxy = "ReadType")]
    public int Type { get; set; }

    /// <summary>
    /// 测试泛型集合
    /// </summary>
    public HashSet<string>? hashSet;
    /// <summary>
    /// 测试泛型集合
    /// </summary>
    [DsonProperty(Impl = typeof(HashSet<>), ObjectStyle = ObjectStyle.Flow)]
    public ISet<string>? hashSet2;

    /// <summary>
    /// 测试字段忽略
    /// </summary>
    [DsonIgnore()]
    public HashSet<string>? ignoreSet;

    /// <summary>
    /// 测试字段忽略
    /// </summary>
    [NonSerialized] public HashSet<string>? ignoreSet2;

    public void WriteType(IDsonObjectWriter writer, string dsonName) {
        writer.WriteInt(dsonName, Type);
    }

    public void ReadType(IDsonObjectReader reader, string dsonName) {
        Type = reader.ReadInt(dsonName);
    }

    /// <summary>
    /// 会自动调用
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    public static BeanExample NewInstance(IDsonObjectReader reader) {
        return new BeanExample();
    }

    /// <summary>
    /// 在写入字段前调用，可写入自定义内容
    /// </summary>
    /// <param name="writer"></param>
    public void WriteObject(IDsonObjectWriter writer) {
    }

    /// <summary>
    /// 在读取字段前调用，可读取自定义内容
    /// </summary>
    /// <param name="reader"></param>
    public void ReadObject(IDsonObjectReader reader) {
    }

    /// <summary>
    /// 在对象序列化之前调用
    /// </summary>
    /// <param name="options"></param>
    public void BeforeEncode(ConverterOptions options) {
    }

    /// <summary>
    /// 在对象反序列化后调用
    /// </summary>
    /// <param name="options"></param>
    public void AfterDecode(ConverterOptions options) {
    }
}
}