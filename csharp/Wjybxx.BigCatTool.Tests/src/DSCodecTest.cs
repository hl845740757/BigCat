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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wjybxx.BigCatTool.Tests.Generated;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCatTool.Tests;

/// <summary>
/// 测试生成类型的编解码
/// </summary>
public class DSCodecTest
{
    private static IDsonConverter converter;

    [SetUp]
    public void SetUp() {
        DsonConverterBuilder builder = new DsonConverterBuilder();
        // 反射查找所有的Codec
        List<Type> codecTypes = typeof(SimpleBean).Assembly.GetTypes()
            .Where(e => e.GetInterface("Wjybxx.Dson.Codec.IDsonCodec`1") != null)
            .ToList();
        foreach (Type codecType in codecTypes) {
            // 传递给AbstractCodec的才是EncoderType
            Type encoderType = codecType.BaseType!.GenericTypeArguments[0];
            if (encoderType.IsGenericType) {
                encoderType = encoderType.GetGenericTypeDefinition();
                builder.AddGenericCodec(encoderType, codecType);
            } else {
                builder.AddCodec(encoderType, (IDsonCodec)Activator.CreateInstance(codecType));
            }

            TypeMeta typeMeta = TypeMeta.Of(encoderType, ObjectStyle.Indent, RemoveGenericInfo(encoderType.Name));
            builder.AddTypeMeta(typeMeta);
        }
        converter = builder.Build();
    }

    private static string RemoveGenericInfo(string clsName) {
        int index = clsName.IndexOf('`');
        return index > 0 ? clsName.Substring(0, index) : clsName;
    }

    [Test]
    public void TestVector3() {
        string dson1 = @"{x: 1, y: 2.5, z: 1}"; // object格式
        string dson2 = @"[1, 2.5, 1]"; // array格式
        Vector3 v1 = converter.ReadFromDson<Vector3>(dson1);
        Vector3 v2 = converter.ReadFromDson<Vector3>(dson2);
        Assert.AreEqual(v1, v2);
    }
}