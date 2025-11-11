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
using System.Reflection;
using Wjybxx.BigCat.Fx;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Text;

namespace Wjybxx.BigCat.Tests
{
public class TestRpcSerializer : RpcSerializer
{
    private readonly IDsonConverter converter;

    public TestRpcSerializer() {
        List<Type> codecClsList = ScanCodecs();
        DsonConverterBuilder builder = new DsonConverterBuilder();
        foreach (Type codecType in codecClsList) {
            Type encoderType = GetEncoderType(codecType);
            // 添加Codec
            if (codecType.IsGenericType) {
                builder.AddGenericCodec(encoderType, codecType);
                builder.AddTypeMeta(TypeMeta.Of(encoderType, encoderType.GetGenericTypeDefinition().Name));
            } else {
                builder.AddTypeMeta(TypeMeta.Of(encoderType, encoderType.Name));
                builder.AddCodec((IDsonCodec)Activator.CreateInstance(codecType)!);
            }
        }
        converter = builder.Build();
    }

    private static Type GetEncoderType(Type codecType) {
        Type @interface = codecType.GetInterface(typeof(IDsonCodec<>).Name);
        return @interface.GetGenericArguments()[0];
    }

    private static List<Type> ScanCodecs() {
        string interfaceName = typeof(IDsonCodec).FullName;
        List<Type> result = new List<Type>();
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            string assemblyName = assembly.GetName().Name;
            if (assemblyName == null || !assemblyName.StartsWith("Wjybxx.BigCat")) {
                continue;
            }
            foreach (var type in assembly.GetTypes()) {
                if (type.Name.EndsWith("Codec") && type.GetInterface(interfaceName!) != null) {
                    result.Add(type);
                }
            }
        }
        return result;
    }

    public byte[] Write(object value, Type declaredType) {
        return converter.Write(value, declaredType);
    }

    public object Read(byte[] source, Type declaredType) {
        return converter.Read(source, declaredType);
    }
}
}