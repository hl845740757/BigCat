#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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

using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Codec.Attributes;
using Wjybxx.Dson.Text;

#pragma warning disable CS0169

namespace Wjybxx.Dson.Tests.Apt
{
#nullable disable

[DsonCodecLinkerBean(typeof(ThirdPartyBean))]
public class LinkerBeanExample
{
    /// <summary>
    /// 
    /// </summary>
    [DsonProperty(WriteProxy = "WriteAge", ReadProxy = "ReadAge")]
    private int age;
    /// <summary>
    /// 只匹配类型
    /// </summary>
    [DsonProperty(EncodeFeatures = SerializeFeatures.StringAutoQuote)]
    public string name;

    /// <summary>
    /// 属性映射属性
    /// </summary>
    public int Sex { get; set; }

    public static void BeforeEncode(ThirdPartyBean inst, ConverterOptions options) {
    }

    public static void AfterDecode(ThirdPartyBean inst, ConverterOptions options) {
    }

    public static void WriteAge(ThirdPartyBean inst, IDsonObjectWriter writer, string name) {
        writer.WriteInt(name, inst.Age);
    }

    public static void ReadAge(ThirdPartyBean inst, IDsonObjectReader reader, string name) {
        inst.Age = reader.ReadInt(name);
    }
}
}